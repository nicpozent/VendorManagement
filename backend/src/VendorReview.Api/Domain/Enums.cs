namespace VendorReview.Api.Domain;

public enum ItemWeight { Low, Med, High }

public enum SectionKind { Fixed, Template }

/// <summary>Per-rubric-item score inside a review.</summary>
public enum ItemStatus { Unscored, Pass, Concern, Blocker, NA }

public enum PolicySeverity { Concern, Blocker }

/// <summary>Lifecycle status of a review.</summary>
public enum ReviewStatus { Draft, InProgress, Concern, Approved, Rejected, Finished }

/// <summary>Computed recommendation returned by the verdict engine.</summary>
public enum Verdict { InProgress, Proceed, ProceedWithConditions, DoNotProceed }

public enum NdaStatus { None, Requested, Signed }

public enum VendorStatus { Approved, Rejected }

/// <summary>View-hint role. The API enforces real authorisation; the client only uses this for display.</summary>
public enum AppRole { ITManager, CioCto, Cfo }
