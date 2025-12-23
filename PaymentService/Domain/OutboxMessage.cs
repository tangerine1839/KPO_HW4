namespace PaymentService.Domain;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}