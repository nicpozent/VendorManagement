using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Data;
using VendorReview.Api.Domain;

namespace VendorReview.Api.Services;

public class ReminderOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 360;
    /// <summary>Don't re-remind the same review more often than this.</summary>
    public int MinHoursBetweenReminders { get; set; } = 20;
}

/// <summary>
/// Periodically emails reminders (via MS Graph) to review owners and their
/// approvers/managers when a review is awaiting action — i.e. still open with
/// blockers or concerns. Also invocable on demand from the admin endpoints.
/// </summary>
public class ReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReminderService> _log;
    private readonly ReminderOptions _opt;

    public ReminderService(IServiceScopeFactory scopes, ILogger<ReminderService> log,
        Microsoft.Extensions.Options.IOptions<ReminderOptions> opt)
    {
        _scopes = scopes;
        _log = log;
        _opt = opt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opt.Enabled)
        {
            _log.LogInformation("Reminder service disabled by configuration.");
            return;
        }
        // Small startup delay so migrations/seed complete first.
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sent = await RunOnceAsync(force: false, stoppingToken);
                if (sent > 0) _log.LogInformation("Reminder sweep sent {Count} reminder(s).", sent);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Reminder sweep failed.");
            }
            try { await Task.Delay(TimeSpan.FromMinutes(_opt.IntervalMinutes), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>Runs a single sweep. Returns the number of reminder emails attempted.</summary>
    public async Task<int> RunOnceAsync(bool force, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var graph = scope.ServiceProvider.GetRequiredService<IGraphService>();
        var engine = scope.ServiceProvider.GetRequiredService<VerdictEngine>();
        var mail = scope.ServiceProvider.GetRequiredService<MailGuard>();

        bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
        var cutoff = DateTime.UtcNow.AddHours(-_opt.MinHoursBetweenReminders);

        var pending = await db.Reviews
            .Include(r => r.Sections).ThenInclude(s => s.Items)
            .Include(r => r.Entity)
            .Where(r => r.Status == ReviewStatus.InProgress || r.Status == ReviewStatus.Concern)
            .ToListAsync(ct);

        var admins = await db.AppUsers
            .Where(u => u.Enabled && (u.Role == AppRole.Cfo || u.Role == AppRole.CioCto))
            .ToListAsync(ct);

        int attempted = 0;
        foreach (var r in pending)
        {
            if (!force && r.LastReminderUtc is not null && r.LastReminderUtc > cutoff) continue;

            var v = engine.Evaluate(r, caps);
            bool needsAction = v.BlockerCount > 0 || v.ConcernCount > 0;
            if (!needsAction) continue;

            var approverEmails = admins
                .Where(a => r.ApproverObjectIds.Count == 0 || r.ApproverObjectIds.Contains(a.EntraObjectId))
                .Select(a => a.Email)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!)
                .ToList();

            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(r.OwnerEmail)) candidates.Add(r.OwnerEmail!);
            candidates.AddRange(approverEmails);
            var (to, _) = mail.Partition(candidates);
            if (to.Count == 0) continue;

            var subject = $"Action needed: vendor review — {r.VendorName} ({v.VerdictLabel})";
            var html = BuildBody(r, v);
            var (ccApprovers, _) = mail.Partition(approverEmails);
            await graph.SendMailAsync(to, cc: ccApprovers, subject, html, ct);
            r.LastReminderUtc = DateTime.UtcNow;
            attempted++;
        }

        if (attempted > 0) await db.SaveChangesAsync(ct);
        return attempted;
    }

    private static string BuildBody(Review r, Dtos.VerdictResultDto v) => $@"
<div style='font-family:Segoe UI,Arial,sans-serif'>
  <h2>Vendor technical review needs your attention</h2>
  <p><strong>{r.VendorName}</strong> — {r.ProductName}<br/>
  Ref {r.ReviewRef} · Owner {r.OwnerName}</p>
  <p>Current verdict: <strong>{v.VerdictLabel}</strong> — {v.VerdictReason}<br/>
  {v.BlockerCount} blocker(s) · {v.ConcernCount} concern(s) · readiness {v.ReadinessPct}%.</p>
  <p>Please review and sign off in the Vendor Review workspace.</p>
</div>";
}
