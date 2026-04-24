namespace TodoApi.Models;

public class SubscriptionSummaryResponse
{
    public decimal MonthlyEquivalentTotal { get; set; }
    public decimal YearlyEquivalentTotal { get; set; }
    public Dictionary<string, decimal> TotalsByBillingCycle { get; set; } = new();
    public int UpcomingIn30Days { get; set; }
}