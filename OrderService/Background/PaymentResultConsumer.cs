namespace OrderService.Background;

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


public record PaymentResultDto(Guid OrderId, bool IsSuccess);

public class PaymentResultConsumer : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConnection _conn;
    private readonly IModel _channel;

    public PaymentResultConsumer(IServiceProvider sp, IConfiguration config)
    {
        _sp = sp;
        var factory = new ConnectionFactory { HostName = config["RabbitMq:HostName"] };
        try 
        {
            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();
            _channel.QueueDeclare("payment_results", durable: true, exclusive: false, autoDelete: false);
        } catch {}
    }

    protected override Task ExecuteAsync(CancellationToken st)
    {
        if (_channel == null) return Task.CompletedTask;

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var data = JsonSerializer.Deserialize<PaymentResultDto>(body);

            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = await db.Orders.FindAsync(data.OrderId);
            
            if (order != null && order.Status == "NEW")
            {
                order.Status = data.IsSuccess ? "FINISHED" : "CANCELLED";
                await db.SaveChangesAsync();
            }
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        _channel.BasicConsume("payment_results", false, consumer);
        return Task.CompletedTask;
    }
}
