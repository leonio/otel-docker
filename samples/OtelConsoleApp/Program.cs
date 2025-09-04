using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using OtelConsoleApp;

var builder = Host.CreateApplicationBuilder(args);
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(OtelConsoleAppConfig.ServiceName, OtelConsoleAppConfig.ServiceVersion);

using var listener = new MyEventListener();

builder.Logging
    .AddConsole()
    .SetMinimumLevel(LogLevel.Debug)
    .AddOpenTelemetry(logging =>
    {
        logging.SetResourceBuilder(resourceBuilder);
        //logging
        //    //.AddConsoleExporter()
        //    .AddOtlpExporter();
    });

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder);
        metrics.AddMeter(OtelConsoleAppConfig.ServiceName);
        metrics.AddRuntimeInstrumentation();
        //metrics
        //    //.AddConsoleExporter()
        //    .AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder);
        tracing.AddSource(OtelConsoleAppConfig.ActivitySourceName);
        tracing
            //.AddConsoleExporter()
            .AddOtlpExporter();
    });

builder.Services.AddHostedService<OrderWorker>();

builder.Services.AddSingleton<OrderMetrics>(sp => new OrderMetrics(sp.GetRequiredService<IMeterFactory>()));

var host = builder.Build();
await host.RunAsync();

internal class MyEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // This is a list of known OpenTelemetry EventSource names
        if (eventSource.Name.StartsWith("OpenTelemetry-") || eventSource.Name.StartsWith("OpenTelemetry.Exporter-"))
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.Message is not null)
        {
            var message = eventData.Payload is not null && eventData.Payload.Count > 0
                ? string.Format(eventData.Message, eventData.Payload.ToArray())
                : eventData.Message;
            Console.WriteLine($"[{eventData.EventSource.Name}] {message}");
        }
    }
}
