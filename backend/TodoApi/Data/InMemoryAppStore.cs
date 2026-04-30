using TodoApi.Models;
using Microsoft.AspNetCore.Identity;

namespace TodoApi.Data;

public static class InMemoryAppStore
{
    private static readonly object Sync = new();
    private static readonly List<UserAccount> Users = new();
    private static readonly List<SubscriptionItem> Subscriptions = new();
    private static readonly List<SubscriptionShare> Shares = new();

    private sealed record SubscriptionShare(Guid SubscriptionId, Guid SharedWithUserId, Guid SharedByUserId, DateTime CreatedAt);

    static InMemoryAppStore()
    {
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var demoUser = new UserAccount
        {
            Id = userId,
            Email = "demo@tracker.app",
            CreatedAt = DateTime.UtcNow
        };

        demoUser.PasswordHash = new PasswordHasher<UserAccount>().HashPassword(demoUser, "Secret123!");

        Users.Add(new UserAccount
        {
            Id = demoUser.Id,
            Email = demoUser.Email,
            PasswordHash = demoUser.PasswordHash,
            CreatedAt = demoUser.CreatedAt
        });

        Subscriptions.AddRange(new[]
        {
            new SubscriptionItem
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = userId,
                Name = "Netflix",
                Category = "Streaming",
                BillingCycle = "monthly",
                Amount = 12.99m,
                Currency = "USD",
                NextBillingDate = DateTime.UtcNow.Date.AddDays(7),
                Notes = "Plan estandar",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SubscriptionItem
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UserId = userId,
                Name = "Spotify",
                Category = "Music",
                BillingCycle = "monthly",
                Amount = 9.99m,
                Currency = "USD",
                NextBillingDate = DateTime.UtcNow.Date.AddDays(12),
                Notes = "Cuenta personal",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SubscriptionItem
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = userId,
                Name = "Amazon Prime",
                Category = "Shopping",
                BillingCycle = "yearly",
                Amount = 119.00m,
                Currency = "USD",
                NextBillingDate = DateTime.UtcNow.Date.AddDays(25),
                Notes = "Facturacion anual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        });
    }

    public static UserAccount? GetUserByEmail(string email)
    {
        lock (Sync)
        {
            return Users.FirstOrDefault(u => u.Email.Equals(email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public static UserAccount CreateUser(string email, string passwordHash)
    {
        lock (Sync)
        {
            var user = new UserAccount
            {
                Id = Guid.NewGuid(),
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            Users.Add(user);
            return user;
        }
    }

    public static List<SubscriptionItem> GetSubscriptions(Guid userId)
    {
        lock (Sync)
        {
            return GetAccessibleSubscriptions(userId).ToList();
        }
    }

    public static SubscriptionItem? GetSubscription(Guid userId, Guid id)
    {
        lock (Sync)
        {
            return GetAccessibleSubscriptions(userId).FirstOrDefault(s => s.Id == id);
        }
    }

    public static SubscriptionItem CreateSubscription(Guid userId, CreateSubscriptionRequest request)
    {
        lock (Sync)
        {
            var now = DateTime.UtcNow;
            var item = new SubscriptionItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.Name.Trim(),
                Category = request.Category.Trim(),
                BillingCycle = request.BillingCycle.Trim().ToLowerInvariant(),
                Amount = request.Amount,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                NextBillingDate = request.NextBillingDate.Date,
                Notes = request.Notes,
                CreatedAt = now,
                UpdatedAt = now
            };

            Subscriptions.Add(item);
            return CloneSubscription(item);
        }
    }

    public static SubscriptionItem? UpdateSubscription(Guid userId, Guid id, UpdateSubscriptionRequest request)
    {
        lock (Sync)
        {
            var item = Subscriptions.FirstOrDefault(s => s.UserId == userId && s.Id == id);
            if (item is null)
            {
                return null;
            }

            item.Name = request.Name.Trim();
            item.Category = request.Category.Trim();
            item.BillingCycle = request.BillingCycle.Trim().ToLowerInvariant();
            item.Amount = request.Amount;
            item.Currency = request.Currency.Trim().ToUpperInvariant();
            item.NextBillingDate = request.NextBillingDate.Date;
            item.Notes = request.Notes;
            item.UpdatedAt = DateTime.UtcNow;

            return CloneSubscription(item);
        }
    }

    public static bool DeleteSubscription(Guid userId, Guid id)
    {
        lock (Sync)
        {
            var item = Subscriptions.FirstOrDefault(s => s.UserId == userId && s.Id == id);
            if (item is null)
            {
                return false;
            }

            Subscriptions.Remove(item);
            Shares.RemoveAll(share => share.SubscriptionId == id);
            return true;
        }
    }

    public static bool ShareSubscription(Guid ownerUserId, Guid subscriptionId, Guid sharedWithUserId, string sharedWithEmail)
    {
        lock (Sync)
        {
            var item = Subscriptions.FirstOrDefault(s => s.UserId == ownerUserId && s.Id == subscriptionId);
            if (item is null || ownerUserId == sharedWithUserId)
            {
                return false;
            }

            if (Shares.Any(share => share.SubscriptionId == subscriptionId && share.SharedWithUserId == sharedWithUserId))
            {
                return false;
            }

            Shares.Add(new SubscriptionShare(subscriptionId, sharedWithUserId, ownerUserId, DateTime.UtcNow));
            return true;
        }
    }

    public static bool RevokeShare(Guid ownerUserId, Guid subscriptionId, Guid sharedWithUserId)
    {
        lock (Sync)
        {
            var item = Subscriptions.FirstOrDefault(s => s.UserId == ownerUserId && s.Id == subscriptionId);
            if (item is null)
            {
                return false;
            }

            var share = Shares.FirstOrDefault(x => x.SubscriptionId == subscriptionId && x.SharedWithUserId == sharedWithUserId);
            if (share is null)
            {
                return false;
            }

            Shares.Remove(share);
            return true;
        }
    }

    public static IEnumerable<TodoApi.Models.SubscriptionShareDto> GetShares(Guid ownerUserId, Guid subscriptionId)
    {
        lock (Sync)
        {
            var item = Subscriptions.FirstOrDefault(s => s.UserId == ownerUserId && s.Id == subscriptionId);
            if (item is null)
            {
                return Enumerable.Empty<TodoApi.Models.SubscriptionShareDto>();
            }

            var result = from share in Shares
                         where share.SubscriptionId == subscriptionId
                         join user in Users on share.SharedWithUserId equals user.Id
                         select new TodoApi.Models.SubscriptionShareDto
                         {
                             SharedWithUserId = user.Id,
                             SharedWithEmail = user.Email,
                             CreatedAt = share.CreatedAt
                         };

            return result.ToList();
        }
    }

    private static SubscriptionItem CloneSubscription(SubscriptionItem item, bool isOwner = true, string? sharedByEmail = null) => new()
    {
        Id = item.Id,
        UserId = item.UserId,
        IsOwner = isOwner,
        SharedByEmail = sharedByEmail,
        Name = item.Name,
        Category = item.Category,
        BillingCycle = item.BillingCycle,
        Amount = item.Amount,
        Currency = item.Currency,
        NextBillingDate = item.NextBillingDate,
        Notes = item.Notes,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };

    private static List<SubscriptionItem> GetAccessibleSubscriptions(Guid userId)
    {
        var owned = Subscriptions
            .Where(subscription => subscription.UserId == userId)
            .Select(subscription => CloneSubscription(subscription));

        var shared = from share in Shares
                      where share.SharedWithUserId == userId
                      join subscription in Subscriptions on share.SubscriptionId equals subscription.Id
                      join owner in Users on subscription.UserId equals owner.Id
                      select CloneSubscription(subscription, false, owner.Email);

        return owned.Concat(shared).OrderBy(x => x.NextBillingDate).ThenByDescending(x => x.UpdatedAt).ToList();
    }
}