using PaymentService.Domain;

namespace PaymentService.Background;

using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


public record OrderEventDto(Guid OrderId, Guid UserId, decimal Amount);

public class OrderConsumer : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IModel _channel;
    private readonly IConnection _conn;

    public OrderConsumer(IServiceProvider sp, IConfiguration config)
    {
        _sp = sp;
        var factory = new ConnectionFactory { HostName = config["RabbitMq:HostName"] };
        try {
            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();
            _channel.QueueDeclare("orders_queue", durable: true, exclusive: false, autoDelete: false);
        } catch {}
    }

    protected override Task ExecuteAsync(CancellationToken st)
    {
        if (_channel == null) return Task.CompletedTask;

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var data = JsonSerializer.Deserialize<OrderEventDto>(json);

            if (data != null)
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                
                if (!await db.InboxMessages.AnyAsync(m => m.Id == data.OrderId))
                {
                    db.InboxMessages.Add(new InboxMessage 
                    { 
                        Id = data.OrderId, 
                        Data = json, 
                        Processed = false 
                    });
                    await db.SaveChangesAsync();
                }
            }
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        _channel.BasicConsume("orders_queue", false, consumer);
        return Task.CompletedTask;
    }
}
