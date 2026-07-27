using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using VendorReview.Api.Auth;
using VendorReview.Api.Domain;
using VendorReview.Api.Services;
using Xunit;

namespace VendorReview.Api.Tests.Unit;

/// <summary>Outbound-mail recipient allowlist.</summary>
public class MailGuardTests
{
    private static MailGuard Guard(params string[] domains)
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(
            domains.Select((d, i) => new KeyValuePair<string, string?>($"Mail:AllowedRecipientDomains:{i}", d))).Build();
        return new MailGuard(cfg);
    }

    [Fact]
    public void Empty_allowlist_allows_all()
    {
        var g = Guard();
        Assert.False(g.Enabled);
        Assert.True(g.IsAllowed("anyone@anywhere.example"));
    }

    [Fact]
    public void Configured_allowlist_filters_by_domain()
    {
        var g = Guard("birgma.com");
        Assert.True(g.Enabled);
        Assert.True(g.IsAllowed("nick@birgma.com"));
        Assert.False(g.IsAllowed("attacker@evil.example"));
        Assert.False(g.IsAllowed(null));
        Assert.False(g.IsAllowed("no-at-sign"));
    }

    [Fact]
    public void Partition_splits_allowed_and_blocked()
    {
        var (allowed, blocked) = Guard("birgma.com").Partition(
            new[] { "a@birgma.com", "b@evil.example", "c@birgma.com" });
        Assert.Equal(new[] { "a@birgma.com", "c@birgma.com" }, allowed);
        Assert.Equal(new[] { "b@evil.example" }, blocked);
    }
}

/// <summary>Role is derived from server-trusted claims — never a client hint.</summary>
public class CurrentUserRoleTests
{
    private static CurrentUser From(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
        claims.Add(new Claim("oid", "obj-1"));
        claims.Add(new Claim("name", "Test User"));
        var http = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        return new CurrentUser(new HttpContextAccessor { HttpContext = http });
    }

    [Fact]
    public void Cfo_is_admin_and_leadership()
    {
        var u = From("Cfo");
        Assert.Equal(AppRole.Cfo, u.Role);
        Assert.True(u.IsAdmin);
        Assert.True(u.IsLeadership);
    }

    [Theory]
    [InlineData("cio")]
    [InlineData("cto")]
    [InlineData("ciocto")]
    [InlineData("admin")]
    [InlineData("leadership")]
    public void Leadership_synonyms_map_to_ciocto(string role)
    {
        var u = From(role);
        Assert.Equal(AppRole.CioCto, u.Role);
        Assert.True(u.IsAdmin);
    }

    [Fact]
    public void No_role_claim_defaults_to_manager()
    {
        var u = From();
        Assert.Equal(AppRole.ITManager, u.Role);
        Assert.False(u.IsAdmin);
        Assert.False(u.IsLeadership);
    }
}
