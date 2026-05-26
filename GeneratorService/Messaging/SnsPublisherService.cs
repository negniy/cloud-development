using Amazon.SimpleNotificationService;
using PatientApp.Generator.Models;
using System.Text.Json;

namespace PatientApp.Generator.Messaging;

public class SnsPublisherService(IAmazonSimpleNotificationService snsClient, IConfiguration config, ILogger<SnsPublisherService> logger)
{
    private readonly string _topicArn = config["AWS:Resources:SNSTopicArn"]
        ?? "arn:aws:sns:us-east-1:000000000000:patient-topic";

    public async Task PublishPatientAsync(Patient patient)
    {
        var message = JsonSerializer.Serialize(patient);

        await snsClient.PublishAsync(new Amazon.SimpleNotificationService.Model.PublishRequest
        {
            TopicArn = _topicArn,
            Message = message,
            MessageAttributes = new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>
            {
                ["EventType"] = new() { DataType = "String", StringValue = "PatientGenerated" }
            }
        });

        logger.LogInformation("Пациент {id} отправлен в SNS", patient.Id);
    }
}