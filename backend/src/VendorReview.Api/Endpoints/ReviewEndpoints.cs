using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Auth;
using VendorReview.Api.Data;
using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;
using VendorReview.Api.Services;

namespace VendorReview.Api.Endpoints;

public static class ReviewEndpoints
{
    public static void MapReviews(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/reviews").RequireAuthorization();

        g.MapGet("", async (string? status, Guid? categoryId, Guid? entityId,
            AppDbContext db, CurrentUser me, VerdictEngine engine, CancellationToken ct) =>
        {
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var q = db.Reviews.Include(r => r.Entity).Include(r => r.Sections).ThenInclude(s => s.Items).AsQueryable();

            // Role scoping: managers see only their own; leadership sees the whole portfolio.
            if (!me.IsLeadership)
                q = q.Where(r => r.OwnerObjectId == me.ObjectId || r.OwnerName == me.DisplayName);

            if (categoryId is not null) q = q.Where(r => r.CategoryId == categoryId);
            if (entityId is not null) q = q.Where(r => r.EntityId == entityId);
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReviewStatus>(status, true, out var st))
                q = q.Where(r => r.Status == st);

            var list = await q.OrderByDescending(r => r.UpdatedUtc).ToListAsync(ct);
            return Results.Ok(list.Select(r => r.ToListItem(engine.Evaluate(r, caps))).ToList());
        });

