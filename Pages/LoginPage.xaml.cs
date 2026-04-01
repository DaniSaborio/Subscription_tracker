using Subscription_tracker.Services;

namespace Subscription_tracker.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly TokenService _tokenService;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = ServiceHelper.GetService<ApiService>();
        _tokenService = ServiceHelper.GetService<TokenService>();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ErrorLabel.Text = "Email and password are required";
            ErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;

            var response = await _apiService.LoginAsync(email, password);
            _apiService.SetAuthToken(response.Token);
            _tokenService.SaveToken(response);

            // Navigate to MainPage
            Shell.Current.GoToAsync("///home");
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Login failed: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnRegisterTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("register");
    }
}
