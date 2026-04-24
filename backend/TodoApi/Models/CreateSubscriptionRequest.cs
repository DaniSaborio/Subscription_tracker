namespace TodoApi.Models;

public class CreateSubscriptionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = "monthly";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime NextBillingDate { get; set; }
    public string? Notes { get; set; }
}