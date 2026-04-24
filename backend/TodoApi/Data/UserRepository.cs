using System.Data;
using Dapper;
using Npgsql;
using TodoApi.Models;

namespace TodoApi.Data;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task<UserAccount?> GetByEmailAsync(string email)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.GetUserByEmail(email);
        }

        const string sql = @"
            SELECT
                id,
                email,
                password_hash AS PasswordHash,
                created_at AS CreatedAt
            FROM users
            WHERE email = @Email;";

        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { Email = email.Trim().ToLowerInvariant() });
    }

    public async Task<UserAccount> CreateAsync(string email, string passwordHash)
    {
        if (!await CanUseDatabaseAsync())
        {
            return InMemoryAppStore.CreateUser(email, passwordHash);
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        const string sql = @"
            INSERT INTO users (id, email, password_hash, created_at)
            VALUES (@Id, @Email, @PasswordHash, @CreatedAt)
            RETURNING
                id,
                email,
                password_hash AS PasswordHash,
                created_at AS CreatedAt;";

        var parameters = new
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        using var connection = CreateConnection();
        return await connection.QuerySingleAsync<UserAccount>(sql, parameters);
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