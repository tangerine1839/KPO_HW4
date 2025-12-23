namespace PaymentService.Background;


using System.Text;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;


public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConnection _conn;
    private readonly IModel _channel;

    public OutboxPublisher(IServiceProvider sp, IConfiguration config)
    {
        _sp = sp;
        var factory = new ConnectionFactory { HostName = config["RabbitMq:HostName"] };
        try {
            _conn = factory.CreateConnection();
            _channel = _conn.CreateModel();
            _channel.QueueDeclare("payment_results", durable: true, exclusive: false, autoDelete: false);
        } catch {}
    }

    protected override async Task ExecuteAsync(CancellationToken st)
    {
        while (!st.IsCancellationRequested)
        {
            if (_channel == null) { await Task.Delay(5000, st); continue; }

            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            
            var msgs = await db.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .Take(20)
                .ToListAsync(st);

            foreach (var msg in msgs)
            {
                var body = Encoding.UTF8.GetBytes(msg.Data);
                _channel.BasicPublish("", "payment_results", null, body);
                msg.ProcessedAt = DateTime.UtcNow;
            }

            if (msgs.Any()) await db.SaveChangesAsync(st);
            await Task.Delay(1000, st);
        }
    }
}
