using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using PatientApp.Generator.Messaging;
using PatientApp.Generator.Services;
using PatientApp.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddAwsService<IAmazonSimpleNotificationService>(); //AddAwsService
builder.Services.AddScoped<SnsPublisherService>();

builder.Services.AddDefaultAWSOptions(new AWSOptions
{
    Credentials = new BasicAWSCredentials("test", "test"),
    Region = RegionEndpoint.EUCentral1,
    DefaultClientConfig =
    {
        ServiceURL = "http://localhost:4566"
    }
});
builder.Services.AddLocalStack(builder.Configuration);

builder.AddRedisDistributedCache("redis");

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
    logger.LogInformation("Received request for patient with ID: {id}", id);

    if (id <= 0)
    {
        logger.LogWarning("Received invalid ID: {id}", id);
        return Results.BadRequest(new { error = "ID must be a positive number"});
    }

    try
    {
        var application = await service.GetByIdAsync(id, cancellationToken);
        return Results.Ok(application);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while getting patient {id}", id);
        return Results.Problem("An error occurred while processing the request");
    }
})
.WithName("GetPatient");

app.Run();
