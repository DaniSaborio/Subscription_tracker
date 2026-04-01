namespace Subscription_tracker.API.Models;

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string ServiceName { get; set; }
    public required string Category { get; set; }
    public decimal Amount { get; set; }
    public required string BillingCycle { get; set; } // "Monthly", "Biweekly"
    public DateTime NextBillingDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
