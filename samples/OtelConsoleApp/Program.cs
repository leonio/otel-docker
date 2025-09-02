using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bogus;
using OtelConsoleApp;

const string ServiceName = "MyBasicConsoleApp";
const string ServiceVersion = "1.0.0";
const string ActivitySourceName = "ConsoleAppActivitySource";

var faker = new Faker();
var productFaker = new Faker<Product>()
    .RuleFor(p => p.Id, f => f.Random.Int(1, 100))
    .RuleFor(p => p.Name, f => f.Commerce.ProductName())
    .RuleFor(p => p.Price, f => f.Random.Decimal(10, 1000))
    .RuleFor(p => p.Category, f => f.Commerce.Department());

var customerFaker = new Faker<Customer>()
    .RuleFor(c => c.Id, f => f.Random.Guid())
    .RuleFor(c => c.Name, f => f.Name.FullName())
    .RuleFor(c => c.Email, f => f.Internet.Email())
    .RuleFor(c => c.Country, f => f.Address.Country());

var builder = Host.CreateApplicationBuilder(args);
        
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(
        ResourceBuilder
            .CreateDefault()
            .AddService(ServiceName, ServiceVersion));
    logging.AddConsoleExporter();
    logging.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(
            ResourceBuilder
                .CreateDefault()
                .AddService(ServiceName, ServiceVersion));
        metrics.AddRuntimeInstrumentation();
        metrics.AddConsoleExporter();
        metrics.AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(
            ResourceBuilder
                .CreateDefault()
                .AddService(ServiceName, ServiceVersion));
        tracing.AddSource(ActivitySourceName);
        tracing.AddConsoleExporter();
        tracing.AddOtlpExporter();
    });

builder.Services.AddHostedService<OrderWorker>();

builder.Services.AddSingleton<OrderMetrics>(sp => new OrderMetrics(sp.GetRequiredService<IMeterFactory>()));

var host = builder.Build();
await host.RunAsync();
