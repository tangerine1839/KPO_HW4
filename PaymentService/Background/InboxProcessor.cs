namespace PaymentService.Background;


using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;


public class InboxProcessor : BackgroundService
{
    private readonly IServiceProvider _sp;
    public InboxProcessor(IServiceProvider sp) => _sp = sp;

    protected override async Task ExecuteAsync(CancellationToken st)
    {
        while (!st.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                
                var msgs = await db.InboxMessages.Where(m => !m.Processed).Take(10).ToListAsync(st);

                foreach (var msg in msgs)
                {
                    using var tran = await db.Database.BeginTransactionAsync();
                    
                    var data = JsonSerializer.Deserialize<OrderEventDto>(msg.Data);
                    var acc = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == data.UserId);
                    
                    bool success = false;
                    if (acc != null && acc.Balance >= data.Amount)
                    {
                        acc.Balance -= data.Amount;
                        success = true;
                    }

                    var outbox = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        Data = JsonSerializer.Serialize(new { OrderId = data.OrderId, IsSuccess = success })
                    };

                    db.OutboxMessages.Add(outbox);
                    msg.Processed = true;
                    
                    await db.SaveChangesAsync();
                    await tran.CommitAsync();
                }
            }
            catch {}
            
            await Task.Delay(500, st);
        }
    }
    record OrderEventDto(Guid OrderId, Guid UserId, decimal Amount);
}
