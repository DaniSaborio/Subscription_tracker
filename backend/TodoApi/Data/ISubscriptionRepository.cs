using TodoApi.Models;

namespace TodoApi.Data;

public interface ISubscriptionRepository
{
    Task<IEnumerable<SubscriptionItem>> GetAllAsync(Guid userId, string? search, string? category, string? billingCycle, int? upcomingDays);
    Task<IEnumerable<SubscriptionItem>> GetUpcomingAsync(Guid userId, int days);
    Task<SubscriptionItem?> GetByIdAsync(Guid userId, Guid id);
    Task<SubscriptionItem> CreateAsync(Guid userId, CreateSubscriptionRequest request);
    Task<SubscriptionItem?> UpdateAsync(Guid userId, Guid id, UpdateSubscriptionRequest request);
    Task<bool> DeleteAsync(Guid userId, Guid id);
    Task<bool> ShareAsync(Guid ownerUserId, Guid subscriptionId, Guid sharedWithUserId, string sharedWithEmail);
    Task<bool> RevokeShareAsync(Guid ownerUserId, Guid subscriptionId, Guid sharedWithUserId);
    Task<IEnumerable<TodoApi.Models.SubscriptionShareDto>> GetSharesAsync(Guid ownerUserId, Guid subscriptionId);
}