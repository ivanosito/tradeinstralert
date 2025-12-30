using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using tradeinstralert.Services;
using tradeinstralert.Utils;

var builder = Host.CreateApplicationBuilder(args);

// Logging + AppInsights
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

// Storage (use the same connection string Azure Functions uses for its host storage)
var storageConn = Env.Require("AzureWebJobsStorage");
builder.Services.AddSingleton(new BlobServiceClient(storageConn));

// HTTP clients
builder.Services.AddHttpClient<TwelveDataClient>(client =>
{
    client.BaseAddress = new Uri("https://api.twelvedata.com/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddHttpClient<VoiceTradingSmsSender>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

// Our services
builder.Services.AddSingleton<BlobConfigStore>();

builder.Services.AddFunctionsWorkerDefaults();

builder.Build().Run();
