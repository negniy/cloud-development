using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using MassTransit;
using PatientApp.EventSink.Consumers;
using PatientApp.EventSink.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var config = new AmazonS3Config
    {
        ServiceURL = "http://localhost:4566",
        ForcePathStyle = true,
        UseHttp = true,
        AuthenticationRegion = "us-east-1"
    };

    return new AmazonS3Client(
        new BasicAWSCredentials("test", "test"),
        config);
});

builder.Services.AddScoped<IStorageService, StorageService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PatientCreatedConsumer>();

    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(
            "us-east-1",
            h =>
            {
                h.AccessKey("test");

                h.SecretKey("test");

                h.Config(new AmazonSQSConfig
                {
                    ServiceURL = "http://localhost:4566",
                    AuthenticationRegion = "us-east-1",
                    UseHttp = true
                });

                h.Config(
                    new AmazonSimpleNotificationServiceConfig
                    {
                        ServiceURL =
                            "http://localhost:4566",

                        AuthenticationRegion =
                            "us-east-1",

                        UseHttp = true
                    });
            });

        cfg.ReceiveEndpoint(
            "patients-queue",
            e =>
            {
                e.ConfigureConsumeTopology = true;

                e.ConfigureConsumer<
                    PatientCreatedConsumer>(
                    context);
            });
    });
});

var app = builder.Build();

app.MapControllers();

app.MapGet("/test-s3", async (
    IAmazonS3 s3) =>
{
    var buckets =
        await s3.ListBucketsAsync();

    return Results.Ok(
        buckets.Buckets
            .Select(x => x.BucketName)
            .ToList());
});

app.MapGet("/test-files", async (
    IAmazonS3 s3) =>
{
    try
    {
        var response =
            await s3.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = "patients"
                });

        return Results.Ok(
            response.S3Objects
                .Select(x => x.Key)
                .ToList());
    }
    catch
    {
        return Results.Ok(
            new List<string>());
    }
});

app.Run();