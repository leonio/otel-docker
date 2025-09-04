using System.Diagnostics.Metrics;

namespace OtelConsoleApp;

public class OrderMetrics
{
    public Meter Meter { get; }
    public Histogram<double> OrderValueHistogram { get; }
    public Counter<int> ProductCounter { get; }

    public OrderMetrics(IMeterFactory meterFactory)
    {
        Meter = meterFactory.Create(OtelConsoleAppConfig.ServiceName);
        OrderValueHistogram = Meter.CreateHistogram<double>("order.value.total");
        ProductCounter = Meter.CreateCounter<int>("products.processed");
    }
}
