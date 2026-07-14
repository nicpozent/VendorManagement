using VendorReview.Api.Domain;

namespace VendorReview.Api.Dtos;

// ---- Catalog ----
public record EntityDto(Guid Id, string Name);
public record EntityUpsertDto(string Name);

public record SectionItemDto(Guid Id, string Label, string Weight, bool Selectable);
public record SectionDto(Guid Id, string Name, string Kind, List<SectionItemDto> Items);

public record CategoryDto(Guid Id, string Name, List<Guid> IncludedSectionIds);

public record PolicyDto(Guid Id, string Rule, Guid SectionId, string SectionName,
    string Severity, string Weight, bool Active);

// ---- Vendors ----
public record VendorDto(Guid Id, string Name, string Category, string ContactName,
    string ContactEmail, string Nda, string Status, DateOnly? LastReview,
    DateOnly? RejectedOn, string? RejectedReason, string? OwnerName);

public record ContactUpdateDto(string ContactName, string ContactEmail);
public record SendNdaResultDto(string Message, string CcTo);

// ---- Reviews ----
public record ReviewListItemDto(Guid Id, string VendorName, string CategoryName,
    string OwnerName, string Status, Guid? EntityId, string? EntityName,
    int Blockers, int Concerns, DateTime UpdatedUtc, string Verdict);

public record OpenQuestionDto(Guid Id, string Text, bool Resolved);

public record ReviewItemScoreDto(Guid Id, Guid? SectionItemId, string Label, string Weight,
    string Status, string? Note, string? Mitigation);

public record ReviewSectionScoreDto(Guid Id, Guid SectionId, string SectionName,
    List<ReviewItemScoreDto> Items);

public record CompletenessCheckDto(string Key, string Label, bool Ok);

public record VerdictResultDto(string Verdict, string VerdictLabel, string VerdictReason,
    int ReadinessPct, int BlockerCount, int ConcernCount, int PassCount, int ApplicableCount,
    List<CompletenessCheckDto> Completeness);

public record MemoBlockDto(string Section, List<string> Note, List<string> Mitigation);

public record ReviewDetailDto(
    Guid Id, string VendorName, string ProductName, Guid? CategoryId, string CategoryName,
    Guid? EntityId, string? EntityName, string OwnerName, string? OwnerEmail, string ReviewRef,
    DateOnly Date, string Status, string RawPitch, string? NdaContactName, string? NdaContactEmail,
    string Nda, string? Recommendation, List<OpenQuestionDto> OpenQuestions,
    List<ReviewSectionScoreDto> Sections, VerdictResultDto Verdict, DateTime UpdatedUtc);

public record ReviewCreateDto(string VendorName, string ProductName, Guid? CategoryId,
    Guid? EntityId, string? OwnerName);

public record ReviewUpdateDto(
    string? VendorName, string? ProductName, Guid? CategoryId, Guid? EntityId,
    string? OwnerName, string? OwnerEmail, string? ReviewRef, DateOnly? Date,
    string? RawPitch, string? NdaContactName, string? NdaContactEmail, string? Nda,
    string? Recommendation, string? Status, List<OpenQuestionDto>? OpenQuestions,
    List<ReviewSectionScoreDto>? Sections);

// ---- Scan (deterministic assisted intake) ----
public record ScanSuggestionDto(string Field, string Label, string Value, string Rationale);
public record ScanResultDto(string? SuggestedProduct, string? SuggestedCategoryName,
    List<ScanSuggestionDto> Suggestions, List<string> DetectedSignals);

// ---- Compare ----
public record CompareCellDto(Guid VendorReviewId, string Status);
public record CompareRowDto(string SectionName, string ItemLabel, List<CompareCellDto> Cells);
public record CompareColumnDto(Guid ReviewId, string VendorName, string Verdict, int ReadinessPct);
public record CompareMatrixDto(string CategoryName, List<CompareColumnDto> Columns,
    List<CompareRowDto> Rows);

// ---- Archive ----
public record ArchiveItemDto(Guid Id, Guid ReviewId, string VendorName, string CategoryName,
    string OwnerName, string? EntityName, string Verdict, int Version, DateOnly FinishedOn);
public record ArchiveDetailDto(ArchiveItemDto Header, string MemoMarkdown);

// ---- Settings ----
public record SettingsDto(bool BlockerCapsVerdict);

// ---- Admin / Entra import ----
public record AppUserDto(Guid Id, string EntraObjectId, string DisplayName, string? Email,
    string? JobTitle, string Role, string? SourceName, bool Enabled, DateTime ImportedUtc);

public record ImportSourceDto(string Id, string DisplayName, string Kind, string? Description,
    int? MemberCountHint);

public record ImportRequestDto(string SourceId, string SourceKind, string SourceName,
    string DefaultRole);

public record ImportResultDto(int Imported, int Updated, int Skipped, List<AppUserDto> Users,
    string? Warning);

public record UserRoleUpdateDto(string Role, bool Enabled);

// ---- Me ----
public record MeDto(string ObjectId, string DisplayName, string? Email, string Role, bool IsAdmin);
