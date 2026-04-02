using Subscription_tracker.Services;

namespace Subscription_tracker.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly TokenService _tokenService;

    public RegisterPage()
    {
        InitializeComponent();
        _apiService = ServiceHelper.GetService<ApiService>();
        _tokenService = ServiceHelper.GetService<TokenService>();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ErrorLabel.Text = "All fields are required";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (password != confirmPassword)
        {
            ErrorLabel.Text = "Passwords do not match";
            ErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;

            var response = await _apiService.RegisterAsync(email, password, confirmPassword);
            _apiService.SetAuthToken(response.Token);
            _tokenService.SaveToken(response);

            // Navigate to MainPage
            Shell.Current.GoToAsync("///home");
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Registration failed: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnLoginTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("///login");
    }
}
