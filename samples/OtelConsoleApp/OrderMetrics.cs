using System.Diagnostics.Metrics;

namespace OtelConsoleApp;

public class OrderMetrics
{
    public Meter Meter { get; }
    public Counter<decimal> OrderValueCounter { get; }
    public Counter<int> ProductCounter { get; }

    public OrderMetrics(IMeterFactory meterFactory)
    {
        Meter = meterFactory.Create("MyBasicConsoleApp");
        OrderValueCounter = Meter.CreateCounter<decimal>("order.value.total");
        ProductCounter = Meter.CreateCounter<int>("products.processed");
    }
}
