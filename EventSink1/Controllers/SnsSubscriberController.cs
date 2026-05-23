using Amazon.SimpleNotificationService.Util;
using EventSink1.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace EventSink1.Controllers;

[ApiController]
[Route("api/sns")]
public class SnsSubscriberController(IS3Service s3Service, ILogger<SnsSubscriberController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ReceiveMessage()
    {
        logger.LogInformation("SNS webhook вызван");

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();

            var snsMessage = Message.ParseMessage(jsonContent);

            if (snsMessage.Type == "SubscriptionConfirmation")
            {
                logger.LogInformation("Получено подтверждение подписки");
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(snsMessage.SubscribeURL);
                logger.LogInformation("Подписка подтверждена: {status}", response.StatusCode);
                return Ok();
            }

            if (snsMessage.Type == "Notification")
            {
                await s3Service.UploadFile(snsMessage.MessageText);
                logger.LogInformation("Сообщение успешно сохранено в S3");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке SNS сообщения");
        }

        return Ok();
    }
}