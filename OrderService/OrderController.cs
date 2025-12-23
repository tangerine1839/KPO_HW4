namespace OrderService;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain;
using System.Text.Json;


public record CreateOrderDto(Guid UserId, decimal Amount, string Description);

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly OrderDbContext _context;

    public OrderController(OrderDbContext context) => _context = context;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest("Amount must be positive");
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Amount = dto.Amount,
                Description = dto.Description,
                Status = "NEW"
            };

            var eventData = new 
            { 
                OrderId = order.Id, 
                UserId = dto.UserId, 
                Amount = dto.Amount 
            };

            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "OrderCreated",
                Data = JsonSerializer.Serialize(eventData)
            };

            _context.Orders.Add(order);
            _context.OutboxMessages.Add(outbox);
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(order.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(Guid userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(orders);
    }
}
