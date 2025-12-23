namespace PaymentService.Domain;

public class InboxMessage
{
    public Guid Id { get; set; } 
    public string Data { get; set; } = string.Empty;
    public bool Processed { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}