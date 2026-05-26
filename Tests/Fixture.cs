using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Tests;

public class Fixture : IAsyncLifetime
{
    private const string BucketName = "patient-bucket";

    public DistributedApplication App { get; private set; } = null!;

    public IDistributedApplicationTestingBuilder Builder { get; private set; } = null!;

    public AmazonS3Client StorageClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.PatientApp_AppHost>();

        Builder.Configuration["DcpPublisher:RandomizePorts"] = "false";

        App = await Builder.BuildAsync();

        await App.StartAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        await Task.WhenAll(
            App.ResourceNotifications.WaitForResourceHealthyAsync("patient-localstack", cts.Token),
            App.ResourceNotifications.WaitForResourceHealthyAsync("gateway", cts.Token),
            App.ResourceNotifications.WaitForResourceHealthyAsync("patient-sink", cts.Token)
        );

        var localStackUrl = App
            .GetEndpoint("patient-localstack", "http")
            .ToString()
            .TrimEnd('/');

        StorageClient = new AmazonS3Client(
            "test",
            "test",
            new AmazonS3Config
            {
                ServiceURL = localStackUrl,
                ForcePathStyle = true
            });
    }

    public async Task<List<S3Object>> WaitForObjectAsync(
        string prefix,
        int maxAttempts = 10)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(1000);

            var response =
                await StorageClient.ListObjectsV2Async(
                    new ListObjectsV2Request
                    {
                        BucketName = BucketName,
                        Prefix = prefix
                    });

            if (response.S3Objects.Count > 0)
            {
                return response.S3Objects;
            }
        }

        return [];
    }

    public async Task DisposeAsync()
    {
        StorageClient.Dispose();

        await App.StopAsync();

        await App.DisposeAsync();

        await Builder.DisposeAsync();
    }
}