using System.Security.Claims;
using VendorReview.Api.Domain;

namespace VendorReview.Api.Auth;

/// <summary>
/// Resolves the caller's identity and role from the validated token (or the dev
/// principal). Role is used for data scoping and admin gating; it is derived from
/// server-trusted claims, never from a client-supplied hint.
/// </summary>
public class CurrentUser
{
    public string ObjectId { get; }
    public string DisplayName { get; }
    public string? Email { get; }
    public AppRole Role { get; }
    public bool IsAuthenticated { get; }

    /// <summary>Platform administrator = CIO/CTO and CFO (per CLAUDE.md §4).</summary>
    public bool IsAdmin => Role is AppRole.CioCto or AppRole.Cfo;

    /// <summary>Leadership sees the whole portfolio; managers see only their own.</summary>
    public bool IsLeadership => IsAdmin;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        var http = accessor.HttpContext;
        var user = http?.User;
        IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        ObjectId = user?.FindFirst("oid")?.Value
            ?? user?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "dev-unknown";

        DisplayName = user?.FindFirst("name")?.Value
            ?? user?.FindFirst(ClaimTypes.Name)?.Value
            ?? "Unknown user";

        Email = user?.FindFirst("preferred_username")?.Value
            ?? user?.FindFirst(ClaimTypes.Email)?.Value
            ?? user?.FindFirst("email")?.Value;

        Role = ResolveRole(user);
    }

    private static AppRole ResolveRole(ClaimsPrincipal? user)
    {
        if (user is null) return AppRole.ITManager;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(user.FindAll("roles").Select(c => c.Value))
            .Select(v => v.ToLowerInvariant())
            .ToHashSet();

        if (roles.Contains("cfo")) return AppRole.Cfo;
        if (roles.Any(r => r is "ciocto" or "cio" or "cto" or "leadership" or "admin"))
            return AppRole.CioCto;
        return AppRole.ITManager;
    }
}