        g.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CurrentUser me,
            VerdictEngine engine, CancellationToken ct) =>
        {
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            if (!me.IsLeadership && r.OwnerObjectId != me.ObjectId && r.OwnerName != me.DisplayName)
                return Results.Forbid();
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            return Results.Ok(r.ToDetail(engine.Evaluate(r, caps)));
        });

        g.MapPost("", async (ReviewCreateDto dto, AppDbContext db, CurrentUser me,
            VerdictEngine engine, CancellationToken ct) =>
        {
            var entityId = dto.EntityId;
            if (entityId is null) // New reviews default to the first entity (CLAUDE.md §5).
                entityId = await db.Entities.OrderBy(e => e.SortOrder).Select(e => (Guid?)e.Id).FirstOrDefaultAsync(ct);

            string categoryName = "";
            if (dto.CategoryId is not null)
                categoryName = await db.Categories.Where(c => c.Id == dto.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(ct) ?? "";

            var count = await db.Reviews.CountAsync(ct);
            var r = new Review
            {
                VendorName = dto.VendorName,
                ProductName = dto.ProductName,
                CategoryId = dto.CategoryId,
                CategoryName = categoryName,
                EntityId = entityId,
                OwnerName = string.IsNullOrWhiteSpace(dto.OwnerName) ? me.DisplayName : dto.OwnerName!,
                OwnerObjectId = me.ObjectId,
                OwnerEmail = me.Email,
                ReviewRef = $"TR-{DateTime.UtcNow:yyyy}-{100 + count:000}",
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = ReviewStatus.Draft,
            };
            db.Reviews.Add(r);
            await db.SaveChangesAsync(ct);
            var full = await LoadFull(db, r.Id, ct);
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            return Results.Created($"/reviews/{r.Id}", full!.ToDetail(engine.Evaluate(full!, caps)));
        });

        g.MapPut("/{id:guid}", async (Guid id, ReviewUpdateDto dto, AppDbContext db,
            CurrentUser me, VerdictEngine engine, CancellationToken ct) =>
        {
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            if (!me.IsLeadership && r.OwnerObjectId != me.ObjectId && r.OwnerName != me.DisplayName)
                return Results.Forbid();

            if (dto.VendorName is not null) r.VendorName = dto.VendorName;
            if (dto.ProductName is not null) r.ProductName = dto.ProductName;
            if (dto.OwnerName is not null) r.OwnerName = dto.OwnerName;
            if (dto.OwnerEmail is not null) r.OwnerEmail = dto.OwnerEmail;
            if (dto.ReviewRef is not null) r.ReviewRef = dto.ReviewRef;
            if (dto.Date is not null) r.Date = dto.Date.Value;
            if (dto.RawPitch is not null) r.RawPitch = dto.RawPitch;
            if (dto.NdaContactName is not null) r.NdaContactName = dto.NdaContactName;
            if (dto.NdaContactEmail is not null) r.NdaContactEmail = dto.NdaContactEmail;
            if (dto.Nda is not null) r.Nda = Mapping.ParseEnum(dto.Nda, r.Nda);
            if (dto.Recommendation is not null) r.Recommendation = dto.Recommendation;
            if (dto.Status is not null) r.Status = Mapping.ParseEnum(dto.Status, r.Status);
            if (dto.EntityId is not null) r.EntityId = dto.EntityId;
            if (dto.CategoryId is not null)
            {
                r.CategoryId = dto.CategoryId;
                r.CategoryName = await db.Categories.Where(c => c.Id == dto.CategoryId)
                    .Select(c => c.Name).FirstOrDefaultAsync(ct) ?? r.CategoryName;
            }

            if (dto.OpenQuestions is not null)
            {
                db.OpenQuestions.RemoveRange(r.OpenQuestions);
                r.OpenQuestions = dto.OpenQuestions.Select((q, i) => new OpenQuestion
                { ReviewId = r.Id, Text = q.Text, Resolved = q.Resolved, SortOrder = i }).ToList();
            }

            if (dto.Sections is not null)
            {
                db.ReviewSectionScores.RemoveRange(r.Sections);
                r.Sections = dto.Sections.Select((s, si) => new ReviewSectionScore
                {
                    ReviewId = r.Id, SectionId = s.SectionId, SectionName = s.SectionName, SortOrder = si,
                    Items = s.Items.Select((it, ii) => new ReviewItemScore
                    {
                        SectionItemId = it.SectionItemId, Label = it.Label,
                        Weight = Mapping.ParseEnum(it.Weight, ItemWeight.Med),
                        Status = Mapping.ParseEnum(it.Status, ItemStatus.Unscored),
                        Note = it.Note, Mitigation = it.Mitigation, SortOrder = ii,
                    }).ToList()
                }).ToList();
            }

            r.UpdatedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            var full = await LoadFull(db, id, ct);
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            return Results.Ok(full!.ToDetail(engine.Evaluate(full!, caps)));
        });

        // Deterministic assisted intake.
        g.MapPost("/{id:guid}/scan", async (Guid id, AppDbContext db, IntakeScanner scanner,
            CurrentUser me, CancellationToken ct) =>
        {
            var r = await db.Reviews.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound();
            return Results.Ok(await scanner.Scan(r.RawPitch, ct));
        });

        // Sign-off: leadership sets the review to Approved/Rejected from the computed verdict.
        g.MapPost("/{id:guid}/signoff", async (Guid id, AppDbContext db, CurrentUser me,
            VerdictEngine engine, CancellationToken ct) =>
        {
            if (!me.IsLeadership) return Results.Forbid();
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var v = engine.Evaluate(r, caps);
            r.Status = v.Verdict == "DoNotProceed" ? ReviewStatus.Rejected : ReviewStatus.Approved;
            r.UpdatedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(r.ToDetail(v));
        });

        // Finish & archive: capture an immutable memo snapshot.
        g.MapPost("/{id:guid}/finish", async (Guid id, AppDbContext db, CurrentUser me,
            VerdictEngine engine, MemoBuilder memo, CancellationToken ct) =>
        {
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            if (!me.IsLeadership && r.OwnerObjectId != me.ObjectId && r.OwnerName != me.DisplayName)
                return Results.Forbid();
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var v = engine.Evaluate(r, caps);
            var version = await db.ArchivedReviews.Where(a => a.VendorName == r.VendorName).CountAsync(ct) + 1;
            db.ArchivedReviews.Add(new ArchivedReview
            {
                ReviewId = r.Id, VendorName = r.VendorName, CategoryName = r.CategoryName,
                OwnerName = r.OwnerName, EntityId = r.EntityId, EntityName = r.Entity?.Name,
                Verdict = Mapping.ParseEnum(v.Verdict, Verdict.InProgress), Version = version,
                FinishedOn = DateOnly.FromDateTime(DateTime.UtcNow),
                MemoMarkdown = memo.Build(r, v, r.Entity?.Name),
            });
            r.Status = ReviewStatus.Finished;
            r.UpdatedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(r.ToDetail(v));
        });

        // Export the rendered memo markdown.
        g.MapGet("/{id:guid}/memo", async (Guid id, AppDbContext db, VerdictEngine engine,
            MemoBuilder memo, CancellationToken ct) =>
        {
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var md = memo.Build(r, engine.Evaluate(r, caps), r.Entity?.Name);
            return Results.Text(md, "text/markdown");
        });

        // Send an approval reminder now (owner + approvers), via MS Graph.
        g.MapPost("/{id:guid}/remind", async (Guid id, AppDbContext db, CurrentUser me,
            VerdictEngine engine, IGraphService graph, CancellationToken ct) =>
        {
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var v = engine.Evaluate(r, caps);

            var approverEmails = await db.AppUsers
                .Where(u => u.Enabled && (u.Role == AppRole.Cfo || u.Role == AppRole.CioCto) && u.Email != null)
                .Select(u => u.Email!).ToListAsync(ct);
            var to = new List<string>();
            if (!string.IsNullOrWhiteSpace(r.OwnerEmail)) to.Add(r.OwnerEmail!);
            to.AddRange(approverEmails);
            var cc = new List<string>();
            if (!string.IsNullOrWhiteSpace(me.Email)) cc.Add(me.Email!); // requester cc'd

            var sent = await graph.SendMailAsync(to, cc,
                $"Reminder: vendor review — {r.VendorName} ({v.VerdictLabel})",
                $"<p>Reminder to action <strong>{r.VendorName}</strong> — {v.VerdictLabel}. {v.VerdictReason}</p>", ct);
            r.LastReminderUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { sent, mock = !graph.IsConfigured, to, cc });
        });

        g.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            var r = await db.Reviews.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound();
            if (!me.IsLeadership && r.OwnerObjectId != me.ObjectId && r.OwnerName != me.DisplayName)
                return Results.Forbid();
            db.Reviews.Remove(r);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static Task<Review?> LoadFull(AppDbContext db, Guid id, CancellationToken ct) =>
        db.Reviews
            .Include(r => r.Entity)
            .Include(r => r.OpenQuestions)
            .Include(r => r.Sections).ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
}
