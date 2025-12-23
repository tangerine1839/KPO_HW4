namespace OrderService.Background;

using System.Text;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;


public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public OutboxPublisher(IServiceProvider sp, IConfiguration config)
    {
        _sp = sp;
        var factory = new ConnectionFactory { HostName = config["RabbitMq:HostName"] };
        try 
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare("orders_queue", durable: true, exclusive: false, autoDelete: false);
        } catch {  }
    }

    protected override async Task ExecuteAsync(CancellationToken st)
    {
        while (!st.IsCancellationRequested)
        {
            if (_channel == null || _channel.IsClosed) { await Task.Delay(5000, st); continue; }

            try 
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                
                var msgs = await db.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .Take(10)
                    .ToListAsync(st);

                foreach (var msg in msgs)
                {
                    var body = Encoding.UTF8.GetBytes(msg.Data);
                    _channel.BasicPublish(exchange: "", routingKey: "orders_queue", basicProperties: null, body: body);
                    msg.ProcessedAt = DateTime.UtcNow;
                }

                if (msgs.Any()) await db.SaveChangesAsync(st);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing: {ex.Message}");
            }
            
            await Task.Delay(1000, st);
        }
    }
}
