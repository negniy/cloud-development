using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var localstack = builder.AddContainer(
        "localstack",
        "localstack/localstack:3.7")
    .WithEndpoint(
        port: 4566,
        targetPort: 4566,
        name: "localstack",
        scheme: "http")
    .WithEnvironment("SERVICES", "s3,sns,sqs")
    .WithEnvironment("DEFAULT_REGION", "us-east-1")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEnvironment("HOSTNAME_EXTERNAL", "host.docker.internal")
    .WithEnvironment(
        "SNS_ENDPOINT_STRATEGY",
        "off");

var eventSink = builder.AddProject<Projects.PatientApp_EventSink>("event-sink")
    .WaitFor(localstack);

var gatewayPort = builder.Configuration.GetValue<int>("GatewayPort");
var gateway = builder
    .AddProject<Projects.PatientApp_Gateway>("gateway")
    .WithExternalHttpEndpoints();

for (var i = 1; i <= 3; ++i)
{
    var currGenerator = builder.AddProject<Projects.PatientApp_Generator>
        ($"generator-{i}")
        .WithEndpoint("http", endpoint => endpoint.Port = gatewayPort + i)
        .WithReference(redis)
        .WaitFor(redis);

    gateway
        .WithReference(currGenerator)
        .WaitFor(currGenerator);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
