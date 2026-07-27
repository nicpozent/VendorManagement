using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Domain;

namespace VendorReview.Api.Data;

public static class SettingsRepo
{
    public const string BlockerCapsKey = "blockerCapsVerdict";
    public const string ReviewIntervalKey = "reviewIntervalMonths";
    public const int DefaultReviewIntervalMonths = 12;

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

    /// <summary>Months between required re-reviews of an approved vendor (default 12).</summary>
    public static async Task<int> ReviewIntervalMonths(AppDbContext db, CancellationToken ct)
    {
        var s = await db.Settings.FindAsync(new object?[] { ReviewIntervalKey }, ct);
        return s is not null && int.TryParse(s.Value, out var m) && m > 0 ? m : DefaultReviewIntervalMonths;
    }

    public static async Task SetReviewIntervalMonths(AppDbContext db, int months, CancellationToken ct)
    {
        if (months < 1) months = DefaultReviewIntervalMonths;
        var s = await db.Settings.FindAsync(new object?[] { ReviewIntervalKey }, ct);
        if (s is null) db.Settings.Add(new Setting { Key = ReviewIntervalKey, Value = months.ToString() });
        else s.Value = months.ToString();
    }
}
