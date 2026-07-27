using System.Text.Encodings.Web;
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
            if (!CanAccess(me, r)) return Results.Forbid();
            return Results.Ok(await scanner.Scan(r.RawPitch, ct));
        });

        // Sign-off: leadership sets the review to Approved/Rejected from the computed verdict.
        g.MapPost("/{id:guid}/signoff", async (Guid id, AppDbContext db, CurrentUser me,
            VerdictEngine engine, AuditLog audit, CancellationToken ct) =>
        {
            if (!me.IsLeadership) return Results.Forbid();
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var v = engine.Evaluate(r, caps);
            r.Status = v.Verdict == "DoNotProceed" ? ReviewStatus.Rejected : ReviewStatus.Approved;
            r.UpdatedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("review.signoff", "review", r.Id.ToString(), r.VendorName,
                $"Signed off as {r.Status} (verdict {v.VerdictLabel})", ct);
            return Results.Ok(r.ToDetail(v));
        });

        // Finish & archive: capture an immutable memo snapshot.
        g.MapPost("/{id:guid}/finish", async (Guid id, AppDbContext db, CurrentUser me,
            VerdictEngine engine, MemoBuilder memo, AuditLog audit, CancellationToken ct) =>
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
            r.FinishedUtc = DateTime.UtcNow;
            r.UpdatedUtc = DateTime.UtcNow;

            // Anchor the renewal clock: stamp the matching vendor's last-review date.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var vendor = await db.Vendors.FirstOrDefaultAsync(x => x.Name == r.VendorName, ct);
            if (vendor is not null) vendor.LastReview = today;

            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("review.finish", "review", r.Id.ToString(), r.VendorName,
                $"Finished & archived v{version} (verdict {v.VerdictLabel})", ct);
            return Results.Ok(r.ToDetail(v));
        });

        // Export the rendered memo markdown.
        g.MapGet("/{id:guid}/memo", async (Guid id, AppDbContext db, CurrentUser me,
            VerdictEngine engine, MemoBuilder memo, CancellationToken ct) =>
        {
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            if (!CanAccess(me, r)) return Results.Forbid();
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var md = memo.Build(r, engine.Evaluate(r, caps), r.Entity?.Name);
            return Results.Text(md, "text/markdown");
        });

        // Send an approval reminder now (owner + approvers), via MS Graph.
        // Rate-limited per user; recipients filtered through the mail allowlist.
        g.MapPost("/{id:guid}/remind", async (Guid id, AppDbContext db, CurrentUser me,
            VerdictEngine engine, IGraphService graph, MailGuard mail, AuditLog audit, CancellationToken ct) =>
        {
            var r = await LoadFull(db, id, ct);
            if (r is null) return Results.NotFound();
            if (!CanAccess(me, r)) return Results.Forbid();
            bool caps = await SettingsRepo.BlockerCapsVerdict(db, ct);
            var v = engine.Evaluate(r, caps);

            var approverEmails = await db.AppUsers
                .Where(u => u.Enabled && (u.Role == AppRole.Cfo || u.Role == AppRole.CioCto) && u.Email != null)
                .Select(u => u.Email!).ToListAsync(ct);
            var recipients = new List<string>();
            if (!string.IsNullOrWhiteSpace(r.OwnerEmail)) recipients.Add(r.OwnerEmail!);
            recipients.AddRange(approverEmails);

            var (to, blocked) = mail.Partition(recipients);
            if (to.Count == 0)
                return Results.BadRequest(new { message = "No reminder recipients pass the mail allowlist.", blocked });

            var cc = new List<string>();
            if (!string.IsNullOrWhiteSpace(me.Email) && mail.IsAllowed(me.Email)) cc.Add(me.Email!); // requester cc'd

            // HTML-encode vendor-controlled values interpolated into the mail body.
            var vendorHtml = HtmlEncoder.Default.Encode(r.VendorName);
            var reasonHtml = HtmlEncoder.Default.Encode(v.VerdictReason);
            var sent = await graph.SendMailAsync(to, cc,
                $"Reminder: vendor review — {r.VendorName} ({v.VerdictLabel})",
                $"<p>Reminder to action <strong>{vendorHtml}</strong> — {v.VerdictLabel}. {reasonHtml}</p>", ct);
            r.LastReminderUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("review.remind", "review", r.Id.ToString(), r.VendorName,
                $"Reminder to {to.Count} recipient(s){(graph.IsConfigured ? "" : " (mock)")}", ct);
            return Results.Ok(new { sent, mock = !graph.IsConfigured, to, cc, blocked });
        }).RequireRateLimiting("mail");

        // ---- Evidence attachments (SOC 2 / ISO / DPA / pen-test / signed NDA) ----
        const long MaxUploadBytes = 15 * 1024 * 1024;

        g.MapGet("/{id:guid}/attachments", async (Guid id, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            var r = await db.Reviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound();
            if (!CanAccess(me, r)) return Results.Forbid();
            var items = await db.Attachments.AsNoTracking().Where(a => a.ReviewId == id)
                .OrderByDescending(a => a.UploadedUtc).ToListAsync(ct);
            return Results.Ok(items.Select(a => a.ToDto()).ToList());
        });

        g.MapPost("/{id:guid}/attachments", async (Guid id, HttpRequest req, AppDbContext db,
            CurrentUser me, AuditLog audit, CancellationToken ct) =>
        {
            var r = await db.Reviews.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound();
            if (!CanAccess(me, r)) return Results.Forbid();
            if (!req.HasFormContentType) return Results.BadRequest(new { message = "Expected multipart/form-data." });
            // Reject oversize before buffering the body (allow small multipart overhead).
            if (req.ContentLength is > MaxUploadBytes + 1024 * 1024)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

            var form = await req.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0) return Results.BadRequest(new { message = "No file uploaded." });
            if (file.Length > MaxUploadBytes)
                return Results.BadRequest(new { message = $"File exceeds the {MaxUploadBytes / (1024 * 1024)} MB limit." });

            var kind = Mapping.ParseEnum(form["kind"].ToString(), AttachmentKind.Other);
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var att = new Attachment
            {
                ReviewId = id,
                FileName = Path.GetFileName(file.FileName),
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                Kind = kind,
                UploadedByObjectId = me.ObjectId,
                UploadedByName = me.DisplayName,
                Data = ms.ToArray(),
            };
            db.Attachments.Add(att);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("attachment.add", "attachment", id.ToString(), att.FileName,
                $"Uploaded {att.Kind} evidence ({att.SizeBytes / 1024} KB) to {r.VendorName}", ct);
            return Results.Created($"/reviews/{id}/attachments/{att.Id}", att.ToDto());
        }).DisableAntiforgery();

        g.MapGet("/{id:guid}/attachments/{attId:guid}/download", async (Guid id, Guid attId,
            AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            var r = await db.Reviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound();
            if (!CanAccess(me, r)) return Results.Forbid();
            var att = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attId && a.ReviewId == id, ct);
            if (att is null) return Results.NotFound();
            return Results.File(att.Data, att.ContentType, att.FileName);
        });

        g.MapDelete("/{id:guid}/attachments/{attId:guid}", async (Guid id, Guid attId,
            AppDbContext db, CurrentUser me, AuditLog audit, CancellationToken ct) =>
        {
            var r = await db.Reviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound();
            if (!CanAccess(me, r)) return Results.Forbid();
            var att = await db.Attachments.FirstOrDefaultAsync(a => a.Id == attId && a.ReviewId == id, ct);
            if (att is null) return Results.NotFound();
            db.Attachments.Remove(att);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("attachment.delete", "attachment", id.ToString(), att.FileName,
                $"Removed evidence from {r.VendorName}", ct);
            return Results.NoContent();
        });

        // Per-review history: the audit events that target this review (sign-off, finish,
        // reminders, attachment add/remove). Visible to the owner and leadership.
        g.MapGet("/{id:guid}/audit", async (Guid id, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            var r = await db.Reviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (r is null) return Results.NotFound();
            if (!CanAccess(me, r)) return Results.Forbid();
            var key = id.ToString();
            var events = await db.AuditEvents.AsNoTracking()
                .Where(a => a.TargetId == key)
                .OrderByDescending(a => a.Utc).Take(200).ToListAsync(ct);
            return Results.Ok(events.Select(e => e.ToDto()).ToList());
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

    /// <summary>Owner-or-leadership access check, matching the review read/write rule.</summary>
    private static bool CanAccess(CurrentUser me, Review r) =>
        me.IsLeadership || r.OwnerObjectId == me.ObjectId || r.OwnerName == me.DisplayName;

    private static Task<Review?> LoadFull(AppDbContext db, Guid id, CancellationToken ct) =>
        db.Reviews
            .Include(r => r.Entity)
            .Include(r => r.OpenQuestions)
            .Include(r => r.Sections).ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
}
