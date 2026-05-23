using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace EventSink1.Messaging;

public class SnsSubscriptionService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SnsSubscriptionService> logger)
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNSTopicArn not found in configuration");

    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Отправка запроса на подписку к {topic}", _topicArn);

        var endpoint = configuration["AWS:Resources:SNSUrl"] ?? "http://localhost:4566";

        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = $"{endpoint}/api/sns",   // вебхук
            ReturnSubscriptionArn = true
        };

        var response = await snsClient.SubscribeAsync(request);

        if (response.HttpStatusCode == HttpStatusCode.OK)
            logger.LogInformation("Запрос на подписку отправлен успешно. Ожидаем подтверждения.");
        else
            logger.LogError("Ошибка подписки на SNS");
    }
}