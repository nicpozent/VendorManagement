using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace VendorReview.Api.Tests.Integration;

/// <summary>
/// Regression tests for the platform's authorization model: managers reach only their
/// own reviews (and every sub-resource), leadership reaches the whole portfolio, and
/// admin surfaces are admin-only. These lock in the IDOR / scoping fixes.
/// </summary>
public class AuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    private readonly HttpClient _alice;
    private readonly HttpClient _bob;
    private readonly HttpClient _cfo;

    public AuthorizationTests(ApiFactory f)
    {
        _f = f;
        _alice = f.ClientAs("ITManager", "alice", "Alice Manager");
        _bob = f.ClientAs("ITManager", "bob", "Bob Manager");
        _cfo = f.ClientAs("Cfo", "cfo", "CFO User");
    }

    private static async Task<string> CreateReview(HttpClient c, string vendorName, string? categoryId = null)
    {
        var res = await c.PostAsJsonAsync("/reviews", new { vendorName, productName = "", categoryId, entityId = (string?)null });
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task<HashSet<string>> Ids(HttpClient c, string path, string prop = "id")
    {
        var res = await c.GetAsync(path);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray().Select(e => e.GetProperty(prop).GetString()!).ToHashSet();
    }

    private static async Task<int> Status(HttpClient c, HttpMethod m, string path)
    {
        var res = await c.SendAsync(new HttpRequestMessage(m, path));
        return (int)res.StatusCode;
    }

    [Fact]
    public async Task Reviews_list_is_scoped_to_owner_for_managers()
    {
        var id = await CreateReview(_alice, "Alice Reviews Scope Co");
        Assert.Contains(id, await Ids(_alice, "/reviews"));
        Assert.DoesNotContain(id, await Ids(_bob, "/reviews"));
        Assert.Contains(id, await Ids(_cfo, "/reviews")); // leadership sees all
    }

    [Fact]
    public async Task Review_read_and_subresources_blocked_for_non_owner()
    {
        var id = await CreateReview(_alice, "Alice IDOR Co");

        // Owner and leadership succeed; a different manager is forbidden on every surface.
        Assert.Equal(200, await Status(_alice, HttpMethod.Get, $"/reviews/{id}"));
        Assert.Equal(200, await Status(_cfo, HttpMethod.Get, $"/reviews/{id}"));
        Assert.Equal(403, await Status(_bob, HttpMethod.Get, $"/reviews/{id}"));

        Assert.Equal(200, await Status(_alice, HttpMethod.Get, $"/reviews/{id}/memo"));
        Assert.Equal(403, await Status(_bob, HttpMethod.Get, $"/reviews/{id}/memo"));

        Assert.Equal(200, await Status(_alice, HttpMethod.Post, $"/reviews/{id}/scan"));
        Assert.Equal(403, await Status(_bob, HttpMethod.Post, $"/reviews/{id}/scan"));

        Assert.Equal(403, await Status(_bob, HttpMethod.Post, $"/reviews/{id}/remind"));
    }

    [Fact]
    public async Task Attachments_are_owner_scoped_and_round_trip()
    {
        var id = await CreateReview(_alice, "Alice Evidence Co");
        var bytes = new byte[2048];
        new Random(7).NextBytes(bytes);

        // Bob cannot upload to Alice's review.
        var bobForm = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "x.bin" }, { new StringContent("Soc2"), "kind" } };
        Assert.Equal(HttpStatusCode.Forbidden, (await _bob.PostAsync($"/reviews/{id}/attachments", bobForm)).StatusCode);

        // Alice uploads, lists, and downloads byte-for-byte.
        var form = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "evidence.bin" }, { new StringContent("Soc2"), "kind" } };
        var up = await _alice.PostAsync($"/reviews/{id}/attachments", form);
        Assert.Equal(HttpStatusCode.Created, up.StatusCode);
        using var upDoc = JsonDocument.Parse(await up.Content.ReadAsStringAsync());
        var attId = upDoc.RootElement.GetProperty("id").GetString()!;

        Assert.Equal(403, await Status(_bob, HttpMethod.Get, $"/reviews/{id}/attachments"));

        var dl = await _alice.GetAsync($"/reviews/{id}/attachments/{attId}/download");
        Assert.Equal(HttpStatusCode.OK, dl.StatusCode);
        Assert.Equal(bytes, await dl.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Archive_is_scoped_and_detail_blocked_for_non_owner()
    {
        var id = await CreateReview(_alice, "Alice Archive Co");
        (await _alice.PostAsync($"/reviews/{id}/finish", null)).EnsureSuccessStatusCode();

        Assert.Contains(id, await Ids(_alice, "/archive", "reviewId"));
        Assert.DoesNotContain(id, await Ids(_bob, "/archive", "reviewId"));
        Assert.Contains(id, await Ids(_cfo, "/archive", "reviewId"));

        // Resolve the archive snapshot id from the owner's list, then check cross-user access.
        var res = await _alice.GetAsync("/archive");
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var archiveId = doc.RootElement.EnumerateArray()
            .First(e => e.GetProperty("reviewId").GetString() == id).GetProperty("id").GetString()!;

        Assert.Equal(403, await Status(_bob, HttpMethod.Get, $"/archive/{archiveId}"));
        Assert.Equal(200, await Status(_alice, HttpMethod.Get, $"/archive/{archiveId}"));
        Assert.Equal(200, await Status(_cfo, HttpMethod.Get, $"/archive/{archiveId}"));
    }

    [Fact]
    public async Task Compare_is_scoped_to_owner_for_managers()
    {
        // Pick a real category, create an Alice review in it.
        var cats = await _cfo.GetAsync("/catalog/categories");
        using var catDoc = JsonDocument.Parse(await cats.Content.ReadAsStringAsync());
        var categoryId = catDoc.RootElement[0].GetProperty("id").GetString()!;
        var vendorName = "Alice Compare Vendor";
        await CreateReview(_alice, vendorName, categoryId);

        static async Task<HashSet<string>> Vendors(HttpClient c, string categoryId)
        {
            var res = await c.GetAsync($"/compare?categoryId={categoryId}");
            res.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("columns").EnumerateArray()
                .Select(e => e.GetProperty("vendorName").GetString()!).ToHashSet();
        }

        Assert.Contains(vendorName, await Vendors(_alice, categoryId));
        Assert.DoesNotContain(vendorName, await Vendors(_bob, categoryId));
        Assert.Contains(vendorName, await Vendors(_cfo, categoryId));
    }

    [Fact]
    public async Task Admin_surfaces_require_admin()
    {
        Assert.Equal(403, await Status(_bob, HttpMethod.Get, "/admin/audit"));
        Assert.Equal(403, await Status(_bob, HttpMethod.Get, "/admin/users"));
        Assert.Equal(200, await Status(_cfo, HttpMethod.Get, "/admin/audit"));

        // Entity + settings mutations are admin-only.
        Assert.Equal(HttpStatusCode.Forbidden, (await _bob.PostAsJsonAsync("/entities", new { name = "Rogue Entity" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _bob.PutAsJsonAsync("/settings", new { blockerCapsVerdict = false, reviewIntervalMonths = 6 })).StatusCode);
    }

    [Fact]
    public async Task Security_headers_present()
    {
        var res = await _alice.GetAsync("/health");
        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors", res.Headers.GetValues("Content-Security-Policy").Single());
    }
}
