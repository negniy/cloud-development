using EventSink2;
using LocalStack.Client.Extensions;
using PatientApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);

builder.AddConsumer();
builder.AddS3();

var app = builder.Build();

await app.UseConsumer();
await app.UseS3();

app.MapControllers();
app.Run();