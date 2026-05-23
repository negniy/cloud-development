using Aspire.Hosting.Testing;
using PatientApp.Generator.Models;
using System.Net;
using System.Text.Json;

namespace Tests;

public class IntegrationTests(Fixture fixture)
    : IClassFixture<Fixture>
{
    private static readonly Random _random = new();

    [Fact]
    public async Task Gateway_Returns_200()
    {
        var id = _random.Next(1, 100);

        using var client =
            fixture.App.CreateHttpClient("gateway", "http");

        var response =
            await client.GetAsync($"/patient?id={id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Patient_Is_Saved_To_S3()
    {
        var id = _random.Next(100, 200);

        using var client =
            fixture.App.CreateHttpClient("gateway", "http");

        await client.GetAsync($"/patient?id={id}");

        var objects =
            await fixture.WaitForObjectAsync("landplot_");

        Assert.NotEmpty(objects);
    }

    [Fact]
    public async Task Cache_Returns_Same_Response()
    {
        var id = _random.Next(200, 300);

        using var client =
            fixture.App.CreateHttpClient("gateway", "http");

        var firstResponse =
            await client.GetAsync($"/patient?id={id}");

        var firstJson =
            await firstResponse.Content.ReadAsStringAsync();

        var secondResponse =
            await client.GetAsync($"/patient?id={id}");

        var secondJson =
            await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(firstJson, secondJson);
    }
}