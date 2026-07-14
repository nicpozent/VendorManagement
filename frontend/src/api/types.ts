// Typed contract mirroring the .NET API DTOs.

export type Role = "ITManager" | "CioCto" | "Cfo";
export type ReviewStatus =
  | "Draft" | "InProgress" | "Concern" | "Approved" | "Rejected" | "Finished";
export type VerdictName =
  | "InProgress" | "Proceed" | "ProceedWithConditions" | "DoNotProceed";
export type ItemStatus = "Unscored" | "Pass" | "Concern" | "Blocker" | "NA";
export type Weight = "Low" | "Med" | "High";
export type SectionKind = "Fixed" | "Template";
export type Severity = "Concern" | "Blocker";
export type Nda = "None" | "Requested" | "Signed";
export type VendorStatus = "Approved" | "Rejected";

export interface Me {
  objectId: string;
  displayName: string;
  email: string | null;
  role: Role;
  isAdmin: boolean;
}

export interface Entity { id: string; name: string; }

export interface SectionItem {
  id: string; label: string; weight: Weight; selectable: boolean;
}
export interface Section {
  id: string; name: string; kind: SectionKind; items: SectionItem[];
}
export interface Category {
  id: string; name: string; includedSectionIds: string[];
}
export interface Policy {
  id: string; rule: string; sectionId: string; sectionName: string;
  severity: Severity; weight: Weight; active: boolean;
}

export interface Vendor {
  id: string; name: string; category: string; contactName: string;
  contactEmail: string; nda: Nda; status: VendorStatus;
  lastReview: string | null; rejectedOn: string | null;
  rejectedReason: string | null; ownerName: string | null;
}

export interface ReviewListItem {
  id: string; vendorName: string; categoryName: string; ownerName: string;
  status: ReviewStatus; entityId: string | null; entityName: string | null;
  blockers: number; concerns: number; updatedUtc: string; verdict: VerdictName;
}

export interface OpenQuestion { id: string; text: string; resolved: boolean; }
export interface ReviewItemScore {
  id: string; sectionItemId: string | null; label: string; weight: Weight;
  status: ItemStatus; note: string | null; mitigation: string | null;
}
export interface ReviewSectionScore {
  id: string; sectionId: string; sectionName: string; items: ReviewItemScore[];
}
export interface CompletenessCheck { key: string; label: string; ok: boolean; }
export interface VerdictResult {
  verdict: VerdictName; verdictLabel: string; verdictReason: string;
  readinessPct: number; blockerCount: number; concernCount: number;
  passCount: number; applicableCount: number; completeness: CompletenessCheck[];
}
export interface ReviewDetail {
  id: string; vendorName: string; productName: string; categoryId: string | null;
  categoryName: string; entityId: string | null; entityName: string | null;
  ownerName: string; ownerEmail: string | null; reviewRef: string; date: string;
  status: ReviewStatus; rawPitch: string; ndaContactName: string | null;
  ndaContactEmail: string | null; nda: Nda; recommendation: string | null;
  openQuestions: OpenQuestion[]; sections: ReviewSectionScore[];
  verdict: VerdictResult; updatedUtc: string;
}

export interface ScanSuggestion {
  field: string; label: string; value: string; rationale: string;
}
export interface ScanResult {
  suggestedProduct: string | null; suggestedCategoryName: string | null;
  suggestions: ScanSuggestion[]; detectedSignals: string[];
}

export interface CompareColumn {
  reviewId: string; vendorName: string; verdict: string; readinessPct: number;
}
export interface CompareCell { vendorReviewId: string; status: ItemStatus; }
export interface CompareRow {
  sectionName: string; itemLabel: string; cells: CompareCell[];
}
export interface CompareMatrix {
  categoryName: string; columns: CompareColumn[]; rows: CompareRow[];
}

export interface ArchiveItem {
  id: string; reviewId: string; vendorName: string; categoryName: string;
  ownerName: string; entityName: string | null; verdict: VerdictName;
  version: number; finishedOn: string;
}
export interface ArchiveDetail { header: ArchiveItem; memoMarkdown: string; }

export interface Settings { blockerCapsVerdict: boolean; }

export interface AppUser {
  id: string; entraObjectId: string; displayName: string; email: string | null;
  jobTitle: string | null; role: Role; sourceName: string | null;
  enabled: boolean; importedUtc: string;
}
export interface ImportSource {
  id: string; displayName: string; kind: string; description: string | null;
  memberCountHint: number | null;
}
export interface GraphMember {
  objectId: string; displayName: string; email: string | null; jobTitle: string | null;
}
export interface ImportResult {
  imported: number; updated: number; skipped: number;
  users: AppUser[]; warning: string | null;
}
export type ImportKind = "group" | "servicePrincipal" | "application";
