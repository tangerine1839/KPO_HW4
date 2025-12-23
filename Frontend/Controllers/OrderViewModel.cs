namespace Frontend.Controllers;

public class OrderViewModel
{
    public Guid Id { get; set; } 
    public decimal Amount { get; set; } 
    public string Status { get; set; } 
    public string Description { get; set; } 
}