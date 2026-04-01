using Subscription_tracker.Models;
using System.Text.Json;

namespace Subscription_tracker.Services;

public class LocalStorageService
{
    private readonly string _dbPath;
    private const string SubscriptionsKey = "subscriptions";
    private const string PendingChangesKey = "pending_changes";

    public LocalStorageService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "subscription_tracker.db");
    }

    public async Task SaveSubscriptionsAsync(List<Subscription> subscriptions)
    {
        var json = JsonSerializer.Serialize(subscriptions);
        var preferencesFile = Path.Combine(FileSystem.AppDataDirectory, "preferences.json");
        await File.WriteAllTextAsync(preferencesFile, json);
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        try
        {
            var preferencesFile = Path.Combine(FileSystem.AppDataDirectory, "preferences.json");
            if (!File.Exists(preferencesFile)) return [];

            var json = await File.ReadAllTextAsync(preferencesFile);
            return JsonSerializer.Deserialize<List<Subscription>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task SavePendingChangeAsync(SyncChange change)
    {
        var changes = await GetPendingChangesAsync();
        changes.Add(change);

        var json = JsonSerializer.Serialize(changes);
        var changesFile = Path.Combine(FileSystem.AppDataDirectory, "pending_changes.json");
        await File.WriteAllTextAsync(changesFile, json);
    }

    public async Task<List<SyncChange>> GetPendingChangesAsync()
    {
        try
        {
            var changesFile = Path.Combine(FileSystem.AppDataDirectory, "pending_changes.json");
            if (!File.Exists(changesFile)) return [];

            var json = await File.ReadAllTextAsync(changesFile);
            return JsonSerializer.Deserialize<List<SyncChange>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task ClearPendingChangesAsync()
    {
        var changesFile = Path.Combine(FileSystem.AppDataDirectory, "pending_changes.json");
        if (File.Exists(changesFile))
            File.Delete(changesFile);

        await Task.CompletedTask;
    }

    public async Task AddSubscriptionLocallyAsync(Subscription subscription)
    {
        var subscriptions = await GetSubscriptionsAsync();
        subscription.Id = subscriptions.Count > 0 ? subscriptions.Max(s => s.Id ?? 0) + 1 : 1;
        subscriptions.Add(subscription);
        await SaveSubscriptionsAsync(subscriptions);
    }

    public async Task UpdateSubscriptionLocallyAsync(Subscription subscription)
    {
        var subscriptions = await GetSubscriptionsAsync();
        var index = subscriptions.FindIndex(s => s.Id == subscription.Id);
        if (index >= 0)
        {
            subscriptions[index] = subscription;
        }
        await SaveSubscriptionsAsync(subscriptions);
    }

    public async Task DeleteSubscriptionLocallyAsync(int id)
    {
        var subscriptions = await GetSubscriptionsAsync();
        subscriptions.RemoveAll(s => s.Id == id);
        await SaveSubscriptionsAsync(subscriptions);
    }
}
