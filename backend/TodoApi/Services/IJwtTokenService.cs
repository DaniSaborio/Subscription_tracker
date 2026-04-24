using TodoApi.Models;

namespace TodoApi.Services;

public interface IJwtTokenService
{
    AuthResponse GenerateToken(UserAccount user);
}