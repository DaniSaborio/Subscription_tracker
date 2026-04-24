namespace TodoApi.Models;

public class SubscriptionItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = "monthly";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime NextBillingDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}