using Amazon.SimpleNotificationService;
using Amazon.SQS;
using MassTransit;
using LocalStack.Client.Extensions;
using PatientApp.Generator.Services;
using PatientApp.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis");

builder.Services.AddMassTransit(x =>
{
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
                    ServiceURL =
                        "http://localhost:4566",

                    AuthenticationRegion =
                        "us-east-1",

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
    });
});

builder.Services.AddSingleton<PatientGenerator>();

builder.Services.AddScoped<PatientService>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins!)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddLocalStack(builder.Configuration);

var app = builder.Build();

app.UseCors();

app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();

app.MapGet("/patient", async (
    int id,
    PatientService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation(
        "Received request for patient with ID: {id}",
        id);

    if (id <= 0)
    {
        logger.LogWarning(
            "Received invalid ID: {id}",
            id);

        return Results.BadRequest(
            new
            {
                error =
                    "ID must be a positive number"
            });
    }

    try
    {
        var patient =
            await service.GetByIdAsync(
                id,
                cancellationToken);

        return Results.Ok(patient);
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Error while getting patient {id}",
            id);

        return Results.Problem(
        detail: ex.ToString(),
        title: ex.Message);
    }
})
.WithName("GetPatient");

app.Run();