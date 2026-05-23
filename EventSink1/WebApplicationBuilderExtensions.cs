using Amazon.S3;
using Amazon.SimpleNotificationService;
using EventSink1.Messaging;
using EventSink1.Storage;
using LocalStack.Client.Enums;
using LocalStack.Client.Extensions;

namespace EventSink1;

internal static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddConsumer(this WebApplicationBuilder builder)
    {
        builder.Services.AddLocalStack(builder.Configuration);
        return builder.AddSnsSubscriber();
    }

    private static WebApplicationBuilder AddSnsSubscriber(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<SnsSubscriptionService>();
        builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
        return builder;
    }

    public static WebApplicationBuilder AddS3(this WebApplicationBuilder builder)
    {
        builder.Services.AddAwsService<IAmazonS3>();
        builder.Services.AddScoped<IS3Service, S3AwsService>();
        return builder;
    }
}