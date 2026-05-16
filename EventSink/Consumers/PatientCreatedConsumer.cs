using System.Text.Json;
using MassTransit;
using PatientApp.Generator.Models;
using PatientApp.EventSink.Storage;

namespace PatientApp.EventSink.Consumers;

public class PatientCreatedConsumer(
    IStorageService storageService,
    ILogger<PatientCreatedConsumer> logger)
    : IConsumer<Patient>
{
    public async Task Consume(
        ConsumeContext<Patient> context)
    {
        var patient = context.Message;

        logger.LogInformation(
            "Received patient {Id}",
            patient.Id);

        var json =
            JsonSerializer.Serialize(patient);

        await storageService.SaveAsync(
            $"{patient.Id}.json",
            json);

        logger.LogInformation(
            "Patient {Id} saved to S3",
            patient.Id);
    }
}