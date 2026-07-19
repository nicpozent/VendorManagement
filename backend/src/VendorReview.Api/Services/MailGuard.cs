namespace VendorReview.Api.Services;

/// <summary>
/// Recipient allowlist for outbound mail (NDA + reminders). When
/// <c>Mail:AllowedRecipientDomains</c> is configured, mail may only be sent to
/// addresses in those domains — closing the "authenticated user sends mail to an
/// arbitrary address" spam/phishing pivot. Empty config = allow all (dev default).
/// </summary>
public class MailGuard
{
    private readonly HashSet<string> _domains;

    public MailGuard(IConfiguration cfg)
    {
        _domains = (cfg.GetSection("Mail:AllowedRecipientDomains").Get<string[]>() ?? Array.Empty<string>())
            .Select(d => d.Trim().TrimStart('@').ToLowerInvariant())
            .Where(d => d.Length > 0)
            .ToHashSet();
    }

    public bool Enabled => _domains.Count > 0;

    public bool IsAllowed(string? email)
    {
        if (!Enabled) return true;
        if (string.IsNullOrWhiteSpace(email)) return false;
        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1) return false;
        return _domains.Contains(email[(at + 1)..].Trim().ToLowerInvariant());
    }

    /// <summary>Split addresses into allowed / blocked by the configured domains.</summary>
    public (List<string> Allowed, List<string> Blocked) Partition(IEnumerable<string> emails)
    {
        var allowed = new List<string>();
        var blocked = new List<string>();
        foreach (var e in emails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct())
            (IsAllowed(e) ? allowed : blocked).Add(e);
        return (allowed, blocked);
    }
}
