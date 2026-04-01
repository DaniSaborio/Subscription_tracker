using Subscription_tracker.Models;

namespace Subscription_tracker.Services;

public class SyncService
{
    private readonly ApiService _apiService;
    private readonly LocalStorageService _localStorage;
    private readonly TokenService _tokenService;
    private CancellationTokenSource _syncCancellation = new();

    public SyncService(
        ApiService apiService,
        LocalStorageService localStorage,
        TokenService tokenService)
    {
        _apiService = apiService;
        _localStorage = localStorage;
        _tokenService = tokenService;
    }

    public bool IsOnline => _apiService.IsConnected();

    public event EventHandler<bool> ConnectivityChanged;

    public async Task InitializeAsync()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

        // If online, sync pending changes
        if (IsOnline)
        {
            await SyncPendingChangesAsync();
        }
    }

    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                _ = SyncPendingChangesAsync();
                ConnectivityChanged?.Invoke(this, true);
            }
            else
            {
                ConnectivityChanged?.Invoke(this, false);
            }
        });
    }

    public async Task SaveSubscriptionAsync(Subscription subscription)
    {
        if (IsOnline)
        {
            try
            {
                if (subscription.Id == null || subscription.Id == 0)
                {
                    var created = await _apiService.CreateSubscriptionAsync(subscription);
                    subscription.Id = created.Id;
                }
                else
                {
                    await _apiService.UpdateSubscriptionAsync(subscription);
                }
            }
            catch
            {
                // Si falla, guardar localmente para sincronización posterior
                await _localStorage.SavePendingChangeAsync(new SyncChange
                {
                    OperationType = subscription.Id == null ? "create" : "update",
                    EntityType = "subscription",
                    Payload = subscription
                });
            }
        }
        else
        {
            // Si está offline, guardar pendiente
            await _localStorage.SavePendingChangeAsync(new SyncChange
            {
                OperationType = subscription.Id == null ? "create" : "update",
                EntityType = "subscription",
                Payload = subscription
            });
        }

        // Guardar localmente siempre
        if (subscription.Id == null)
            await _localStorage.AddSubscriptionLocallyAsync(subscription);
        else
            await _localStorage.UpdateSubscriptionLocallyAsync(subscription);
    }

    public async Task DeleteSubscriptionAsync(int id)
    {
        if (IsOnline)
        {
            try
            {
                await _apiService.DeleteSubscriptionAsync(id);
            }
            catch
            {
                await _localStorage.SavePendingChangeAsync(new SyncChange
                {
                    OperationType = "delete",
                    EntityType = "subscription",
                    Payload = new { id }
                });
            }
        }
        else
        {
            await _localStorage.SavePendingChangeAsync(new SyncChange
            {
                OperationType = "delete",
                EntityType = "subscription",
                Payload = new { id }
            });
        }

        await _localStorage.DeleteSubscriptionLocallyAsync(id);
    }

    public async Task<List<Subscription>> GetSubscriptionsAsync()
    {
        if (IsOnline)
        {
            try
            {
                var subscriptions = await _apiService.GetSubscriptionsAsync();
                await _localStorage.SaveSubscriptionsAsync(subscriptions);
                return subscriptions;
            }
            catch
            {
                return await _localStorage.GetSubscriptionsAsync();
            }
        }

        return await _localStorage.GetSubscriptionsAsync();
    }

    private async Task SyncPendingChangesAsync()
    {
        try
        {
            var pendingChanges = await _localStorage.GetPendingChangesAsync();
            if (pendingChanges.Count == 0) return;

            await _apiService.SyncChangesAsync(pendingChanges);
            await _localStorage.ClearPendingChangesAsync();

            // Refrescar datos desde API
            var subscriptions = await _apiService.GetSubscriptionsAsync();
            await _localStorage.SaveSubscriptionsAsync(subscriptions);
        }
        catch (Exception ex)
        {
            // Log but don't throw - retry on next sync attempt
            System.Diagnostics.Debug.WriteLine($"Sync failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        _syncCancellation?.Cancel();
        _syncCancellation?.Dispose();
    }
}
