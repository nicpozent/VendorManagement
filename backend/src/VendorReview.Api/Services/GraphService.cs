using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using VendorReview.Api.Dtos;

namespace VendorReview.Api.Services;

public record GraphMember(string ObjectId, string DisplayName, string? Email, string? JobTitle);

public interface IGraphService
{
    bool IsConfigured { get; }
    Task<List<ImportSourceDto>> ListSourcesAsync(string kind, CancellationToken ct);
    Task<List<GraphMember>> GetMembersAsync(string kind, string sourceId, CancellationToken ct);
    Task<bool> SendMailAsync(IEnumerable<string> to, IEnumerable<string> cc, string subject,
        string htmlBody, CancellationToken ct);
}

/// <summary>
/// Microsoft Graph integration (app-only / client-credentials). Used by the admin
/// user-import feature and by the reminder service to send mail via MS Graph.
/// When AzureAd + Graph settings are absent it degrades to deterministic mock data
/// so the admin screens are demonstrable without a tenant.
/// </summary>
public class GraphService : IGraphService
{
    private readonly GraphServiceClient? _graph;
    private readonly string? _sender;
    private readonly ILogger<GraphService> _log;

    public bool IsConfigured => _graph is not null;

    public GraphService(IConfiguration cfg, ILogger<GraphService> log)
    {
        _log = log;
        var tenant = cfg["AzureAd:TenantId"];
        var client = cfg["AzureAd:ClientId"];
        var secret = cfg["AzureAd:ClientSecret"];
        _sender = cfg["Graph:SenderUserId"]; // mailbox UPN or object id used as From

        if (!string.IsNullOrWhiteSpace(tenant) && !string.IsNullOrWhiteSpace(client)
            && !string.IsNullOrWhiteSpace(secret))
        {
            var cred = new ClientSecretCredential(tenant, client, secret);
            _graph = new GraphServiceClient(cred, new[] { "https://graph.microsoft.com/.default" });
            _log.LogInformation("Graph configured for app-only access (tenant {Tenant}).", tenant);
        }
        else
        {
            _log.LogWarning("Graph not configured — admin import & reminders run in MOCK mode.");
        }
    }

