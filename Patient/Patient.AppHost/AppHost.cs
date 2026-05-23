using Amazon;
using Aspire.Hosting.LocalStack.Container;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var gatewayPort = builder.Configuration.GetValue<int>("GatewayPort");
var gateway = builder
    .AddProject<Projects.PatientApp_Gateway>("gateway")
    .WithExternalHttpEndpoints();

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localStack = builder
.AddLocalStack("landplot-localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = 4566;
    container.AdditionalEnvironmentVariables
    .Add("DEBUG", "1");
    container.AdditionalEnvironmentVariables
    .Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
});

var eventSink = builder.AddProject<Projects.EventSink2>("landplot-sink")
    .WithHttpEndpoint(port: 5261, name: "sns")
    .WithReference(localStack)
    .WaitFor(localStack);

for (var i = 1; i <= 3; ++i)
{
    var currGenerator = builder.AddProject<Projects.PatientApp_Generator>
        ($"generator-{i}")
        .WithEndpoint("http", endpoint => endpoint.Port = gatewayPort + i)
        .WithReference(redis)
        .WaitFor(redis)
        .WithReference(localStack)
        .WaitFor(localStack);

    gateway
        .WithReference(currGenerator)
        .WaitFor(currGenerator);
}

gateway
    .WithReference(eventSink)
    .WaitFor(eventSink);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.UseLocalStack(localStack);

builder.Build().Run();
