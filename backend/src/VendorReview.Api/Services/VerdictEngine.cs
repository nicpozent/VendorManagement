using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;

namespace VendorReview.Api.Services;

/// <summary>
/// Server-authoritative verdict, readiness % and completeness checks.
/// The client never computes these — it only renders what this returns.
/// </summary>
public class VerdictEngine
{
    public VerdictResultDto Evaluate(Review r, bool blockerCapsVerdict)
    {
        var items = r.Sections.SelectMany(s => s.Items).ToList();

        int blockerCount = items.Count(i => i.Status == ItemStatus.Blocker);
        int openBlockers = items.Count(i => i.Status == ItemStatus.Blocker
                                            && string.IsNullOrWhiteSpace(i.Mitigation));
        int concernCount = items.Count(i => i.Status == ItemStatus.Concern);
        int passCount = items.Count(i => i.Status == ItemStatus.Pass);
        int applicable = items.Count(i => i.Status is ItemStatus.Pass or ItemStatus.Concern or ItemStatus.Blocker);
        int readiness = applicable == 0 ? 0 : (int)Math.Round(100.0 * passCount / applicable);

        Verdict verdict;
        string label;
        string reason;

        if (r.Status == ReviewStatus.Draft && applicable == 0)
        {
            verdict = Verdict.InProgress;
            label = "In progress";
            reason = "Assessment not yet started.";
        }
        else if (blockerCapsVerdict && openBlockers > 0)
        {
            verdict = Verdict.DoNotProceed;
            label = "Do not proceed";
            reason = openBlockers == 1
                ? "1 unresolved blocker caps this review."
                : $"{openBlockers} unresolved blockers cap this review.";
        }
        else if (concernCount > 0 || blockerCount > 0)
        {
            verdict = Verdict.ProceedWithConditions;
            label = "Proceed with conditions";
            var parts = new List<string>();
            if (blockerCount > 0) parts.Add($"{blockerCount} mitigated blocker{(blockerCount == 1 ? "" : "s")}");
            if (concernCount > 0) parts.Add($"{concernCount} concern{(concernCount == 1 ? "" : "s")}");
            reason = "Acceptable subject to " + string.Join(" and ", parts) + ".";
        }
        else if (applicable > 0 && passCount == applicable)
        {
            verdict = Verdict.Proceed;
            label = "Proceed";
            reason = "All assessed items pass.";
        }
        else
        {
            verdict = Verdict.InProgress;
            label = "In progress";
            reason = "Assessment incomplete.";
        }

        return new VerdictResultDto(
            verdict.ToString(), label, reason, readiness,
            blockerCount, concernCount, passCount, applicable,
            Completeness(r, items));
    }

    private static List<CompletenessCheckDto> Completeness(Review r, List<ReviewItemScore> items) => new()
    {
        new("vendor", "Vendor named", !string.IsNullOrWhiteSpace(r.VendorName)),
        new("category", "Category selected", r.CategoryId is not null),
        new("entity", "Entity assigned", r.EntityId is not null),
        new("scored", "At least one item scored",
            items.Any(i => i.Status != ItemStatus.Unscored)),
        new("noUnscored", "No applicable items left unscored",
            items.All(i => i.Status != ItemStatus.Unscored)),
        new("blockersExplained", "Every blocker has a why & unblock path",
            items.Where(i => i.Status == ItemStatus.Blocker)
                 .All(i => !string.IsNullOrWhiteSpace(i.Note) && !string.IsNullOrWhiteSpace(i.Mitigation))),
        new("concernsExplained", "Every concern has a note",
            items.Where(i => i.Status == ItemStatus.Concern)
                 .All(i => !string.IsNullOrWhiteSpace(i.Note))),
        new("nda", "NDA contact captured", !string.IsNullOrWhiteSpace(r.NdaContactEmail)),
        new("recommendation", "Recommendation written",
            !string.IsNullOrWhiteSpace(r.Recommendation)),
    };
}
