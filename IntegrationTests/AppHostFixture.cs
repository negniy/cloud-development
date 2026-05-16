using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace IntegrationTests;

public class AppHostFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;

    public AmazonS3Client S3Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PatientApp_AppHost>();

        App = await appHost.BuildAsync();

        await App.StartAsync();

        await App.ResourceNotifications.WaitForResourceHealthyAsync("gateway").WaitAsync(TimeSpan.FromMinutes(3));
        await App.ResourceNotifications.WaitForResourceHealthyAsync("generator-1").WaitAsync(TimeSpan.FromMinutes(3));
        await App.ResourceNotifications.WaitForResourceHealthyAsync("generator-2").WaitAsync(TimeSpan.FromMinutes(3));
        await App.ResourceNotifications.WaitForResourceHealthyAsync("generator-3").WaitAsync(TimeSpan.FromMinutes(3));
        await App.ResourceNotifications.WaitForResourceHealthyAsync("event-sink").WaitAsync(TimeSpan.FromMinutes(3));
        await App.ResourceNotifications.WaitForResourceHealthyAsync("localstack").WaitAsync(TimeSpan.FromMinutes(3));
        await App.ResourceNotifications.WaitForResourceHealthyAsync("redis").WaitAsync(TimeSpan.FromMinutes(3));

        var localstackUrl = App.GetEndpoint("localstack", "localstack").ToString().TrimEnd('/');

        S3Client = new AmazonS3Client("test", "test", new AmazonS3Config
        {
            ServiceURL = localstackUrl,
            ForcePathStyle = true
        });
    }

    public async Task<List<S3Object>> WaitForS3ObjectAsync(string fileName, int maxAttempts = 20)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            var response = await S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = "patients"
            });

            var match = response.S3Objects.Where(x => x.Key.Contains(fileName)).ToList();

            if (match.Count > 0)
                return match;
        }

        return new List<S3Object>();
    }

    public async Task DisposeAsync()
    {
        S3Client.Dispose();

        try
        {
            await App.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch
        {
            // игнорируем ошибки при остановке тестового хоста
        }
    }
}