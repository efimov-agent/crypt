using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SeanceScheduler.Tests.Fixtures;

/// <summary>
/// Helpers for driving Blazor static-SSR forms over plain HTTP: fetch the page for the antiforgery
/// cookie and token, then post the named form the way a browser would.
/// </summary>
public static partial class PageForms
{
    public static HttpClient CreateNoRedirectClient(TestDatabaseFixture fixture) =>
        fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static async Task<HttpResponseMessage> PostAsync(
        HttpClient client, string url, string formName, IEnumerable<KeyValuePair<string, string>> fields, bool withToken = true)
    {
        var page = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();

        var form = new List<KeyValuePair<string, string>> { new("_handler", formName) };
        if (withToken)
        {
            var match = TokenRegex().Match(html);
            Assert.True(match.Success, "Antiforgery token not found in the rendered page.");
            form.Add(new("__RequestVerificationToken", WebUtility.HtmlDecode(match.Groups[1].Value)));
        }

        form.AddRange(fields);
        return await client.PostAsync(url, new FormUrlEncodedContent(form));
    }

    /// <summary>Attributes of the element with the given id, or null when it is not rendered.</summary>
    public static string? ElementTag(string html, string id)
    {
        var match = Regex.Match(html, $"<[a-z]+[^>]*\\bid=\"{Regex.Escape(id)}\"[^>]*>");
        return match.Success ? match.Value : null;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();
}
