using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Data;
using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;
using VendorReview.Api.Services;

namespace VendorReview.Api.Endpoints;

public static class CompareArchiveEndpoints
{
    public static void MapCompare(this IEndpointRouteBuilder app)
    {
        // Side-by-side matrix for one category. Columns = reviews (vendors) in the
        // category; rows = rubric items grouped by section; cells = per-item status.
        app.MapGet("/compare", async (Guid? categoryId, AppDbContext db, VerdictEngine engine,
            CancellationToken ct) =>
        {
            if (categoryId is null) return Results.BadRequest(new { message = "categoryId is required." });
            var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
            if (cat is null) return Results.NotFound();

            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var reviews = await db.Reviews
                .Include(r => r.Sections).ThenInclude(s => s.Items)
                .Where(r => r.CategoryId == categoryId)
                .OrderBy(r => r.VendorName)
                .ToListAsync(ct);

            var sections = await db.Sections.Include(s => s.Items)
                .Where(s => cat.IncludedSectionIds.Contains(s.Id))
                .OrderBy(s => s.SortOrder).ToListAsync(ct);

            var columns = reviews.Select(r =>
            {
                var v = engine.Evaluate(r, caps);
                // Compare "Verdict" row shows the review's lifecycle status pill.
                return new CompareColumnDto(r.Id, r.VendorName, r.Status.ToString(), v.ReadinessPct);
            }).ToList();

            var rows = new List<CompareRowDto>();
            foreach (var s in sections)
                foreach (var item in s.Items.OrderBy(i => i.SortOrder))
                {
                    var cells = reviews.Select(r =>
                    {
                        var score = r.Sections.SelectMany(x => x.Items)
                            .FirstOrDefault(i => i.SectionItemId == item.Id || i.Label == item.Label);
                        return new CompareCellDto(r.Id, (score?.Status ?? ItemStatus.NA).ToString());
                    }).ToList();
                    rows.Add(new CompareRowDto(s.Name, item.Label, cells));
                }

            return Results.Ok(new CompareMatrixDto(cat.Name, columns, rows));
        }).RequireAuthorization();
    }

    public static void MapArchive(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/archive").RequireAuthorization();

        g.MapGet("", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.ArchivedReviews.OrderByDescending(a => a.FinishedOn).ThenBy(a => a.Version).ToListAsync(ct))
                .Select(a => a.ToDto()).ToList()));

        g.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var a = await db.ArchivedReviews.FindAsync(new object?[] { id }, ct);
            return a is null ? Results.NotFound() : Results.Ok(new ArchiveDetailDto(a.ToDto(), a.MemoMarkdown));
        });
    }
}
