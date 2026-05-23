using EventSink1;
using LocalStack.Client.Extensions;
using PatientApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddLocalStack(builder.Configuration);

builder.AddConsumer();
builder.AddS3();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.UseConsumer();
await app.UseS3();

app.MapControllers();
app.Run();