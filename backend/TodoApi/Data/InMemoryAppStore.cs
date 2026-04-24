using TodoApi.Models;
using Microsoft.AspNetCore.Identity;

namespace TodoApi.Data;

public static class InMemoryAppStore
{
    private static readonly object Sync = new();
    private static readonly List<UserAccount> Users = new();
    private static readonly List<SubscriptionItem> Subscriptions = new();

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
            return Subscriptions.Where(s => s.UserId == userId).Select(CloneSubscription).ToList();
        }
    }

    public static SubscriptionItem? GetSubscription(Guid userId, Guid id)
    {
        lock (Sync)
        {
            var item = Subscriptions.FirstOrDefault(s => s.UserId == userId && s.Id == id);
            return item is null ? null : CloneSubscription(item);
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
            return true;
        }
    }

    private static SubscriptionItem CloneSubscription(SubscriptionItem item) => new()
    {
        Id = item.Id,
        UserId = item.UserId,
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
}