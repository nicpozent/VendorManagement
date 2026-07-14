using System.Text;
using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;

namespace VendorReview.Api.Services;

/// <summary>Renders the blocker-first technical review memo as Markdown.</summary>
public class MemoBuilder
{
    public const string Attribution =
        "Nick [Surname], Senior Manager, Global IT Infrastructure, Birgma International SA";

    public string Build(Review r, VerdictResultDto v, string? entityName)
    {
        var sb = new StringBuilder();
        var items = r.Sections.SelectMany(s => s.Items.Select(i => (s.SectionName, i))).ToList();

        sb.AppendLine($"# Technical Review — {r.VendorName}");
        sb.AppendLine();
        sb.AppendLine($"**{r.ProductName}**  ");
        sb.AppendLine($"Ref {r.ReviewRef} · {r.Date:d MMMM yyyy}"
            + (entityName is null ? "" : $" · {entityName}"));
        sb.AppendLine();

        sb.AppendLine($"## Verdict: {v.VerdictLabel}");
        sb.AppendLine();
        sb.AppendLine($"{v.VerdictReason}  ");
        sb.AppendLine($"Readiness **{v.ReadinessPct}%** · "
            + $"{v.BlockerCount} blocker(s) · {v.ConcernCount} concern(s) · {v.PassCount} pass.");
        sb.AppendLine();

        void Group(string heading, ItemStatus status, bool withWhy, bool withMitigation)
        {
            var group = items.Where(x => x.i.Status == status).ToList();
            if (group.Count == 0) return;
            sb.AppendLine($"## {heading}");
            sb.AppendLine();
            foreach (var (section, item) in group)
            {
                sb.AppendLine($"- **{item.Label}** — _{section}_");
                if (withWhy && !string.IsNullOrWhiteSpace(item.Note))
                    sb.AppendLine($"  - Why: {item.Note}");
                if (withMitigation && !string.IsNullOrWhiteSpace(item.Mitigation))
                    sb.AppendLine($"  - Unblock: {item.Mitigation}");
            }
            sb.AppendLine();
        }

        Group("Blockers", ItemStatus.Blocker, true, true);
        Group("Concerns", ItemStatus.Concern, true, true);
        Group("Acceptable", ItemStatus.Pass, false, false);

        var open = r.OpenQuestions.Where(q => !q.Resolved).ToList();
        if (open.Count > 0)
        {
            sb.AppendLine("## Open questions");
            sb.AppendLine();
            foreach (var q in open) sb.AppendLine($"- {q.Text}");
            sb.AppendLine();
        }

        sb.AppendLine("## Recommendation");
        sb.AppendLine();
        sb.AppendLine(string.IsNullOrWhiteSpace(r.Recommendation)
            ? "_No recommendation recorded._"
            : r.Recommendation);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine(Attribution);

        return sb.ToString();
    }
}
