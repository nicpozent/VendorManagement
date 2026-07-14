using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;

namespace VendorReview.Api.Services;

/// <summary>Domain → DTO projections shared by the endpoints.</summary>
public static class Mapping
{
    public static EntityDto ToDto(this Entity e) => new(e.Id, e.Name);

    public static SectionItemDto ToDto(this SectionItem i) =>
        new(i.Id, i.Label, i.Weight.ToString(), i.Selectable);

    public static SectionDto ToDto(this Section s) =>
        new(s.Id, s.Name, s.Kind.ToString(),
            s.Items.OrderBy(i => i.SortOrder).Select(i => i.ToDto()).ToList());

    public static CategoryDto ToDto(this Category c) =>
        new(c.Id, c.Name, c.IncludedSectionIds);

    public static PolicyDto ToDto(this Policy p) =>
        new(p.Id, p.Rule, p.SectionId, p.Section?.Name ?? "",
            p.Severity.ToString(), p.Weight.ToString(), p.Active);

    public static VendorDto ToDto(this Vendor v) =>
        new(v.Id, v.Name, v.Category, v.ContactName, v.ContactEmail, v.Nda.ToString(),
            v.Status.ToString(), v.LastReview, v.RejectedOn, v.RejectedReason, v.OwnerName);

    public static OpenQuestionDto ToDto(this OpenQuestion q) => new(q.Id, q.Text, q.Resolved);

    public static ReviewItemScoreDto ToDto(this ReviewItemScore i) =>
        new(i.Id, i.SectionItemId, i.Label, i.Weight.ToString(), i.Status.ToString(),
            i.Note, i.Mitigation);

    public static ReviewSectionScoreDto ToDto(this ReviewSectionScore s) =>
        new(s.Id, s.SectionId, s.SectionName,
            s.Items.OrderBy(i => i.SortOrder).Select(i => i.ToDto()).ToList());

    public static ReviewListItemDto ToListItem(this Review r, VerdictResultDto v) =>
        new(r.Id, r.VendorName, r.CategoryName, r.OwnerName, r.Status.ToString(),
            r.EntityId, r.Entity?.Name, v.BlockerCount, v.ConcernCount, r.UpdatedUtc,
            v.Verdict);

    public static ReviewDetailDto ToDetail(this Review r, VerdictResultDto v) =>
        new(r.Id, r.VendorName, r.ProductName, r.CategoryId, r.CategoryName, r.EntityId,
            r.Entity?.Name, r.OwnerName, r.OwnerEmail, r.ReviewRef, r.Date, r.Status.ToString(),
            r.RawPitch, r.NdaContactName, r.NdaContactEmail, r.Nda.ToString(), r.Recommendation,
            r.OpenQuestions.OrderBy(q => q.SortOrder).Select(q => q.ToDto()).ToList(),
            r.Sections.OrderBy(s => s.SortOrder).Select(s => s.ToDto()).ToList(),
            v, r.UpdatedUtc);

    public static AppUserDto ToDto(this AppUser u) =>
        new(u.Id, u.EntraObjectId, u.DisplayName, u.Email, u.JobTitle, u.Role.ToString(),
            u.SourceName, u.Enabled, u.ImportedUtc);

    public static ArchiveItemDto ToDto(this ArchivedReview a) =>
        new(a.Id, a.ReviewId, a.VendorName, a.CategoryName, a.OwnerName, a.EntityName,
            a.Verdict.ToString(), a.Version, a.FinishedOn);

    public static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var r) ? r : fallback;
}
