using TodoApi.Models;

namespace TodoApi.Data;

public interface IUserRepository
{
    Task<UserAccount?> GetByEmailAsync(string email);
    Task<UserAccount> CreateAsync(string email, string passwordHash);
}