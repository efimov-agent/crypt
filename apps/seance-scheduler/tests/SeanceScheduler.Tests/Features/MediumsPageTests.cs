using System.Net;
using System.Net.Http.Json;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Features.Mediums;
using Xunit;

namespace SeanceScheduler.Tests.Features;

[Collection("Integration tests")]
public class MediumsPageTests : IClassFixture<TestDatabaseFixture>
{
    private readonly HttpClient _client;

    public MediumsPageTests(TestDatabaseFixture fixture)
    {
        _client = PageForms.CreateNoRedirectClient(fixture);
    }

    private static string UniqueName(string prefix = "Medium") => $"{prefix} {Guid.NewGuid():N}";

    private Task<HttpResponseMessage> PostCreateAsync(string name, string specialty = "") =>
        PageForms.PostAsync(_client, "/mediums", "CreateMedium",
            [new("Form.Name", name), new("Form.Specialty", specialty)]);

    [Fact]
    public async Task Get_RendersListAndForm()
    {
        var name = UniqueName();
        Assert.Equal(HttpStatusCode.Found, (await PostCreateAsync(name)).StatusCode);

        var html = await _client.GetStringAsync("/mediums");

        Assert.Contains("<title>Mediums · Seance Scheduler</title>", html);
        Assert.Contains(name, html);
        Assert.Contains("id=\"medium-name\"", html);
    }

    [Fact]
    public async Task Create_Valid_RedirectsWithPrgAndShowsNotice()
    {
        var name = UniqueName();

        var response = await PostCreateAsync(name, "Tarot");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/mediums?created=1", response.Headers.Location?.PathAndQuery);
        var html = await _client.GetStringAsync("/mediums?created=1");
        Assert.Contains("The medium has been summoned.", html);
        Assert.Contains(name, html);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400WithAccessibleFieldError()
    {
        var response = await PostCreateAsync("   ", "Keep me");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var input = PageForms.ElementTag(html, "medium-name");
        Assert.NotNull(input);
        Assert.Contains("aria-invalid=\"true\"", input);
        Assert.Contains("aria-describedby=\"medium-name-error\"", input);
        Assert.NotNull(PageForms.ElementTag(html, "medium-name-error"));
        Assert.Contains("value=\"Keep me\"", PageForms.ElementTag(html, "medium-specialty"));
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409AndKeepsValue()
    {
        var name = UniqueName();
        Assert.Equal(HttpStatusCode.Found, (await PostCreateAsync(name)).StatusCode);

        var response = await PostCreateAsync(name.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.NotNull(PageForms.ElementTag(html, "medium-form-error"));
        Assert.Contains($"value=\"{name.ToUpperInvariant()}\"", PageForms.ElementTag(html, "medium-name"));
    }

    [Fact]
    public async Task Create_WithoutAntiforgeryToken_Returns400()
    {
        var response = await PageForms.PostAsync(_client, "/mediums", "CreateMedium",
            [new("Form.Name", UniqueName())], withToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_Existing_RedirectsAndMarksDeparted()
    {
        var name = UniqueName();
        Assert.Equal(HttpStatusCode.Found, (await PostCreateAsync(name)).StatusCode);
        var mediums = await _client.GetFromJsonAsync<List<MediumDto>>("/api/mediums");
        var id = mediums!.Single(m => m.Name == name).Id;

        var response = await PageForms.PostAsync(_client, "/mediums", "DeactivateMedium",
            [new("DeactivateId", id.ToString())]);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/mediums?deactivated=1", response.Headers.Location?.PathAndQuery);
        var medium = await _client.GetFromJsonAsync<MediumDto>($"/api/mediums/{id}");
        Assert.False(medium!.IsActive);
        var html = await _client.GetStringAsync("/mediums?deactivated=1");
        Assert.DoesNotContain($"aria-label=\"Deactivate {name}\"", html);
    }

    [Fact]
    public async Task Deactivate_Unknown_Returns400WithMessage()
    {
        Assert.Equal(HttpStatusCode.Found, (await PostCreateAsync(UniqueName())).StatusCode);

        var response = await PageForms.PostAsync(_client, "/mediums", "DeactivateMedium",
            [new("DeactivateId", "999999")]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("role=\"alert\"", html);
        Assert.Contains("999999", html);
    }
}
