using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace EventSink.Messaging;

public class SnsSubscriptionService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SnsSubscriptionService> logger)
{
    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Создание темы SNS");
        var createResponse = await snsClient.CreateTopicAsync("patient-topic");

        logger.LogInformation("Отправка запроса на подписку к {topic}", createResponse.TopicArn);

        var endpoint = configuration["AWS:Resources:SNSUrl"] ?? "http://localhost:4566";

        var request = new SubscribeRequest
        {
            TopicArn = createResponse.TopicArn,
            Protocol = "http",
            Endpoint = $"{endpoint}/api/sns",
            ReturnSubscriptionArn = true
        };

        var response = await snsClient.SubscribeAsync(request);

        if (response.HttpStatusCode == HttpStatusCode.OK)
            logger.LogInformation("Запрос на подписку отправлен успешно. Ожидаем подтверждения.");
        else
            logger.LogError("Ошибка подписки на SNS");
    }
}