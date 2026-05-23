using EventSink2.Messaging;
using EventSink2.Storage;

namespace EventSink2;

internal static class WebApplicationExtensions
{
    public static async Task<WebApplication> UseConsumer(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
        await subscriptionService.SubscribeEndpoint();
        return app;
    }

    public static async Task<WebApplication> UseS3(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
        await s3Service.EnsureBucketExists();
        return app;
    }
}