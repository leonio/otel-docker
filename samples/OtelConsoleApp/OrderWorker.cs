using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bogus;

namespace OtelConsoleApp;

public class OrderWorker : BackgroundService
{
    private readonly ILogger<OrderWorker> _logger;
    private readonly ActivitySource _activitySource = new ActivitySource("ConsoleAppActivitySource");
    private readonly Faker _faker = new Faker();
    private readonly Faker<Product> _productFaker;
    private readonly Faker<Customer> _customerFaker;
    private readonly Counter<decimal> _orderValueCounter;
    private readonly Counter<int> _productCounter;

    public OrderWorker(ILogger<OrderWorker> logger, OrderMetrics metrics)
    {
        _logger = logger;
        _productFaker = new Faker<Product>()
            .RuleFor(p => p.Id, f => f.Random.Int(1, 100))
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Price, f => f.Random.Decimal(10, 1000))
            .RuleFor(p => p.Category, f => f.Commerce.Department());

        _customerFaker = new Faker<Customer>()
            .RuleFor(c => c.Id, f => f.Random.Guid())
            .RuleFor(c => c.Name, f => f.Name.FullName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Country, f => f.Address.Country());

        _orderValueCounter = metrics.OrderValueCounter;
        _productCounter = metrics.ProductCounter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var orderActivity = _activitySource.StartActivity("ProcessOrder"))
            {
                var customer = _customerFaker.Generate();
                var products = _productFaker.Generate(_faker.Random.Int(1, 5));
                decimal orderTotal = products.Sum(p => p.Price);

                orderActivity?.SetTag("order.customer.id", customer.Id);
                orderActivity?.SetTag("order.customer.country", customer.Country);
                orderActivity?.SetTag("order.total", orderTotal);
                orderActivity?.SetTag("order.items.count", products.Count);

                using (var scope = _logger.BeginScope("Order ID: {OrderId}", Guid.NewGuid()))
                {
                    _logger.LogInformation(
                        "Processing order for {CustomerName} from {Country}. Order total: ${OrderTotal:F2}",
                        customer.Name,
                        customer.Country,
                        orderTotal);

                    await Task.Delay(_faker.Random.Int(100, 1000), stoppingToken);

                    foreach (var product in products)
                    {
                        using var productActivity = _activitySource.StartActivity("ProcessProduct");
                        productActivity?.SetTag("product.id", product.Id);
                        productActivity?.SetTag("product.name", product.Name);
                        productActivity?.SetTag("product.category", product.Category);
                        productActivity?.SetTag("product.price", product.Price);

                        _logger.LogInformation(
                            "Processing product: {ProductName} (${Price:F2})",
                            product.Name,
                            product.Price);

                        await Task.Delay(_faker.Random.Int(50, 200), stoppingToken);

                        _productCounter.Add(1, new KeyValuePair<string, object?>("category", product.Category));
                    }

                    _orderValueCounter.Add(orderTotal);

                    if (_faker.Random.Int(1, 100) <= 5)
                    {
                        var error = _faker.Random.Bool()
                            ? "Payment Declined"
                            : "Insufficient Inventory";

                        _logger.LogError("Order processing failed: {Error}", error);
                        orderActivity?.SetTag("error", true);
                        orderActivity?.SetTag("error.type", error);
                    }
                    else
                    {
                        _logger.LogInformation("Order processed successfully!");
                    }
                }
            }

            await Task.Delay(2000, stoppingToken);
        }
    }
}

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public required string Category { get; set; }
}

public class Customer
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Country { get; set; }
}
