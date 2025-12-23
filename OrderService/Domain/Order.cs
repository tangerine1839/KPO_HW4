namespace OrderService.Domain;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "NEW"; 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}