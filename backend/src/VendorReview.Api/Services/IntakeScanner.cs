using System.Text.RegularExpressions;
using VendorReview.Api.Data;
using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace VendorReview.Api.Services;

/// <summary>
/// Deterministic "Scan &amp; suggest" assisted intake. Purely rule-based keyword
/// detection over the raw pitch — no model calls, fully reproducible.
/// </summary>
public class IntakeScanner
{
    private readonly AppDbContext _db;
    public IntakeScanner(AppDbContext db) => _db = db;

    private static readonly (string Signal, string[] Keywords)[] SignalMap =
    {
        ("Cloud / SaaS hosting", new[] { "saas", "cloud", "multi-tenant", "hosted", "aws", "azure", "gcp" }),
        ("Data residency (EU/EEA)", new[] { "eu", "eea", "gdpr", "residency", "data center", "datacentre", "frankfurt", "stockholm" }),
        ("Single sign-on", new[] { "sso", "saml", "oidc", "scim", "entra", "okta", "azure ad" }),
        ("Certifications", new[] { "soc 2", "soc2", "iso 27001", "iso27001", "iso 27701", "pci" }),
        ("Sub-processors / DPA", new[] { "sub-processor", "subprocessor", "dpa", "data processing agreement" }),
        ("Encryption", new[] { "encryption", "encrypted", "tls", "aes-256", "at rest", "in transit" }),
        ("Export / portability", new[] { "export", "api", "portability", "bulk export", "data extract" }),
        ("Support & SLA", new[] { "sla", "uptime", "99.9", "support", "24/7" }),
    };

    public async Task<ScanResultDto> Scan(string rawPitch, CancellationToken ct)
    {
        var text = (rawPitch ?? "").ToLowerInvariant();
        var detected = new List<string>();
        var suggestions = new List<ScanSuggestionDto>();

        foreach (var (signal, keywords) in SignalMap)
        {
            var hit = keywords.FirstOrDefault(k => text.Contains(k));
            if (hit is null) continue;
            detected.Add(signal);
            suggestions.Add(new ScanSuggestionDto("section", signal, "Attention needed",
                $"Pitch mentions \"{hit}\" — confirm coverage in the relevant section."));
        }

        // Product name: first capitalised phrase or first line.
        string? product = null;
        var firstLine = (rawPitch ?? "").Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
        if (!string.IsNullOrWhiteSpace(firstLine) && firstLine!.Length <= 120)
            product = firstLine;

        // Category guess: match a known category by keyword overlap.
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct);
        string? categoryGuess = null;
        if (text.Contains("saas") || text.Contains("cloud"))
            categoryGuess = categories.FirstOrDefault(c => c.Name.Contains("Cloud", StringComparison.OrdinalIgnoreCase))?.Name;
        else if (text.Contains("identity") || text.Contains("sso") || text.Contains("idp"))
            categoryGuess = categories.FirstOrDefault(c => c.Name.Contains("Identity", StringComparison.OrdinalIgnoreCase))?.Name;

        // Email → NDA contact suggestion.
        var email = Regex.Match(rawPitch ?? "", @"[\w.+-]+@[\w-]+\.[\w.-]+");
        if (email.Success)
            suggestions.Add(new ScanSuggestionDto("ndaContactEmail", "NDA contact email",
                email.Value, "Detected a contact email in the pitch."));

        if (categoryGuess is not null)
            suggestions.Insert(0, new ScanSuggestionDto("categoryName", "Vendor category",
                categoryGuess, "Inferred from hosting/identity signals in the pitch."));
        if (product is not null)
            suggestions.Insert(0, new ScanSuggestionDto("productName", "Product / proposal",
                product, "Taken from the first line of the pitch."));

        return new ScanResultDto(product, categoryGuess, suggestions, detected);
    }
}
