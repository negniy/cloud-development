using PatientApp.Generator.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting.Testing;

namespace IntegrationTests;

public class PatientIntegrationTests : IClassFixture<AppHostFixture>
{
    private readonly AppHostFixture _fixture;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PatientIntegrationTests(AppHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Gateway_ReturnsPatient()
    {
        using var client = _fixture.App.CreateHttpClient("gateway");

        var response = await client.GetAsync("/patient?id=1");

        var content = await response.Content.ReadAsStringAsync();
        
        Assert.True(response.IsSuccessStatusCode, content);

        var patient = await response.Content.ReadFromJsonAsync<Patient>(_jsonOptions);

        Assert.NotNull(patient);

        Assert.Equal(1, patient.Id);
    }

    [Fact]
    public async Task Gateway_RepeatedRequest_ReturnsCachedPatient()
    {
        var id = Random.Shared.Next(10000, 20000);

        using var client = _fixture.App.CreateHttpClient("gateway");

        var response1 = await client.GetStringAsync($"/patient?id={id}");

        var response2 = await client.GetStringAsync($"/patient?id={id}");

        Assert.Equal(response1, response2);
    }

    [Fact]
    public async Task EventSink_SavesPatientToS3()
    {
        var id = Random.Shared.Next(20000, 30000);

        using var client = _fixture.App.CreateHttpClient("gateway");

        var response = await client.GetAsync($"/patient?id={id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var objects = await _fixture.WaitForS3ObjectAsync($"{id}.json");

        Assert.NotEmpty(objects);

        var s3Object = objects.First();

        var file = await _fixture.S3Client.GetObjectAsync("patients", s3Object.Key);

        using var reader = new StreamReader(file.ResponseStream);

        var json = await reader.ReadToEndAsync();

        var savedPatient = JsonSerializer.Deserialize<Patient>(json, _jsonOptions);

        Assert.NotNull(savedPatient);

        Assert.Equal(id, savedPatient.Id);
    }

    [Fact]
    public async Task DifferentIds_ReturnDifferentPatients()
    {
        using var client = _fixture.App.CreateHttpClient("gateway");

        var patient1 = await client.GetFromJsonAsync<Patient>("/patient?id=501");

        var patient2 = await client.GetFromJsonAsync<Patient>("/patient?id=502");

        Assert.NotNull(patient1);
        Assert.NotNull(patient2);

        Assert.NotEqual(patient1.Id, patient2.Id);
    }

    [Fact]
    public async Task CacheHit_DoesNotDuplicateS3File()
    {
        var id = Random.Shared.Next(50000, 60000);

        using var client = _fixture.App.CreateHttpClient("gateway");

        await client.GetAsync($"/patient?id={id}");

        var first = await _fixture.WaitForS3ObjectAsync($"{id}.json");

        Assert.NotEmpty(first);

        await client.GetAsync($"/patient?id={id}");

        await Task.Delay(TimeSpan.FromSeconds(5));

        var list = await _fixture.S3Client.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
        {
            BucketName = "patients"
        });

        var matches = list.S3Objects.Where(x => x.Key.Contains($"{id}.json")).ToList();

        Assert.Single(matches);
    }
}