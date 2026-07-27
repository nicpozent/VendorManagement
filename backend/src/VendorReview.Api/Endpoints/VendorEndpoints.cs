using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Auth;
using VendorReview.Api.Data;
using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;
using VendorReview.Api.Services;

namespace VendorReview.Api.Endpoints;

public static class VendorEndpoints
{
    public static void MapVendors(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/vendors").RequireAuthorization();

        g.MapGet("", async (AppDbContext db, CancellationToken ct) =>
        {
            var interval = await SettingsRepo.ReviewIntervalMonths(db, ct);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return Results.Ok((await db.Vendors.OrderBy(v => v.Name).ToListAsync(ct))
                .Select(v => v.ToDto(interval, today)).ToList());
        });

        g.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var v = await db.Vendors.FindAsync(new object?[] { id }, ct);
            if (v is null) return Results.NotFound();
            var interval = await SettingsRepo.ReviewIntervalMonths(db, ct);
            return Results.Ok(v.ToDto(interval, DateOnly.FromDateTime(DateTime.UtcNow)));
        });

        g.MapPut("/{id:guid}/contact", async (Guid id, ContactUpdateDto dto, AppDbContext db, CancellationToken ct) =>
        {
            var v = await db.Vendors.FindAsync(new object?[] { id }, ct);
            if (v is null) return Results.NotFound();
            v.ContactName = dto.ContactName;
            v.ContactEmail = dto.ContactEmail;
            await db.SaveChangesAsync(ct);
            var interval = await SettingsRepo.ReviewIntervalMonths(db, ct);
            return Results.Ok(v.ToDto(interval, DateOnly.FromDateTime(DateTime.UtcNow)));
        });

        g.MapPut("/{id:guid}/nda", async (Guid id, VendorDto dto, AppDbContext db, CancellationToken ct) =>
        {
            var v = await db.Vendors.FindAsync(new object?[] { id }, ct);
            if (v is null) return Results.NotFound();
            v.Nda = Mapping.ParseEnum(dto.Nda, v.Nda);
            await db.SaveChangesAsync(ct);
            var interval = await SettingsRepo.ReviewIntervalMonths(db, ct);
            return Results.Ok(v.ToDto(interval, DateOnly.FromDateTime(DateTime.UtcNow)));
        });

        // Send NDA via MS Graph; the requesting user is cc'd. Marks the vendor "Requested".
        // Rate-limited per user; recipient domain checked against the mail allowlist.
        g.MapPost("/{id:guid}/send-nda", async (Guid id, AppDbContext db, CurrentUser me,
            IGraphService graph, MailGuard mail, AuditLog audit, CancellationToken ct) =>
        {
            var v = await db.Vendors.FindAsync(new object?[] { id }, ct);
            if (v is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(v.ContactEmail))
                return Results.BadRequest(new { message = "Vendor has no contact email." });
            if (!mail.IsAllowed(v.ContactEmail))
                return Results.BadRequest(new { message = $"Recipient domain is not on the mail allowlist: {v.ContactEmail}" });

            var cc = string.IsNullOrWhiteSpace(me.Email) ? Array.Empty<string>() : new[] { me.Email! };
            await graph.SendMailAsync(
                new[] { v.ContactEmail }, cc,
                $"NDA for review — {v.Name}",
                $"<p>Hi {v.ContactName},</p><p>Please find attached the mutual NDA required before our technical review of {v.Name}. Kindly sign and return.</p><p>Birgma International — Global IT Infrastructure</p>",
                ct);

            if (v.Nda == NdaStatus.None) v.Nda = NdaStatus.Requested;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("vendor.nda.send", "vendor", v.Id.ToString(), v.Name,
                $"NDA sent to {v.ContactEmail}{(graph.IsConfigured ? "" : " (mock)")}", ct);
            return Results.Ok(new SendNdaResultDto(
                $"NDA sent to {v.ContactEmail}" + (graph.IsConfigured ? "" : " (mock — Graph not configured)"),
                me.Email ?? "you"));
        }).RequireRateLimiting("mail");
    }
}
