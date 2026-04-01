namespace Subscription_tracker.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
}

public class Subscription
{
    public int? Id { get; set; }
    public string ServiceName { get; set; }
    public string Category { get; set; }
    public decimal Amount { get; set; }
    public string BillingCycle { get; set; }
    public DateTime NextBillingDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string PaymentMethod { get; set; }
    public string Notes { get; set; }

    public override string ToString() => $"{ServiceName} - {Amount:C} ({BillingCycle})";
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; }
    public User User { get; set; }
}

public class SyncChange
{
    public string OperationType { get; set; } // "create", "update", "delete"
    public string EntityType { get; set; }     // "subscription"
    public object Payload { get; set; }
}
