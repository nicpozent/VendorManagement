using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Domain;

namespace VendorReview.Api.Data;

/// <summary>
/// Seeds the catalog plus a sample portfolio that mirrors the reference screenshots.
/// The client hard-codes nothing; everything is served from here. "Reset sample data"
/// (Configuration ▸ Settings) calls <see cref="ResetAsync"/>.
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Sections.AnyAsync(ct)) return;
        await ResetAsync(db, ct);
    }

    public static async Task ResetAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Wipe tool data (keep imported AppUsers — those are provisioned from Entra).
        db.ReviewItemScores.RemoveRange(db.ReviewItemScores);
        db.ReviewSectionScores.RemoveRange(db.ReviewSectionScores);
        db.OpenQuestions.RemoveRange(db.OpenQuestions);
        db.Reviews.RemoveRange(db.Reviews);
        db.ArchivedReviews.RemoveRange(db.ArchivedReviews);
        db.Vendors.RemoveRange(db.Vendors);
        db.Policies.RemoveRange(db.Policies);
        db.SectionItems.RemoveRange(db.SectionItems);
        db.Sections.RemoveRange(db.Sections);
        db.Categories.RemoveRange(db.Categories);
        db.Entities.RemoveRange(db.Entities);
        await db.SaveChangesAsync(ct);

        // ---- Entities ----
        string[] entityNames =
        {
            "Birgma International SA", "Biltema Sweden", "Biltema Norway",
            "Biltema Finland", "Biltema Denmark",
        };
        var entities = entityNames.Select((n, i) => new Entity { Name = n, SortOrder = i }).ToList();
        db.Entities.AddRange(entities);

        // ---- Sections + items ----
        var arch = Section("Architecture & hosting", SectionKind.Fixed, 0,
            ("Hosting & tenancy model", ItemWeight.High, false),
            ("Resilience & disaster recovery", ItemWeight.High, false));
        var privacy = Section("Privacy & data regulations", SectionKind.Template, 1,
            ("GDPR (EU)", ItemWeight.High, true),
            ("Data residency (EU/EEA)", ItemWeight.High, true),
            ("DPA & sub-processor list", ItemWeight.High, true));
        var security = Section("Security & identity", SectionKind.Fixed, 2,
            ("SSO via SAML 2.0 or OIDC", ItemWeight.High, false),
            ("SOC 2 Type II or ISO 27001", ItemWeight.High, false),
            ("Encryption at rest & in transit", ItemWeight.Med, false));
        var data = Section("Data & portability", SectionKind.Fixed, 3,
            ("Documented bulk-export API", ItemWeight.Med, false));
        var sections = new[] { arch, privacy, security, data };
        db.Sections.AddRange(sections);

        // ---- Categories ----
        var cloud = new Category
        {
            Name = "Cloud / SaaS", SortOrder = 0,
            IncludedSectionIds = new() { arch.Id, privacy.Id, security.Id, data.Id }
        };
        var idp = new Category
        {
            Name = "Identity provider", SortOrder = 1,
            IncludedSectionIds = new() { security.Id, privacy.Id }
        };
        var mdm = new Category
        {
            Name = "Endpoint / MDM", SortOrder = 2,
            IncludedSectionIds = new() { security.Id, arch.Id }
        };
        db.Categories.AddRange(cloud, idp, mdm);

        // ---- Policy library ----
        db.Policies.AddRange(
            Policy("Customer & personal data must stay in the EU/EEA", privacy, PolicySeverity.Blocker, ItemWeight.High, 0),
            Policy("Signed DPA and current sub-processor list on file", privacy, PolicySeverity.Blocker, ItemWeight.High, 1),
            Policy("SSO via SAML 2.0 or OIDC — no local passwords", security, PolicySeverity.Blocker, ItemWeight.High, 2),
            Policy("SOC 2 Type II or ISO 27001 certification current", security, PolicySeverity.Blocker, ItemWeight.High, 3),
            Policy("Documented bulk-export API for exit / portability", data, PolicySeverity.Concern, ItemWeight.Med, 4),
            Policy("Encryption at rest and in transit (TLS 1.2+/AES-256)", security, PolicySeverity.Concern, ItemWeight.Med, 5));

        // ---- Vendors ----
        db.Vendors.AddRange(
            new Vendor { Name = "Helios IdP", Category = "Identity provider", ContactName = "Anna Lindqvist", ContactEmail = "anna.lindqvist@helios-id.com", Nda = NdaStatus.Signed, Status = VendorStatus.Approved, LastReview = new(2026, 6, 29), OwnerName = "Priya [Surname]" },
            new Vendor { Name = "Vault Archive DMS", Category = "Cloud / SaaS", ContactName = "Tomas Berg", ContactEmail = "tomas.berg@vaultarchive.eu", Nda = NdaStatus.Signed, Status = VendorStatus.Approved, LastReview = new(2026, 6, 2), OwnerName = "Nick [Surname]" },
            new Vendor { Name = "Atlas Endpoint MDM", Category = "Identity provider", ContactName = "Sofia Maric", ContactEmail = "sofia.maric@atlasmdm.com", Nda = NdaStatus.Requested, Status = VendorStatus.Approved, LastReview = new(2026, 6, 29), OwnerName = "Nick [Surname]" },
            new Vendor { Name = "Cobalt CRM", Category = "Cloud / SaaS", ContactName = "Marcus Vogel", ContactEmail = "m.vogel@cobaltcrm.com", Nda = NdaStatus.None, Status = VendorStatus.Rejected, RejectedOn = new(2026, 5, 12), RejectedReason = "Personal data stored in us-east with no EU residency option.", OwnerName = "Nick [Surname]" },
            new Vendor { Name = "Zephyr Mail", Category = "Cloud / SaaS", ContactName = "Lena Poulsen", ContactEmail = "lena@zephyrmail.io", Nda = NdaStatus.None, Status = VendorStatus.Rejected, RejectedOn = new(2026, 4, 28), RejectedReason = "No SSO; local passwords only. No SOC 2 / ISO 27001.", OwnerName = "Priya [Surname]" });

        // ---- Reviews (portfolio) ----
        var eSweden = entities[1]; var eNorway = entities[2]; var eFinland = entities[3];
        var eBirgma = entities[0]; var eDenmark = entities[4];

        db.Reviews.AddRange(
            Review("Northwind Cloud Suite", "Northwind DMS — document & contract management",
                cloud, eSweden, "Nick [Surname]", "nick@birgma.dev", "TR-2026-047", new(2026, 6, 29),
                ReviewStatus.InProgress, NdaStatus.Signed,
                rec: "Do not proceed until EU data residency is contractually guaranteed and the current sub-processor list is provided.",
                scores: new()
                {
                    (arch, "Hosting & tenancy model", ItemStatus.Pass, null, null),
                    (arch, "Resilience & disaster recovery", ItemStatus.Pass, null, null),
                    (privacy, "Data residency (EU/EEA)", ItemStatus.Blocker, "Primary region is us-east-1; no EU residency commitment.", null),
                    (privacy, "DPA & sub-processor list", ItemStatus.Concern, "DPA provided but sub-processor list is 8 months old.", "Request current signed list before sign-off."),
                },
                questions: new() { "Can data residency be pinned to eu-central (Frankfurt)?", "Latest sub-processor list date?" }),

            Review("Meridian ERP", "Meridian ERP — finance & operations",
                cloud, eNorway, "Priya [Surname]", "priya@birgma.dev", "TR-2026-051", new(2026, 6, 24),
                ReviewStatus.Concern, NdaStatus.Signed,
                rec: "Proceed with conditions; resolve export API concern before go-live.",
                scores: new()
                {
                    (arch, "Hosting & tenancy model", ItemStatus.Pass, null, null),
                    (privacy, "Data residency (EU/EEA)", ItemStatus.Pass, null, null),
                    (data, "Documented bulk-export API", ItemStatus.Concern, "Export is CSV-only and manual.", "Vendor to expose REST export by Q4."),
                },
                questions: new() { "Timeline for the documented export API?" }),

            Review("Orbit Analytics", "Orbit — product analytics",
                cloud, eBirgma, "Nick [Surname]", "nick@birgma.dev", "TR-2026-052", new(2026, 7, 1),
                ReviewStatus.Draft, NdaStatus.None, rec: null,
                scores: new(), questions: new()),

            Review("Vault DMS", "Vault — records & retention",
                cloud, eFinland, "Nick [Surname]", "nick@birgma.dev", "TR-2026-039", new(2026, 6, 4),
                ReviewStatus.Finished, NdaStatus.Signed,
                rec: "Proceed with conditions; retention policy documented and DPA signed.",
                scores: new()
                {
                    (arch, "Hosting & tenancy model", ItemStatus.Pass, null, null),
                    (arch, "Resilience & disaster recovery", ItemStatus.Pass, null, null),
                    (privacy, "Data residency (EU/EEA)", ItemStatus.Pass, null, null),
                    (privacy, "DPA & sub-processor list", ItemStatus.Concern, "Signed DPA; two sub-processors outside EEA with SCCs.", "Accepted with standard contractual clauses."),
                },
                questions: new()),

            Review("Helios IdP", "Helios — identity provider",
                idp, eBirgma, "Priya [Surname]", "priya@birgma.dev", "TR-2026-044", new(2026, 6, 29),
                ReviewStatus.Approved, NdaStatus.Signed,
                rec: "Proceed. Meets SSO, certification and data-residency requirements.",
                scores: new()
                {
                    (security, "SSO via SAML 2.0 or OIDC", ItemStatus.Pass, null, null),
                    (security, "SOC 2 Type II or ISO 27001", ItemStatus.Pass, null, null),
                    (privacy, "Data residency (EU/EEA)", ItemStatus.Pass, null, null),
                },
                questions: new()),

            Review("Atlas Endpoint MDM", "Atlas — endpoint management",
                idp, eDenmark, "Nick [Surname]", "nick@birgma.dev", "TR-2026-046", new(2026, 6, 29),
                ReviewStatus.Approved, NdaStatus.Requested,
                rec: "Proceed. Certification current; NDA in flight.",
                scores: new()
                {
                    (security, "SSO via SAML 2.0 or OIDC", ItemStatus.Pass, null, null),
                    (security, "SOC 2 Type II or ISO 27001", ItemStatus.Pass, null, null),
                },
                questions: new()),

            Review("Cobalt CRM", "Cobalt — sales CRM",
                cloud, eSweden, "Nick [Surname]", "nick@birgma.dev", "TR-2026-031", new(2026, 5, 12),
                ReviewStatus.Rejected, NdaStatus.None,
                rec: "Do not proceed. No EU residency option for personal data.",
                scores: new()
                {
                    (privacy, "Data residency (EU/EEA)", ItemStatus.Blocker, "Personal data stored in us-east; no EU option.", null),
                    (security, "SOC 2 Type II or ISO 27001", ItemStatus.Pass, null, null),
                },
                questions: new()),

            Review("Zephyr Mail", "Zephyr — team email",
                cloud, eNorway, "Priya [Surname]", "priya@birgma.dev", "TR-2026-028", new(2026, 4, 28),
                ReviewStatus.Rejected, NdaStatus.None,
                rec: "Do not proceed. No SSO and no current certification.",
                scores: new()
                {
                    (security, "SSO via SAML 2.0 or OIDC", ItemStatus.Blocker, "Local passwords only; no SAML/OIDC.", null),
                    (security, "SOC 2 Type II or ISO 27001", ItemStatus.Concern, "SOC 2 audit in progress, not complete.", null),
                },
                questions: new()));

        // ---- Archive (immutable finished snapshots) ----
        db.ArchivedReviews.AddRange(
            new ArchivedReview { VendorName = "Vault Archive DMS", CategoryName = "Cloud / SaaS", OwnerName = "Nick [Surname]", EntityName = "Biltema Finland", Verdict = Verdict.DoNotProceed, Version = 1, FinishedOn = new(2026, 5, 20), MemoMarkdown = "# Technical Review — Vault Archive DMS\n\n## Verdict: Do not proceed\n\n1 unresolved blocker caps this review." },
            new ArchivedReview { VendorName = "Vault Archive DMS", CategoryName = "Cloud / SaaS", OwnerName = "Nick [Surname]", EntityName = "Biltema Finland", Verdict = Verdict.ProceedWithConditions, Version = 2, FinishedOn = new(2026, 6, 4), MemoMarkdown = "# Technical Review — Vault Archive DMS\n\n## Verdict: Proceed with conditions\n\nAcceptable subject to 1 concern." });

        await SettingsRepo.SetBlockerCapsVerdict(db, true, ct);
        await db.SaveChangesAsync(ct);
    }

    // ---- builders ----
    private static Section Section(string name, SectionKind kind, int order,
        params (string label, ItemWeight weight, bool selectable)[] items)
    {
        var s = new Section { Name = name, Kind = kind, SortOrder = order };
        int i = 0;
        foreach (var (label, weight, selectable) in items)
            s.Items.Add(new SectionItem { Label = label, Weight = weight, Selectable = selectable, SortOrder = i++ });
        return s;
    }

    private static Policy Policy(string rule, Section section, PolicySeverity sev, ItemWeight w, int order) =>
        new() { Rule = rule, SectionId = section.Id, Severity = sev, Weight = w, SortOrder = order, Active = true };

    private static Review Review(string vendor, string product, Category cat, Entity entity,
        string owner, string ownerEmail, string reviewRef, DateOnly date, ReviewStatus status,
        NdaStatus nda, string? rec,
        List<(Section section, string label, ItemStatus st, string? note, string? mit)> scores,
        List<string> questions)
    {
        var r = new Review
        {
            VendorName = vendor, ProductName = product, CategoryId = cat.Id, CategoryName = cat.Name,
            EntityId = entity.Id, OwnerName = owner, OwnerEmail = ownerEmail, OwnerObjectId = "mock-" + owner.Split(' ')[0].ToLowerInvariant(),
            ReviewRef = reviewRef, Date = date, Status = status, Nda = nda, Recommendation = rec,
            RawPitch = product + ". " + (rec ?? ""),
            UpdatedUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        };
        int qi = 0;
        foreach (var q in questions) r.OpenQuestions.Add(new OpenQuestion { Text = q, SortOrder = qi++ });

        foreach (var group in scores.GroupBy(s => s.section))
        {
            var ss = new ReviewSectionScore { SectionId = group.Key.Id, SectionName = group.Key.Name, SortOrder = group.Key.SortOrder };
            int ii = 0;
            foreach (var (section, label, st, note, mit) in group)
            {
                var item = section.Items.FirstOrDefault(x => x.Label == label);
                ss.Items.Add(new ReviewItemScore
                {
                    SectionItemId = item?.Id, Label = label, Weight = item?.Weight ?? ItemWeight.Med,
                    Status = st, Note = note, Mitigation = mit, SortOrder = ii++,
                });
            }
            r.Sections.Add(ss);
        }
        return r;
    }
}
