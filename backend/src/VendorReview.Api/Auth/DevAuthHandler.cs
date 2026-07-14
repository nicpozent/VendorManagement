using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace VendorReview.Api.Auth;

/// <summary>
/// Development-only authentication used when Entra ("AzureAd") is not configured, so
/// the whole app runs without a tenant. It fabricates a principal from optional
/// X-Debug-* headers (role defaults to CFO / admin). NEVER registered when AzureAd
/// settings are present — production always validates real Entra tokens.
/// </summary>
public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Dev";

    public DevAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["X-Debug-Role"].FirstOrDefault() ?? "Cfo";
        var name = Request.Headers["X-Debug-User"].FirstOrDefault() ?? "Nick [Surname]";
        var oid = Request.Headers["X-Debug-Oid"].FirstOrDefault() ?? "dev-nick";
        var email = Request.Headers["X-Debug-Email"].FirstOrDefault() ?? "nick@birgma.dev";

        var claims = new List<Claim>
        {
            new("oid", oid),
            new("name", name),
            new("preferred_username", email),
            new(ClaimTypes.Role, role),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
