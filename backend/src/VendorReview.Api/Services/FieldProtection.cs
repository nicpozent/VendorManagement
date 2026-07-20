using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace VendorReview.Api.Services;

/// <summary>
/// Transparent AES-256-GCM encryption for sensitive personal-data columns
/// (GDPR Art. 32). Configured once at startup from <c>Encryption:Key</c> (base64,
/// 32 bytes) and <c>Encryption:Enabled</c>. Values are envelope-marked
/// (<c>enc:v1:</c> + base64 of nonce|tag|ciphertext) so legacy plaintext rows read
/// back unchanged and the scheme is versionable/rotatable. When disabled, both
/// operations are pass-throughs, so existing deployments keep working untouched.
/// </summary>
public static class FieldProtection
{
    private const string Prefix = "enc:v1:";
    private const int NonceLen = 12;
    private const int TagLen = 16;

    private static byte[]? _key;
    private static bool _enabled;

    public static bool Enabled => _enabled;

    public static void Configure(string? base64Key, bool enabled)
    {
        if (!enabled)
        {
            _enabled = false;
            _key = null;
            return;
        }
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new InvalidOperationException(
                "Encryption:Enabled is true but Encryption:Key is not set (expect a base64-encoded 32-byte key).");
        byte[] key;
        try { key = Convert.FromBase64String(base64Key); }
        catch (FormatException) { throw new InvalidOperationException("Encryption:Key must be valid base64."); }
        if (key.Length != 32)
            throw new InvalidOperationException("Encryption:Key must decode to 32 bytes (AES-256).");
        _key = key;
        _enabled = true;
    }

    public static string Protect(string plaintext)
    {
        if (!_enabled || _key is null || plaintext.Length == 0) return plaintext;
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceLen);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagLen];
        using var aes = new AesGcm(_key, TagLen);
        aes.Encrypt(nonce, plain, cipher, tag);
        var env = new byte[NonceLen + TagLen + cipher.Length];
        Buffer.BlockCopy(nonce, 0, env, 0, NonceLen);
        Buffer.BlockCopy(tag, 0, env, NonceLen, TagLen);
        Buffer.BlockCopy(cipher, 0, env, NonceLen + TagLen, cipher.Length);
        return Prefix + Convert.ToBase64String(env);
    }

    public static string Unprotect(string stored)
    {
        if (stored.Length == 0 || !stored.StartsWith(Prefix, StringComparison.Ordinal))
            return stored; // legacy plaintext or empty — return as-is
        if (!_enabled || _key is null)
            return stored; // key unavailable — cannot decrypt; surface the marker rather than throw
        try
        {
            var env = Convert.FromBase64String(stored[Prefix.Length..]);
            var nonce = env.AsSpan(0, NonceLen);
            var tag = env.AsSpan(NonceLen, TagLen);
            var cipher = env.AsSpan(NonceLen + TagLen);
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(_key, TagLen);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return stored;
        }
    }
}

/// <summary>EF Core converter applying <see cref="FieldProtection"/> at the storage boundary.</summary>
public class EncryptedConverter : ValueConverter<string, string>
{
    public EncryptedConverter()
        : base(v => FieldProtection.Protect(v), v => FieldProtection.Unprotect(v)) { }
}
