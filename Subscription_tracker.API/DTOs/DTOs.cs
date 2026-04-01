namespace Subscription_tracker.API.DTOs;

public record LoginDto(
    string Email,
    string Password
);

public record RegisterDto(
    string Email,
    string Password,
    string ConfirmPassword
);

public record AuthResponseDto(
    string Token,
    UserDto User
);

public record UserDto(
    int Id,
    string Email
);

public record SubscriptionDto(
    int? Id,
    string ServiceName,
    string Category,
    decimal Amount,
    string BillingCycle,
    DateTime NextBillingDate,
    bool IsActive,
    string? PaymentMethod,
    string? Notes
);

public record SyncChangeDto(
    string OperationType, // "create", "update", "delete"
    string EntityType,     // "subscription"
    object Payload
);
