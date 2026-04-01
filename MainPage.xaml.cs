using System.Collections.ObjectModel;
using Subscription_tracker.Models;
using Subscription_tracker.Services;

namespace Subscription_tracker;

public partial class MainPage : ContentPage
{
    private readonly SyncService _syncService;
    private readonly TokenService _tokenService;
    private ObservableCollection<Subscription> _subscriptions = [];
    private List<Subscription> _allSubscriptions = [];

    public MainPage()
    {
        InitializeComponent();
        _syncService = ServiceHelper.GetService<SyncService>();
        _tokenService = ServiceHelper.GetService<TokenService>();
        SubscriptionsCollection.ItemsSource = _subscriptions;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Check if user is logged in
        if (!_tokenService.IsUserLoggedIn())
        {
            Shell.Current.GoToAsync("login");
            return;
        }

        await LoadSubscriptions();
        _syncService.ConnectivityChanged += OnConnectivityChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _syncService.ConnectivityChanged -= OnConnectivityChanged;
    }

    private async Task LoadSubscriptions()
    {
        try
        {
            _allSubscriptions = await _syncService.GetSubscriptionsAsync();
            UpdateUI();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load subscriptions: {ex.Message}", "OK");
        }
    }

    private void UpdateUI()
    {
        _subscriptions.Clear();
        foreach (var sub in _allSubscriptions)
        {
            _subscriptions.Add(sub);
        }

        // Calculate totals
        var monthlyTotal = _allSubscriptions
            .Where(s => s.BillingCycle == "Monthly")
            .Sum(s => s.Amount);

        var biweeklyTotal = _allSubscriptions
            .Where(s => s.BillingCycle == "Biweekly")
            .Sum(s => s.Amount);

        MonthlyCostLabel.Text = $"${monthlyTotal:F2}";
        BiweeklyCostLabel.Text = $"${biweeklyTotal:F2}";
        SubscriptionCountLabel.Text = $"{_allSubscriptions.Count} active";
    }

    private async void OnAddSubscriptionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("add");
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            _subscriptions.Clear();
            foreach (var sub in _allSubscriptions)
                _subscriptions.Add(sub);
        }
        else
        {
            var filtered = _allSubscriptions
                .Where(s => s.ServiceName.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _subscriptions.Clear();
            foreach (var sub in filtered)
                _subscriptions.Add(sub);
        }
    }

    private void OnBillingCycleFilterChanged(object sender, EventArgs e)
    {
        string selectedCycle = BillingCyclePicker.SelectedItem as string;

        if (string.IsNullOrEmpty(selectedCycle) || selectedCycle == "All")
        {
            _subscriptions.Clear();
            foreach (var sub in _allSubscriptions)
                _subscriptions.Add(sub);
        }
        else
        {
            var filtered = _allSubscriptions
                .Where(s => s.BillingCycle == selectedCycle)
                .ToList();

            _subscriptions.Clear();
            foreach (var sub in filtered)
                _subscriptions.Add(sub);
        }
    }

    private async void OnSubscriptionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Subscription selected)
            return;

        var action = await DisplayActionSheet(
            "Subscription Options",
            "Cancel",
            "Delete",
            "Edit"
        );

        if (action == "Delete")
        {
            bool confirm = await DisplayAlert(
                "Confirm Delete",
                $"Delete {selected.ServiceName}?",
                "Yes",
                "No"
            );

            if (confirm && selected.Id.HasValue)
            {
                await _syncService.DeleteSubscriptionAsync(selected.Id.Value);
                await LoadSubscriptions();
            }
        }
        else if (action == "Edit")
        {
            // For now, delete and re-add
            // In future, can create EditSubscriptionPage
            await Shell.Current.GoToAsync("add");
        }

        SubscriptionsCollection.SelectedItem = null;
    }

    private void OnConnectivityChanged(object sender, bool isOnline)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectivityStatusLabel.Text = isOnline ? "Online" : "Offline";
            ConnectivityStatusLabel.TextColor = isOnline ? Colors.Green : Colors.Orange;
        });
    }
}
