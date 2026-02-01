using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;

public class Worker : IHostedService, IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private ILogger _logger;
    private AppDbContext context;

    public Worker()
    {
        // проверка каталога logs и создание файла с текущей датой
        var logsDir = "logs";
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);
        var logFilePath = Path.Combine("logs", $"consumer-{DateTime.Today:yyyy-MM-dd}.log");

        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(logFilePath)
            .CreateLogger();
        context = new AppDbContext();
        // Принудительно создаём БД и таблицы, если их нет
        context.Database.EnsureCreated();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(options: null, cancellationToken: cancellationToken);

            await _channel.QueueDeclareAsync("order_queue", false, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.Information($"Получен заказ: {json}");
                    try
                    {
                        var message = JsonConvert.DeserializeObject<OrderMessage>(json);
                        if (message != null)
                            await SaveOrderAsync(message, ea.CancellationToken);
                        else
                            _logger.Warning("Получено сообщение, но не удалось десериализовать");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Ошибка при обработке сообщения");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    _logger.Error($"Ошибка: {ex.Message}");
                }
                // return true; // ✅ Теперь компилятор понимает, что это Task<bool>
            };

            await _channel.BasicConsumeAsync("order_queue", true, consumer);
            _logger.Information("Consumer слушает очередь order_queue...");
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Критическая ошибка при запуске Consumer");
            throw;
        }
    }
    private async Task SaveOrderAsync(OrderMessage message, CancellationToken ct)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Name == message.CustomerName, ct);

        if (customer == null)
        {
            customer = new Customer
            {
                Name = message.CustomerName,
                Email = $"{message.CustomerName.Replace(" ", "").ToLower()}@yandex.ru"
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync(ct);
            _logger.Information("Новый клиент сохранён: {Name}", customer.Name);
        }

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Name == message.ProductName, ct);

        if (product == null)
        {
            product = new Product
            {
                Name = message.ProductName,
                Price = new Random().Next(100, 1000)
            };
            context.Products.Add(product);
            await context.SaveChangesAsync(ct);
            _logger.Information("Новый товар сохранён: {Name}", product.Name);
        }

        var order = new Order
        {
            CustomerId = customer.Id,
            ProductId = product.Id,
            Quantity = message.Quantity
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync(ct);
        _logger.Information("Заказ сохранён: ID={OrderId}, Клиент={Customer}, Товар={Product}",
                order.Id, customer.Name, product.Name);
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Остановка Consumer...");
        return Task.CompletedTask;
    }
    public void Dispose()
    {
        _logger.Information("Остановка канала и соединения...");
        _channel?.DisposeAsync().GetAwaiter().GetResult();
        _connection?.DisposeAsync().GetAwaiter().GetResult();
        context.Dispose();
    }
}