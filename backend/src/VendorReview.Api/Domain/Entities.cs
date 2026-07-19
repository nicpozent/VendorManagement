using System.ComponentModel.DataAnnotations;

namespace VendorReview.Api.Domain;

/// <summary>A Birgma group business used to tag and filter reviews (e.g. Biltema Sweden).</summary>
public class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}

/// <summary>Vendor category (e.g. Cloud / SaaS). Drives which review sections apply.</summary>
public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    /// <summary>Ids of the review sections included for vendors in this category.</summary>
    public List<Guid> IncludedSectionIds { get; set; } = new();
}

/// <summary>A rubric section (fixed items, or a template with selectable options).</summary>
public class Section
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public string Name { get; set; } = "";
    public SectionKind Kind { get; set; }
    public int SortOrder { get; set; }
    public List<SectionItem> Items { get; set; } = new();
}

public class SectionItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SectionId { get; set; }
    public Section? Section { get; set; }
    [MaxLength(300)] public string Label { get; set; } = "";
    public ItemWeight Weight { get; set; } = ItemWeight.Med;
    /// <summary>For template sections: whether this option is picked into a review by default.</summary>
    public bool Selectable { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Weighted policy-library rule mapped to a section.</summary>
public class Policy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(400)] public string Rule { get; set; } = "";
    public Guid SectionId { get; set; }
    public Section? Section { get; set; }
    public PolicySeverity Severity { get; set; }
    public ItemWeight Weight { get; set; } = ItemWeight.High;
    public bool Active { get; set; } = true;
    public int SortOrder { get; set; }
}

public class Vendor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(200)] public string Category { get; set; } = "";
    [MaxLength(200)] public string ContactName { get; set; } = "";
    [MaxLength(320)] public string ContactEmail { get; set; } = "";
    public NdaStatus Nda { get; set; } = NdaStatus.None;
    public VendorStatus Status { get; set; } = VendorStatus.Approved;
    public DateOnly? LastReview { get; set; }
    public DateOnly? RejectedOn { get; set; }
    [MaxLength(600)] public string? RejectedReason { get; set; }
    [MaxLength(200)] public string? OwnerName { get; set; }
}

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public string VendorName { get; set; } = "";
    [MaxLength(300)] public string ProductName { get; set; } = "";
    public Guid? CategoryId { get; set; }
    [MaxLength(200)] public string CategoryName { get; set; } = "";
    public Guid? EntityId { get; set; }
    public Entity? Entity { get; set; }
    [MaxLength(200)] public string OwnerName { get; set; } = "";
    /// <summary>Entra object id of the owning manager; used for reminders and manager scoping.</summary>
    [MaxLength(100)] public string? OwnerObjectId { get; set; }
    [MaxLength(320)] public string? OwnerEmail { get; set; }
    [MaxLength(80)] public string ReviewRef { get; set; } = "";
    public DateOnly Date { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Draft;
    public string RawPitch { get; set; } = "";
    [MaxLength(320)] public string? NdaContactName { get; set; }
    [MaxLength(320)] public string? NdaContactEmail { get; set; }
    public NdaStatus Nda { get; set; } = NdaStatus.None;
    [MaxLength(2000)] public string? Recommendation { get; set; }
    public List<OpenQuestion> OpenQuestions { get; set; } = new();
    public List<ReviewSectionScore> Sections { get; set; } = new();
    /// <summary>Approver object ids notified for sign-off; used by the reminder service.</summary>
    public List<string> ApproverObjectIds { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastReminderUtc { get; set; }
}

public class ReviewSectionScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReviewId { get; set; }
    public Guid SectionId { get; set; }
    [MaxLength(200)] public string SectionName { get; set; } = "";
    public int SortOrder { get; set; }
    public List<ReviewItemScore> Items { get; set; } = new();
}

public class ReviewItemScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReviewSectionScoreId { get; set; }
    public Guid? SectionItemId { get; set; }
    [MaxLength(300)] public string Label { get; set; } = "";
    public ItemWeight Weight { get; set; } = ItemWeight.Med;
    public ItemStatus Status { get; set; } = ItemStatus.Unscored;
    /// <summary>"Why" for a concern/blocker.</summary>
    [MaxLength(2000)] public string? Note { get; set; }
    /// <summary>Unblock path / mitigation.</summary>
    [MaxLength(2000)] public string? Mitigation { get; set; }
    public int SortOrder { get; set; }
}

public class OpenQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReviewId { get; set; }
    [MaxLength(600)] public string Text { get; set; } = "";
    public bool Resolved { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Immutable finished-review snapshot (Archive).</summary>
public class ArchivedReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReviewId { get; set; }
    [MaxLength(200)] public string VendorName { get; set; } = "";
    [MaxLength(200)] public string CategoryName { get; set; } = "";
    [MaxLength(200)] public string OwnerName { get; set; } = "";
    public Guid? EntityId { get; set; }
    [MaxLength(200)] public string? EntityName { get; set; }
    public Verdict Verdict { get; set; }
    public int Version { get; set; }
    public DateOnly FinishedOn { get; set; }
    /// <summary>Rendered memo markdown captured at finish time.</summary>
    public string MemoMarkdown { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Append-only audit record of a security-sensitive action (sign-off, finish,
/// NDA/reminder mail, directory import, role change, settings change). Written by
/// <see cref="Services.AuditLog"/>; never updated or deleted through the API.
/// </summary>
public class AuditEvent
{
    public long Id { get; set; }
    public DateTime Utc { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string ActorObjectId { get; set; } = "";
    [MaxLength(200)] public string ActorName { get; set; } = "";
    [MaxLength(40)] public string ActorRole { get; set; } = "";
    /// <summary>Dotted action key, e.g. "review.signoff", "vendor.nda.send".</summary>
    [MaxLength(80)] public string Action { get; set; } = "";
    [MaxLength(60)] public string TargetType { get; set; } = "";
    [MaxLength(100)] public string? TargetId { get; set; }
    [MaxLength(300)] public string? TargetName { get; set; }
    [MaxLength(600)] public string? Summary { get; set; }
}

/// <summary>Application setting (single-row config lives here as key/value).</summary>
public class Setting
{
    [Key][MaxLength(100)] public string Key { get; set; } = "";
    [MaxLength(1000)] public string Value { get; set; } = "";
}

/// <summary>A user imported from an Entra group / enterprise app / app registration.</summary>
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public string EntraObjectId { get; set; } = "";
    [MaxLength(200)] public string DisplayName { get; set; } = "";
    [MaxLength(320)] public string? Email { get; set; }
    [MaxLength(200)] public string? JobTitle { get; set; }
    public AppRole Role { get; set; } = AppRole.ITManager;
    /// <summary>How this user was imported (group / app display name).</summary>
    [MaxLength(300)] public string? SourceName { get; set; }
    [MaxLength(100)] public string? SourceId { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime ImportedUtc { get; set; } = DateTime.UtcNow;
}
