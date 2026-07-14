using Microsoft.EntityFrameworkCore;
using VendorReview.Api.Auth;
using VendorReview.Api.Data;
using VendorReview.Api.Domain;
using VendorReview.Api.Dtos;
using VendorReview.Api.Services;

namespace VendorReview.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalog(this IEndpointRouteBuilder app)
    {
        MapCategories(app);
        MapSections(app);
        MapPolicies(app);
        MapEntities(app);
        MapSettings(app);
    }

    private static void MapCategories(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/catalog/categories").RequireAuthorization();

        g.MapGet("", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.Categories.OrderBy(c => c.SortOrder).ToListAsync(ct))
                .Select(c => c.ToDto()).ToList()));

        g.MapPost("", async (CategoryDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var max = await db.Categories.MaxAsync(c => (int?)c.SortOrder, ct) ?? -1;
            var c = new Category { Name = dto.Name, IncludedSectionIds = dto.IncludedSectionIds ?? new(), SortOrder = max + 1 };
            db.Categories.Add(c);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/catalog/categories/{c.Id}", c.ToDto());
        });

        g.MapPut("/{id:guid}", async (Guid id, CategoryDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var c = await db.Categories.FindAsync(new object?[] { id }, ct);
            if (c is null) return Results.NotFound();
            c.Name = dto.Name;
            c.IncludedSectionIds = dto.IncludedSectionIds ?? new();
            await db.SaveChangesAsync(ct);
            return Results.Ok(c.ToDto());
        });

        g.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var c = await db.Categories.FindAsync(new object?[] { id }, ct);
            if (c is null) return Results.NotFound();
            db.Categories.Remove(c);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static void MapSections(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/catalog/sections").RequireAuthorization();

        g.MapGet("", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.Sections.Include(s => s.Items).OrderBy(s => s.SortOrder).ToListAsync(ct))
                .Select(s => s.ToDto()).ToList()));

        g.MapPost("", async (SectionDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var max = await db.Sections.MaxAsync(s => (int?)s.SortOrder, ct) ?? -1;
            var s = new Section
            {
                Name = dto.Name, Kind = Mapping.ParseEnum(dto.Kind, SectionKind.Fixed), SortOrder = max + 1,
                Items = dto.Items.Select((i, ix) => new SectionItem
                { Label = i.Label, Weight = Mapping.ParseEnum(i.Weight, ItemWeight.Med), Selectable = i.Selectable, SortOrder = ix }).ToList()
            };
            db.Sections.Add(s);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/catalog/sections/{s.Id}", s.ToDto());
        });

        g.MapPut("/{id:guid}", async (Guid id, SectionDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var s = await db.Sections.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null) return Results.NotFound();
            s.Name = dto.Name;
            s.Kind = Mapping.ParseEnum(dto.Kind, s.Kind);
            db.SectionItems.RemoveRange(s.Items);
            s.Items = dto.Items.Select((i, ix) => new SectionItem
            { SectionId = s.Id, Label = i.Label, Weight = Mapping.ParseEnum(i.Weight, ItemWeight.Med), Selectable = i.Selectable, SortOrder = ix }).ToList();
            await db.SaveChangesAsync(ct);
            return Results.Ok(s.ToDto());
        });

        g.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var s = await db.Sections.FindAsync(new object?[] { id }, ct);
            if (s is null) return Results.NotFound();
            db.Sections.Remove(s);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static void MapPolicies(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/catalog/policies").RequireAuthorization();

        g.MapGet("", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.Policies.Include(p => p.Section).OrderBy(p => p.SortOrder).ToListAsync(ct))
                .Select(p => p.ToDto()).ToList()));

        g.MapPost("", async (PolicyDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var max = await db.Policies.MaxAsync(p => (int?)p.SortOrder, ct) ?? -1;
            var p = new Policy
            {
                Rule = dto.Rule, SectionId = dto.SectionId, Severity = Mapping.ParseEnum(dto.Severity, PolicySeverity.Concern),
                Weight = Mapping.ParseEnum(dto.Weight, ItemWeight.High), Active = dto.Active, SortOrder = max + 1
            };
            db.Policies.Add(p);
            await db.SaveChangesAsync(ct);
            p = await db.Policies.Include(x => x.Section).FirstAsync(x => x.Id == p.Id, ct);
            return Results.Created($"/catalog/policies/{p.Id}", p.ToDto());
        });

        g.MapPut("/{id:guid}", async (Guid id, PolicyDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var p = await db.Policies.FindAsync(new object?[] { id }, ct);
            if (p is null) return Results.NotFound();
            p.Rule = dto.Rule; p.SectionId = dto.SectionId;
            p.Severity = Mapping.ParseEnum(dto.Severity, p.Severity);
            p.Weight = Mapping.ParseEnum(dto.Weight, p.Weight);
            p.Active = dto.Active;
            await db.SaveChangesAsync(ct);
            p = await db.Policies.Include(x => x.Section).FirstAsync(x => x.Id == p.Id, ct);
            return Results.Ok(p.ToDto());
        });

        g.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var p = await db.Policies.FindAsync(new object?[] { id }, ct);
            if (p is null) return Results.NotFound();
            db.Policies.Remove(p);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static void MapEntities(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/entities").RequireAuthorization();

        // All roles can read entities (IT Manager sees them read-only).
        g.MapGet("", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.Entities.OrderBy(e => e.SortOrder).ToListAsync(ct))
                .Select(e => e.ToDto()).ToList()));

        // Create/update/delete: admins only (CIO/CTO & CFO).
        g.MapPost("", async (EntityUpsertDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var max = await db.Entities.MaxAsync(e => (int?)e.SortOrder, ct) ?? -1;
            var e = new Entity { Name = dto.Name, SortOrder = max + 1 };
            db.Entities.Add(e);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/entities/{e.Id}", e.ToDto());
        });

        g.MapPut("/{id:guid}", async (Guid id, EntityUpsertDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var e = await db.Entities.FindAsync(new object?[] { id }, ct);
            if (e is null) return Results.NotFound();
            e.Name = dto.Name;
            await db.SaveChangesAsync(ct);
            return Results.Ok(e.ToDto());
        });

        g.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            var e = await db.Entities.FindAsync(new object?[] { id }, ct);
            if (e is null) return Results.NotFound();
            db.Entities.Remove(e);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static void MapSettings(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/settings").RequireAuthorization();

        g.MapGet("", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(new SettingsDto(await SettingsRepo.BlockerCapsVerdict(db, ct))));

        g.MapPut("", async (SettingsDto dto, AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            await SettingsRepo.SetBlockerCapsVerdict(db, dto.BlockerCapsVerdict, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new SettingsDto(dto.BlockerCapsVerdict));
        });

        g.MapPost("/reset", async (AppDbContext db, CurrentUser me, CancellationToken ct) =>
        {
            if (!me.IsAdmin) return Results.Forbid();
            await SeedData.ResetAsync(db, ct);
            return Results.Ok(new { message = "Sample data restored." });
        });
    }
}