    public async Task<List<ImportSourceDto>> ListSourcesAsync(string kind, CancellationToken ct)
    {
        if (_graph is null) return MockSources(kind);
        try
        {
            switch (kind)
            {
                case "group":
                {
                    var r = await _graph.Groups.GetAsync(rc =>
                    {
                        rc.QueryParameters.Select = new[] { "id", "displayName", "description" };
                        rc.QueryParameters.Top = 200;
                    }, ct);
                    return (r?.Value ?? new()).Select(g => new ImportSourceDto(
                        g.Id!, g.DisplayName ?? "(unnamed group)", "group", g.Description, null)).ToList();
                }
                case "servicePrincipal": // Enterprise applications
                {
                    var r = await _graph.ServicePrincipals.GetAsync(rc =>
                    {
                        rc.QueryParameters.Select = new[] { "id", "displayName", "appId", "tags" };
                        rc.QueryParameters.Top = 200;
                    }, ct);
                    return (r?.Value ?? new()).Select(s => new ImportSourceDto(
                        s.Id!, s.DisplayName ?? "(unnamed app)", "servicePrincipal",
                        $"Enterprise app · appId {s.AppId}", null)).ToList();
                }
                case "application": // App registrations
                {
                    var r = await _graph.Applications.GetAsync(rc =>
                    {
                        rc.QueryParameters.Select = new[] { "id", "displayName", "appId" };
                        rc.QueryParameters.Top = 200;
                    }, ct);
                    return (r?.Value ?? new()).Select(a => new ImportSourceDto(
                        a.Id!, a.DisplayName ?? "(unnamed registration)", "application",
                        $"App registration · appId {a.AppId}", null)).ToList();
                }
                default: return new();
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Graph ListSources failed for kind {Kind}", kind);
            throw;
        }
    }

    public async Task<List<GraphMember>> GetMembersAsync(string kind, string sourceId, CancellationToken ct)
    {
        if (_graph is null) return MockMembers(sourceId);
        try
        {
            return kind switch
            {
                "group" => await GroupMembers(sourceId, ct),
                "servicePrincipal" => await AppRolePrincipals(sourceId, ct),
                "application" => await AppOwners(sourceId, ct),
                _ => new()
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Graph GetMembers failed for {Kind}/{Source}", kind, sourceId);
            throw;
        }
    }

    private async Task<List<GraphMember>> GroupMembers(string groupId, CancellationToken ct)
    {
        var members = new List<GraphMember>();
        var page = await _graph!.Groups[groupId].Members.GetAsync(rc =>
            rc.QueryParameters.Top = 200, ct);
        foreach (var obj in page?.Value ?? new())
            if (obj is User u) members.Add(ToMember(u));
        return members;
    }

    private async Task<List<GraphMember>> AppRolePrincipals(string spId, CancellationToken ct)
    {
        var members = new List<GraphMember>();
        var assignments = await _graph!.ServicePrincipals[spId].AppRoleAssignedTo.GetAsync(rc =>
            rc.QueryParameters.Top = 200, ct);
        foreach (var a in assignments?.Value ?? new())
        {
            if (!string.Equals(a.PrincipalType, "User", StringComparison.OrdinalIgnoreCase)) continue;
            if (a.PrincipalId is null) continue;
            var u = await SafeGetUser(a.PrincipalId.Value.ToString(), ct);
            if (u is not null) members.Add(u);
            else members.Add(new GraphMember(a.PrincipalId.Value.ToString(),
                a.PrincipalDisplayName ?? "(user)", null, null));
        }
        return members;
    }

    private async Task<List<GraphMember>> AppOwners(string appId, CancellationToken ct)
    {
        var members = new List<GraphMember>();
        var page = await _graph!.Applications[appId].Owners.GetAsync(rc =>
            rc.QueryParameters.Top = 200, ct);
        foreach (var obj in page?.Value ?? new())
            if (obj is User u) members.Add(ToMember(u));
        return members;
    }

    private async Task<GraphMember?> SafeGetUser(string id, CancellationToken ct)
    {
        try
        {
            var u = await _graph!.Users[id].GetAsync(rc =>
                rc.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle" }, ct);
            return u is null ? null : ToMember(u);
        }
        catch { return null; }
    }

    private static GraphMember ToMember(User u) => new(
        u.Id!, u.DisplayName ?? u.UserPrincipalName ?? "(user)",
        u.Mail ?? u.UserPrincipalName, u.JobTitle);

    public async Task<bool> SendMailAsync(IEnumerable<string> to, IEnumerable<string> cc,
        string subject, string htmlBody, CancellationToken ct)
    {
        var toList = to.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();
        if (toList.Count == 0) return false;

        if (_graph is null || string.IsNullOrWhiteSpace(_sender))
        {
            _log.LogInformation("[MOCK MAIL] to={To} cc={Cc} subject={Subject}",
                string.Join(",", toList), string.Join(",", cc), subject);
            return false; // signal "not really sent" so callers can note mock mode
        }

        Recipient R(string e) => new() { EmailAddress = new EmailAddress { Address = e } };
        var body = new SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = htmlBody },
                ToRecipients = toList.Select(R).ToList(),
                CcRecipients = cc.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().Select(R).ToList(),
            },
            SaveToSentItems = true,
        };
        await _graph.Users[_sender].SendMail.PostAsync(body, cancellationToken: ct);
        return true;
    }

    // ---- Mock data (dev / no tenant) ----
    private static List<ImportSourceDto> MockSources(string kind) => kind switch
    {
        "group" => new()
        {
            new("grp-it", "IT Managers — Global", "group", "Sample group (mock)", 3),
            new("grp-lead", "IT Leadership (CIO/CTO/CFO)", "group", "Sample group (mock)", 2),
        },
        "servicePrincipal" => new()
        {
            new("sp-vr", "Vendor Review (Assess)", "servicePrincipal", "Enterprise app (mock)", 4),
        },
        "application" => new()
        {
            new("app-vr", "Vendor Review API", "application", "App registration (mock)", 1),
        },
        _ => new()
    };

    private static List<GraphMember> MockMembers(string sourceId) => sourceId switch
    {
        "grp-lead" => new()
        {
            new("mock-cio", "Alex Berg", "alex.berg@birgma.dev", "CIO / CTO"),
            new("mock-cfo", "Office of the CFO", "cfo@birgma.dev", "CFO"),
        },
        _ => new()
        {
            new("mock-nick", "Nick [Surname]", "nick@birgma.dev", "Senior Manager, Global IT Infrastructure"),
            new("mock-priya", "Priya [Surname]", "priya@birgma.dev", "IT Manager"),
            new("mock-sofia", "Sofia Maric", "sofia@birgma.dev", "IT Manager"),
        }
    };
}
