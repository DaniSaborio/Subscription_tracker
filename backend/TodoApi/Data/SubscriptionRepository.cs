using System.Data;
using System.Text;
using Dapper;
using Npgsql;
using TodoApi.Models;

namespace TodoApi.Data;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly string _connectionString;

    public SubscriptionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task<IEnumerable<SubscriptionItem>> GetAllAsync(Guid userId, string? search, string? category, string? billingCycle, int? upcomingDays)
    {
        if (!await CanUseDatabaseAsync())
        {
            var items = InMemoryAppStore.GetSubscriptions(userId).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                items = items.Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Category.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                items = items.Where(x => x.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(billingCycle))
            {
                items = items.Where(x => x.BillingCycle.Equals(billingCycle.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (upcomingDays is > 0)
            {
                var limit = DateTime.UtcNow.Date.AddDays(upcomingDays.Value);
                items = items.Where(x => x.NextBillingDate.Date >= DateTime.UtcNow.Date && x.NextBillingDate.Date <= limit);
            }

            return items.OrderBy(x => x.NextBillingDate).ThenByDescending(x => x.UpdatedAt);
        }

        var sqlBuilder = new StringBuilder(@"
            WITH accessible_subscriptions AS (
                SELECT
                    s.id,
                    s.user_id,
                    TRUE AS is_owner,
                    NULL::text AS shared_by_email,
                    s.name,
                    s.category,
                    s.billing_cycle,
                    s.amount,
                    s.currency,
                    s.next_billing_date,
                    s.notes,
                    s.created_at,
                    s.updated_at
                FROM subscriptions s
                WHERE s.user_id = @UserId

                UNION ALL

                SELECT
                    s.id,
                    s.user_id,
                    FALSE AS is_owner,
                    owner.email AS shared_by_email,
                    s.name,
                    s.category,
                    s.billing_cycle,
                    s.amount,
                    s.currency,
                    s.next_billing_date,
                    s.notes,
                    s.created_at,
                    s.updated_at
                FROM subscriptions s
                INNER JOIN subscription_shares share ON share.subscription_id = s.id AND share.shared_with_user_id = @UserId
                INNER JOIN users owner ON owner.id = s.user_id
            )
            SELECT
                id,
                user_id AS UserId,
                is_owner AS IsOwner,
                shared_by_email AS SharedByEmail,
                name,
                category,
                billing_cycle AS BillingCycle,
                amount,
                currency,
                next_billing_date AS NextBillingDate,
                notes,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM accessible_subscriptions
            WHERE 1 = 1");

        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            sqlBuilder.Append("\n  AND (name ILIKE '%' || @Search || '%' OR category ILIKE '%' || @Search || '%')");
            parameters.Add("Search", normalizedSearch);
        }

        var normalizedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        if (!string.IsNullOrEmpty(normalizedCategory))
        {
            sqlBuilder.Append("\n  AND category = @Category");
            parameters.Add("Category", normalizedCategory);
        }

        var normalizedBillingCycle = string.IsNullOrWhiteSpace(billingCycle) ? null : billingCycle.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(normalizedBillingCycle))
        {
            sqlBuilder.Append("\n  AND billing_cycle = @BillingCycle");
            parameters.Add("BillingCycle", normalizedBillingCycle);
        }

        if (upcomingDays is > 0)
        {
            var todayDate = DateTime.UtcNow.Date;
            var upcomingToDate = todayDate.AddDays(upcomingDays.Value);

            sqlBuilder.Append("\n  AND next_billing_date >= @TodayDate AND next_billing_date <= @UpcomingToDate");
            parameters.Add("TodayDate", todayDate);
            parameters.Add("UpcomingToDate", upcomingToDate);
        }

        sqlBuilder.Append("\nORDER BY next_billing_date ASC, updated_at DESC;");

        using var connection = CreateConnection();
        return await connection.QueryAsync<SubscriptionItem>(sqlBuilder.ToString(), parameters);
    }

    public async Task<IEnumerable<SubscriptionItem>> GetUpcomingAsync(Guid userId, int days)
    {
        var safeDays = days <= 0 ? 30 : days;
        return await GetAllAsync(userId, null, null, null, safeDays);
    }

    public async Task<SubscriptionItem?> GetByIdAsync(Guid userId, Guid id)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.GetSubscription(userId, id);
        }

        const string sql = @"
            WITH accessible_subscriptions AS (
                SELECT
                    s.id,
                    s.user_id,
                    TRUE AS is_owner,
                    NULL::text AS shared_by_email,
                    s.name,
                    s.category,
                    s.billing_cycle,
                    s.amount,
                    s.currency,
                    s.next_billing_date,
                    s.notes,
                    s.created_at,
                    s.updated_at
                FROM subscriptions s
                WHERE s.user_id = @UserId

                UNION ALL

                SELECT
                    s.id,
                    s.user_id,
                    FALSE AS is_owner,
                    owner.email AS shared_by_email,
                    s.name,
                    s.category,
                    s.billing_cycle,
                    s.amount,
                    s.currency,
                    s.next_billing_date,
                    s.notes,
                    s.created_at,
                    s.updated_at
                FROM subscriptions s
                INNER JOIN subscription_shares share ON share.subscription_id = s.id AND share.shared_with_user_id = @UserId
                INNER JOIN users owner ON owner.id = s.user_id
            )
            SELECT
                id,
                user_id AS UserId,
                is_owner AS IsOwner,
                shared_by_email AS SharedByEmail,
                name,
                category,
                billing_cycle AS BillingCycle,
                amount,
                currency,
                next_billing_date AS NextBillingDate,
                notes,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM accessible_subscriptions
            WHERE id = @Id;";

        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SubscriptionItem>(sql, new { UserId = userId, Id = id });
    }

    public async Task<SubscriptionItem> CreateAsync(Guid userId, CreateSubscriptionRequest request)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.CreateSubscription(userId, request);
        }

        const string sql = @"
            INSERT INTO subscriptions (
                id,
                user_id,
                name,
                category,
                billing_cycle,
                amount,
                currency,
                next_billing_date,
                notes,
                created_at,
                updated_at
            )
            VALUES (
                @Id,
                @UserId,
                @Name,
                @Category,
                @BillingCycle,
                @Amount,
                @Currency,
                @NextBillingDate,
                @Notes,
                @CreatedAt,
                @UpdatedAt
            )
            RETURNING
                id,
                user_id AS UserId,
                TRUE AS IsOwner,
                NULL::text AS SharedByEmail,
                name,
                category,
                billing_cycle AS BillingCycle,
                amount,
                currency,
                next_billing_date AS NextBillingDate,
                notes,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt;";

        var now = DateTime.UtcNow;

        using var connection = CreateConnection();
        return await connection.QuerySingleAsync<SubscriptionItem>(sql, new
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
        });
    }

    public async Task<SubscriptionItem?> UpdateAsync(Guid userId, Guid id, UpdateSubscriptionRequest request)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.UpdateSubscription(userId, id, request);
        }

        const string sql = @"
            UPDATE subscriptions
            SET
                name = @Name,
                category = @Category,
                billing_cycle = @BillingCycle,
                amount = @Amount,
                currency = @Currency,
                next_billing_date = @NextBillingDate,
                notes = @Notes,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId
              AND id = @Id
            RETURNING
                id,
                user_id AS UserId,
                TRUE AS IsOwner,
                NULL::text AS SharedByEmail,
                name,
                category,
                billing_cycle AS BillingCycle,
                amount,
                currency,
                next_billing_date AS NextBillingDate,
                notes,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt;";

        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SubscriptionItem>(sql, new
        {
            UserId = userId,
            Id = id,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            BillingCycle = request.BillingCycle.Trim().ToLowerInvariant(),
            Amount = request.Amount,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            NextBillingDate = request.NextBillingDate.Date,
            Notes = request.Notes,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.DeleteSubscription(userId, id);
        }

        const string sql = @"
            DELETE FROM subscriptions
            WHERE user_id = @UserId
              AND id = @Id;";

        using var connection = CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new { UserId = userId, Id = id });
        return rows > 0;
    }

    public async Task<bool> ShareAsync(Guid ownerUserId, Guid subscriptionId, Guid sharedWithUserId, string sharedWithEmail)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.ShareSubscription(ownerUserId, subscriptionId, sharedWithUserId, sharedWithEmail);
        }

        const string sql = @"
            INSERT INTO subscription_shares (
                id,
                subscription_id,
                shared_with_user_id,
                shared_by_user_id,
                created_at
            )
            SELECT
                @Id,
                s.id,
                @SharedWithUserId,
                @SharedByUserId,
                @CreatedAt
            FROM subscriptions s
            WHERE s.id = @SubscriptionId
              AND s.user_id = @SharedByUserId
            ON CONFLICT (subscription_id, shared_with_user_id) DO NOTHING
            RETURNING id;";

        using var connection = CreateConnection();
        var insertedId = await connection.QueryFirstOrDefaultAsync<Guid?>(sql, new
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            SharedWithUserId = sharedWithUserId,
            SharedByUserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        });

        return insertedId.HasValue;
    }

    public async Task<bool> RevokeShareAsync(Guid ownerUserId, Guid subscriptionId, Guid sharedWithUserId)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.RevokeShare(ownerUserId, subscriptionId, sharedWithUserId);
        }

        const string sql = @"
            DELETE FROM subscription_shares
            WHERE subscription_id = @SubscriptionId
              AND shared_with_user_id = @SharedWithUserId
              AND shared_by_user_id = @SharedByUserId;";

        using var connection = CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new
        {
            SubscriptionId = subscriptionId,
            SharedWithUserId = sharedWithUserId,
            SharedByUserId = ownerUserId
        });

        return rows > 0;
    }

    public async Task<IEnumerable<TodoApi.Models.SubscriptionShareDto>> GetSharesAsync(Guid ownerUserId, Guid subscriptionId)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.GetShares(ownerUserId, subscriptionId);
        }

        const string sql = @"
            SELECT
                s.shared_with_user_id AS SharedWithUserId,
                u.email AS SharedWithEmail,
                s.created_at AS CreatedAt
            FROM subscription_shares s
            INNER JOIN subscriptions sub ON sub.id = s.subscription_id AND sub.user_id = @OwnerUserId
            INNER JOIN users u ON u.id = s.shared_with_user_id
            WHERE s.subscription_id = @SubscriptionId
            ORDER BY s.created_at ASC;";

        using var connection = CreateConnection();
        return await connection.QueryAsync<TodoApi.Models.SubscriptionShareDto>(sql, new { OwnerUserId = ownerUserId, SubscriptionId = subscriptionId });
    }

    private async Task<bool> CanUseDatabaseAsync()
    {
        try
        {
            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT 1;") == 1;
        }
        catch
        {
            return false;
        }
    }
}