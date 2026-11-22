using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Features.Mediums;
using Xunit;

namespace SeanceScheduler.Tests.Features;

[Collection("Integration tests")]
public class MediumsEndpointTests : IClassFixture<TestDatabaseFixture>
{
    private readonly HttpClient _client;

    public MediumsEndpointTests(TestDatabaseFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    private static string UniqueName(string prefix = "Medium") => $"{prefix} {Guid.NewGuid():N}";

    private async Task<MediumDto> CreateAsync(string name, string? specialty = null)
    {
        var response = await _client.PostAsJsonAsync("/api/mediums", new { name, specialty });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MediumDto>())!;
    }

    [Fact]
    public async Task GetMediums_ReturnsCreatedMediumsOrderedByName()
    {
        var b = await CreateAsync(UniqueName("ZZ"));
        var a = await CreateAsync(UniqueName("AA"));

        var mediums = await _client.GetFromJsonAsync<List<MediumDto>>("/api/mediums");

        Assert.NotNull(mediums);
        var names = mediums.Select(m => m.Name).ToList();
        Assert.True(names.IndexOf(a.Name) < names.IndexOf(b.Name));
        Assert.Equal(names.Order(StringComparer.OrdinalIgnoreCase), names);
    }

    [Fact]
    public async Task GetMedium_Existing_ReturnsOk()
    {
        var created = await CreateAsync(UniqueName(), "Ouija");

        var fetched = await _client.GetFromJsonAsync<MediumDto>($"/api/mediums/{created.Id}");

        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task GetMedium_Unknown_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/mediums/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem?.Status);
    }

    [Fact]
    public async Task CreateMedium_Valid_Returns201WithLocation_TrimmedAndActive()
    {
        var name = UniqueName();

        var response = await _client.PostAsJsonAsync("/api/mediums", new { name = $"  {name}  ", specialty = "  Tarot " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MediumDto>();
        Assert.NotNull(created);
        Assert.Equal(name, created.Name);
        Assert.Equal("Tarot", created.Specialty);
        Assert.True(created.IsActive);
        Assert.Equal($"/api/mediums/{created.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateMedium_DuplicateNameDifferentCase_Returns409ProblemDetails()
    {
        var name = UniqueName();
        await CreateAsync(name);

        var response = await _client.PostAsJsonAsync("/api/mediums", new { name = name.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem?.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateMedium_EmptyName_Returns400WithNameError(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/mediums", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task CreateMedium_NameTooLong_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/mediums", new { name = new string('x', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Deactivate_Existing_Returns200AndPersists()
    {
        var created = await CreateAsync(UniqueName());

        var response = await _client.PostAsync($"/api/mediums/{created.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<MediumDto>())!.IsActive);
        Assert.False((await _client.GetFromJsonAsync<MediumDto>($"/api/mediums/{created.Id}"))!.IsActive);
    }

    [Fact]
    public async Task Deactivate_Unknown_Returns404()
    {
        var response = await _client.PostAsync("/api/mediums/999999/deactivate", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
