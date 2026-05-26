using Amazon.S3;
using Amazon.SimpleNotificationService;
using EventSink;
using EventSink.Messaging;
using EventSink.Storage;
using LocalStack.Client.Extensions;
using PatientApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);

builder.Services.AddScoped<SnsSubscriptionService>();

builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

builder.Services.AddAwsService<IAmazonS3>();

builder.Services.AddScoped<IS3Service, S3AwsService>();

var app = builder.Build();

await app.UseConsumer();
await app.UseS3();

app.MapControllers();
app.Run();