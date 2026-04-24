using System.Data;
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

        const string sql = @"
            SELECT
                id,
                user_id AS UserId,
                name,
                category,
                billing_cycle AS BillingCycle,
                amount,
                currency,
                next_billing_date AS NextBillingDate,
                notes,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM subscriptions
            WHERE user_id = @UserId
              AND (@Search IS NULL OR name ILIKE '%' || @Search || '%' OR category ILIKE '%' || @Search || '%')
              AND (@Category IS NULL OR category = @Category)
              AND (@BillingCycle IS NULL OR billing_cycle = @BillingCycle)
              AND (
                    @UpcomingToDate IS NULL
                    OR (next_billing_date >= @TodayDate AND next_billing_date <= @UpcomingToDate)
                  )
            ORDER BY next_billing_date ASC, updated_at DESC;";

        var nowDate = DateTime.UtcNow.Date;
        DateTime? upcomingToDate = null;
        if (upcomingDays is > 0)
        {
            upcomingToDate = nowDate.AddDays(upcomingDays.Value);
        }

        using var connection = CreateConnection();
        return await connection.QueryAsync<SubscriptionItem>(sql, new
        {
            UserId = userId,
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            BillingCycle = string.IsNullOrWhiteSpace(billingCycle) ? null : billingCycle.Trim().ToLowerInvariant(),
            TodayDate = nowDate,
            UpcomingToDate = upcomingToDate
        });
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
            SELECT
                id,
                user_id AS UserId,
                name,
                category,
                billing_cycle AS BillingCycle,
                amount,
                currency,
                next_billing_date AS NextBillingDate,
                notes,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM subscriptions
            WHERE user_id = @UserId
              AND id = @Id;";

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