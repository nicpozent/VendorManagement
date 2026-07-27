using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VendorReview.Api.Tests.Integration;

/// <summary>
/// Boots the real API against a Postgres database (Development environment, so the
/// dev-auth handler is registered and requests carry role via X-Debug-* headers).
/// The connection string comes from TEST_DB_CONNECTION / ConnectionStrings__Default,
/// defaulting to a local instance. Encryption and the reminder sweep are disabled so
/// tests are deterministic and never attempt outbound mail.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            var conn = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                ?? "Host=localhost;Port=5432;Database=vendorreview;Username=vendorreview;Password=devpassword";
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = conn,
                ["Encryption:Enabled"] = "false",
                ["Reminders:Enabled"] = "false",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning",
                // Empty Entra config → dev-auth fallback (permitted in Development).
                ["AzureAd:TenantId"] = "",
                ["AzureAd:ClientId"] = "",
            });
        });
    }

    /// <summary>An HTTP client that authenticates as the given role/identity via dev-auth headers.</summary>
    public HttpClient ClientAs(string role, string oid, string name)
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Add("X-Debug-Role", role);
        c.DefaultRequestHeaders.Add("X-Debug-Oid", oid);
        c.DefaultRequestHeaders.Add("X-Debug-User", name);
        c.DefaultRequestHeaders.Add("X-Debug-Email", $"{oid}@birgma.dev");
        return c;
    }
}
