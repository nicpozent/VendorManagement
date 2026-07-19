using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Auth;
using VendorReview.Api.Data;
using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;
using VendorReview.Api.Services;

namespace VendorReview.Api.Endpoints;

/// <summary>
/// Administrator-only surface: provisioning users from Entra (groups / enterprise
/// apps / app registrations) via MS Graph, and driving reminder sweeps.
/// </summary>
public static class AdminEndpoints
{
    public static void MapAdmin(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/admin").RequireAuthorization();

        g.MapGet("/graph-status", (IGraphService graph) =>
            Results.Ok(new { configured = graph.IsConfigured }));

        // Append-only audit trail of sensitive actions (admins only).
        g.MapGet("/audit", async (int? take, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var n = Math.Clamp(take ?? 100, 1, 500);
            var events = await db.AuditEvents.OrderByDescending(a => a.Id).Take(n).ToListAsync(ct);
            return Results.Ok(events.Select(a => a.ToDto()).ToList());
        });

        // ---- Imported users ----
        g.MapGet("/users", async (AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            return Results.Ok((await db.AppUsers.OrderBy(u => u.DisplayName).ToListAsync(ct))
                .Select(u => u.ToDto()).ToList());
        });

        g.MapPut("/users/{id:guid}", async (Guid id, UserRoleUpdateDto dto, AppDbContext db,
            CurrentUser me, AuditLog audit, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var u = await db.AppUsers.FindAsync(new object?[] { id }, ct);
            if (u is null) return Results.NotFound();
            u.Role = Mapping.ParseEnum(dto.Role, u.Role);
            u.Enabled = dto.Enabled;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("admin.user.update", "user", u.Id.ToString(), u.DisplayName,
                $"Role {u.Role}, {(u.Enabled ? "enabled" : "disabled")}", ct);
            return Results.Ok(u.ToDto());
        });

        g.MapDelete("/users/{id:guid}", async (Guid id, AppDbContext db, CurrentUser me,
            AuditLog audit, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var u = await db.AppUsers.FindAsync(new object?[] { id }, ct);
            if (u is null) return Results.NotFound();
            db.AppUsers.Remove(u);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("admin.user.delete", "user", u.Id.ToString(), u.DisplayName,
                "Removed imported user", ct);
            return Results.NoContent();
        });

        // ---- Entra import ----
        g.MapGet("/import/sources", async (string kind, IGraphService graph, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            try { return Results.Ok(await graph.ListSourcesAsync(kind, ct)); }
            catch (Exception ex) { return Results.Problem($"Graph error: {ex.Message}"); }
        });

        g.MapGet("/import/preview", async (string kind, string sourceId, IGraphService graph,
            CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            try { return Results.Ok(await graph.GetMembersAsync(kind, sourceId, ct)); }
            catch (Exception ex) { return Results.Problem($"Graph error: {ex.Message}"); }
        });

        g.MapPost("/import", async (ImportRequestDto req, AppDbContext db, IGraphService graph,
            CurrentUser me, AuditLog audit, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            List<GraphMember> members;
            try { members = await graph.GetMembersAsync(req.SourceKind, req.SourceId, ct); }
            catch (Exception ex) { return Results.Problem($"Graph error: {ex.Message}"); }

            var role = Mapping.ParseEnum(req.DefaultRole, AppRole.ITManager);
            int imported = 0, updated = 0, skipped = 0;
            var touched = new List<AppUser>();

            foreach (var m in members)
            {
                if (string.IsNullOrWhiteSpace(m.ObjectId)) { skipped++; continue; }
                var existing = await db.AppUsers.FirstOrDefaultAsync(u => u.EntraObjectId == m.ObjectId, ct);
                if (existing is null)
                {
                    var u = new AppUser
                    {
                        EntraObjectId = m.ObjectId, DisplayName = m.DisplayName, Email = m.Email,
                        JobTitle = m.JobTitle, Role = role, SourceName = req.SourceName, SourceId = req.SourceId,
                    };
                    db.AppUsers.Add(u);
                    touched.Add(u);
                    imported++;
                }
                else
                {
                    existing.DisplayName = m.DisplayName;
                    existing.Email = m.Email ?? existing.Email;
                    existing.JobTitle = m.JobTitle ?? existing.JobTitle;
                    existing.SourceName = req.SourceName;
                    existing.SourceId = req.SourceId;
                    touched.Add(existing);
                    updated++;
                }
            }
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("admin.users.import", "directory", req.SourceId, req.SourceName,
                $"Imported {imported}, updated {updated}, skipped {skipped} from {req.SourceKind} as {req.DefaultRole}", ct);
            return Results.Ok(new ImportResultDto(imported, updated, skipped,
                touched.Select(u => u.ToDto()).ToList(),
                graph.IsConfigured ? null : "Graph not configured — imported from mock directory data."));
        });

        // ---- Reminders ----
        g.MapPost("/reminders/run", async (ReminderService reminders, CurrentUser me,
            AuditLog audit, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var count = await reminders.RunOnceAsync(force: true, ct);
            await audit.WriteAsync("admin.reminders.run", "system", null, null,
                $"Manual reminder sweep — {count} attempted", ct);
            return Results.Ok(new { attempted = count });
        });
    }
}
