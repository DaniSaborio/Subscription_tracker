using Subscription_tracker.Models;
using Subscription_tracker.Services;

namespace Subscription_tracker.Pages;

public partial class AddSubscriptionPage : ContentPage
{
    private readonly SyncService _syncService;

    public AddSubscriptionPage()
    {
        InitializeComponent();
        _syncService = ServiceHelper.GetService<SyncService>();
        NextBillingDatePicker.Date = DateTime.Now.AddDays(1);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ServiceNameEntry.Text))
        {
            ErrorLabel.Text = "Service name is required";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (CategoryPicker.SelectedIndex < 0)
        {
            ErrorLabel.Text = "Please select a category";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!decimal.TryParse(AmountEntry.Text, out var amount))
        {
            ErrorLabel.Text = "Please enter a valid amount";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (BillingCyclePicker.SelectedIndex < 0)
        {
            ErrorLabel.Text = "Please select a billing cycle";
            ErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            var subscription = new Subscription
            {
                ServiceName = ServiceNameEntry.Text.Trim(),
                Category = CategoryPicker.Items[CategoryPicker.SelectedIndex],
                Amount = amount,
                BillingCycle = BillingCyclePicker.Items[BillingCyclePicker.SelectedIndex],
                NextBillingDate = NextBillingDatePicker.Date ?? DateTime.Now,
                PaymentMethod = PaymentMethodEntry.Text?.Trim(),
                Notes = NotesEditor.Text?.Trim(),
                IsActive = true
            };

            await _syncService.SaveSubscriptionAsync(subscription);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Error saving subscription: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
