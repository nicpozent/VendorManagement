using VendorReview.Api.Services;
using Xunit;

namespace VendorReview.Api.Tests.Unit;

/// <summary>
/// AES-256-GCM field protection: round-trips, envelope markers, disabled pass-through,
/// and graceful handling of tampered/undecryptable input. FieldProtection is a static
/// singleton, so each test sets the config it needs and Dispose resets to disabled.
/// </summary>
public class FieldProtectionTests : IDisposable
{
    // A valid base64 32-byte AES-256 key (deterministic for the test).
    private static readonly string Key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    public void Dispose() => FieldProtection.Configure(null, false);

    [Fact]
    public void Disabled_is_passthrough()
    {
        FieldProtection.Configure(null, enabled: false);
        Assert.Equal("hello", FieldProtection.Protect("hello"));
        Assert.Equal("hello", FieldProtection.Unprotect("hello"));
    }

    [Fact]
    public void Configure_rejects_bad_key()
    {
        Assert.Throws<InvalidOperationException>(() => FieldProtection.Configure("not-base64!!", enabled: true));
        Assert.Throws<InvalidOperationException>(() => FieldProtection.Configure(Convert.ToBase64String(new byte[16]), enabled: true));
        Assert.Throws<InvalidOperationException>(() => FieldProtection.Configure(null, enabled: true));
    }

    [Fact]
    public void Text_round_trips_and_ciphertext_differs()
    {
        FieldProtection.Configure(Key, enabled: true);
        var cipher = FieldProtection.Protect("sofia.maric@atlasmdm.com");
        Assert.StartsWith("enc:v1:", cipher);
        Assert.NotEqual("sofia.maric@atlasmdm.com", cipher);
        Assert.Equal("sofia.maric@atlasmdm.com", FieldProtection.Unprotect(cipher));
    }

    [Fact]
    public void Two_encryptions_use_distinct_nonces()
    {
        FieldProtection.Configure(Key, enabled: true);
        Assert.NotEqual(FieldProtection.Protect("same"), FieldProtection.Protect("same"));
    }

    [Fact]
    public void Legacy_plaintext_reads_back_unchanged()
    {
        FieldProtection.Configure(Key, enabled: true);
        Assert.Equal("legacy plaintext", FieldProtection.Unprotect("legacy plaintext"));
    }

    [Fact]
    public void Bytes_round_trip_and_are_marked()
    {
        FieldProtection.Configure(Key, enabled: true);
        var data = new byte[5000];
        new Random(42).NextBytes(data);
        var enc = FieldProtection.ProtectBytes(data);
        Assert.False(enc.AsSpan().SequenceEqual(data));       // actually encrypted
        Assert.Equal(new byte[] { 0x45, 0x4E, 0x43, 0x31 }, enc[..4]); // "ENC1" magic
        Assert.True(FieldProtection.UnprotectBytes(enc).AsSpan().SequenceEqual(data));
    }

    [Fact]
    public void Tampered_ciphertext_does_not_throw()
    {
        FieldProtection.Configure(Key, enabled: true);
        var cipher = FieldProtection.Protect("secret");
        var tampered = cipher[..^2] + (cipher.EndsWith("A") ? "B" : "A");
        var result = FieldProtection.Unprotect(tampered); // GCM tag fails → returns stored, never throws
        Assert.NotEqual("secret", result);
    }
}
