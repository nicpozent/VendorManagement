using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using VendorReview.Api.Auth;
using VendorReview.Api.Data;
using VendorReview.Api.Endpoints;
using VendorReview.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// ---- Persistence (PostgreSQL) ----
var conn = cfg.GetConnectionString("Default")
           ?? "Host=localhost;Port=5432;Database=vendorreview;Username=vendorreview;Password=devpassword";
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));

// ---- Auth: real Entra when configured, else a dev principal so the app runs locally ----
bool entraConfigured = !string.IsNullOrWhiteSpace(cfg["AzureAd:ClientId"])
                       && !string.IsNullOrWhiteSpace(cfg["AzureAd:TenantId"]);
if (entraConfigured)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(cfg.GetSection("AzureAd"));
}
else
{
    builder.Services.AddAuthentication(DevAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.SchemeName, _ => { });
}
builder.Services.AddAuthorization();

// ---- App services ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<VerdictEngine>();
builder.Services.AddScoped<IntakeScanner>();
builder.Services.AddSingleton<MemoBuilder>();
builder.Services.AddSingleton<IGraphService, GraphService>();

builder.Services.Configure<ReminderOptions>(cfg.GetSection("Reminders"));
builder.Services.AddSingleton<ReminderService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ReminderService>());

// ---- CORS for the SPA ----
var corsOrigins = cfg.GetSection("Cors:Origins").Get<string[]>()
                  ?? new[] { "http://localhost:5173", "http://localhost:4173" };
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---- Migrate (or create) + seed ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasMigrations = (await db.Database.GetPendingMigrationsAsync()).Any()
                        || (await db.Database.GetAppliedMigrationsAsync()).Any();
    if (hasMigrations) await db.Database.MigrateAsync();
    else await db.Database.EnsureCreatedAsync();
    await SeedData.EnsureSeededAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", entra = entraConfigured }));
app.MapMe();
app.MapReviews();
app.MapVendors();
app.MapCatalog();
app.MapCompare();
app.MapArchive();
app.MapAdmin();

app.Run();
