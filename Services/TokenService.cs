using System.IdentityModel.Tokens.Jwt;
using Subscription_tracker.Models;

namespace Subscription_tracker.Services;

public class TokenService
{
    private readonly IPreferences _preferences;
    private const string TokenKey = "auth_token";
    private const string UserIdKey = "user_id";
    private const string UserEmailKey = "user_email";
    private const string TokenExpiryKey = "token_expiry";

    public TokenService(IPreferences preferences)
    {
        _preferences = preferences;
    }

    public void SaveToken(AuthResponse response)
    {
        _preferences.Set(TokenKey, response.Token);
        _preferences.Set(UserIdKey, response.User.Id);
        _preferences.Set(UserEmailKey, response.User.Email);
        // Set expiry to 24 hours from now
        _preferences.Set(TokenExpiryKey, DateTime.UtcNow.AddHours(24).Ticks);
    }

    public string GetToken()
    {
        return _preferences.Get(TokenKey, string.Empty);
    }

    public int GetUserId()
    {
        return _preferences.Get(UserIdKey, 0);
    }

    public string GetUserEmail()
    {
        return _preferences.Get(UserEmailKey, string.Empty);
    }

    public bool IsTokenValid()
    {
        var token = GetToken();
        if (string.IsNullOrEmpty(token)) return false;

        var expiryTicks = _preferences.Get(TokenExpiryKey, 0L);
        if (expiryTicks == 0) return false;

        var expiry = new DateTime(expiryTicks, DateTimeKind.Utc);
        return expiry > DateTime.UtcNow;
    }

    public void ClearToken()
    {
        _preferences.Remove(TokenKey);
        _preferences.Remove(UserIdKey);
        _preferences.Remove(UserEmailKey);
        _preferences.Remove(TokenExpiryKey);
    }

    public bool IsUserLoggedIn()
    {
        return IsTokenValid() && !string.IsNullOrEmpty(GetToken());
    }
}
