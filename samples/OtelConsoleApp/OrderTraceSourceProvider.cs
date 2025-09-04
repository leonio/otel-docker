using System.Diagnostics;

namespace OtelConsoleApp;

public sealed class OrderTraceSourceProvider
{
    public static ActivitySource OrderActivities { get; } = new ActivitySource(OtelConsoleAppConfig.ActivitySourceName);
}
