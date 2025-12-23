namespace PaymentService;

using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;


public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions options) : base(options) { Database.EnsureCreated(); }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
}
