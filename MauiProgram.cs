using Microsoft.Extensions.Logging;
using Subscription_tracker.Pages;
using Subscription_tracker.Services;

namespace Subscription_tracker;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Services
		builder.Services.AddSingleton<IPreferences>(Preferences.Default);
		builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);

		// API and Data Services
		builder.Services.AddSingleton<ApiService>();
		builder.Services.AddSingleton<LocalStorageService>();
		builder.Services.AddSingleton<TokenService>();
		builder.Services.AddSingleton<SyncService>();

		// Pages
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<LoginPage>();
		builder.Services.AddSingleton<RegisterPage>();
		builder.Services.AddSingleton<AddSubscriptionPage>();

		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Initialize services
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			var syncService = app.Services.GetRequiredService<SyncService>();
			await syncService.InitializeAsync();
		});

		return app;
	}
}

// Service helper for getting services from MainThread
public static class ServiceHelper
{
	public static T GetService<T>() where T : class
	{
		if (Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(T)) is T service)
		{
			return service;
		}
		throw new InvalidOperationException($"Service {typeof(T).Name} not found");
	}
}

