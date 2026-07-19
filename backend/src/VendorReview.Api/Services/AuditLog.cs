using VendorReview.Api.Auth;
using VendorReview.Api.Data;
using VendorReview.Api.Domain;

namespace VendorReview.Api.Services;

/// <summary>
/// Writes append-only audit records for security-sensitive actions. The actor is
/// taken from the validated token (never client-supplied). Records are only ever
/// inserted — there is no update/delete path.
/// </summary>
public class AuditLog
{
    private readonly AppDbContext _db;
    private readonly CurrentUser _me;

    public AuditLog(AppDbContext db, CurrentUser me)
    {
        _db = db;
        _me = me;
    }

    public async Task WriteAsync(string action, string targetType, string? targetId,
        string? targetName, string? summary, CancellationToken ct = default)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            Utc = DateTime.UtcNow,
            ActorObjectId = _me.ObjectId,
            ActorName = _me.DisplayName,
            ActorRole = _me.Role.ToString(),
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            TargetName = targetName,
            Summary = summary,
        });
        await _db.SaveChangesAsync(ct);
    }
}
