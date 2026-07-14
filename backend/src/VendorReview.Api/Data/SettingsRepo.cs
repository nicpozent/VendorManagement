using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Domain;

namespace VendorReview.Api.Data;

public static class SettingsRepo
{
    public const string BlockerCapsKey = "blockerCapsVerdict";

    public static async Task<bool> BlockerCapsVerdict(AppDbContext db, CancellationToken ct)
    {
        var s = await db.Settings.FindAsync(new object?[] { BlockerCapsKey }, ct);
        return s is null || s.Value != "false"; // default ON
    }

    public static async Task SetBlockerCapsVerdict(AppDbContext db, bool value, CancellationToken ct)
    {
        var s = await db.Settings.FindAsync(new object?[] { BlockerCapsKey }, ct);
        if (s is null) db.Settings.Add(new Setting { Key = BlockerCapsKey, Value = value ? "true" : "false" });
        else s.Value = value ? "true" : "false";
    }
}
