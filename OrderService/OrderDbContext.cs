namespace OrderService;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) 
    {
        Database.EnsureCreated();
    }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
}