using System.Net.Http.Json;
using System.Text.Json;
using Subscription_tracker.Models;

namespace Subscription_tracker.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly IPreferences _preferences;

    public ApiService(IPreferences preferences)
    {
        _preferences = preferences;
        _baseUrl = "http://localhost:5000"; // Docker API
        _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
    }

    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<AuthResponse> RegisterAsync(string email, string password, string confirmPassword)
    {
        var request = new { Email = email, Password = password, ConfirmPassword = confirmPassword };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(json) ?? throw new InvalidOperationException("Failed to parse auth response");
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var request = new { Email = email, Password = password };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(json) ?? throw new InvalidOperationException("Failed to parse auth response");
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        var response = await _httpClient.GetAsync("/api/subscriptions");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Subscription>>(json) ?? [];
    }

    public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/subscriptions", subscription);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Subscription>(json) ?? throw new InvalidOperationException("Failed to parse subscription response");
    }

    public async Task UpdateSubscriptionAsync(Subscription subscription)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/subscriptions/{subscription.Id}", subscription);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteSubscriptionAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"/api/subscriptions/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task SyncChangesAsync(List<SyncChange> changes)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/subscriptions/sync", changes);
        response.EnsureSuccessStatusCode();
    }

    public bool IsConnected()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }
}
