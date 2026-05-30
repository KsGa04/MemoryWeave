namespace MemoryWeave.Client;

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using MemoryWeave.Client.Services;
using MemoryWeave.Client.ViewModels;
using MemoryWeave.Client.Utils;

/// <summary>
/// Main application class for Avalonia
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<App> _logger;

    public App()
    {
        _serviceProvider = ConfigureServices();
        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<App>();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _logger.LogInformation("Initializing MemoryWeave Client");

        if (ApplicationLifetime is IClassicDesktopApplicationLifetime desktop)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
        _logger.LogInformation("Application initialized successfully");
    }

    /// <summary>
    /// Configure dependency injection services
    /// </summary>
    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        var loggerFactory = LoggingHelper.CreateLoggerFactory();
        services.AddSingleton(loggerFactory);
        services.AddLogging(builder => builder.AddSerilog());

        // Settings
        services.AddSingleton<SettingsManager>();
        services.AddSingleton<AppSettings>(sp => sp.GetRequiredService<SettingsManager>().Current);

        // API and HTTP
        var apiBaseUrl = ConfigurationHelper.GetApiBaseUrl();
        services.AddHttpClient<ApiService>((client, sp) =>
        {
            var logger = sp.GetRequiredService<ILogger<ApiService>>();
            return new ApiService(client, logger, apiBaseUrl);
        });

        // Services
        services.AddSingleton<PersonalityService>();
        services.AddSingleton<ChatService>();
        services.AddSingleton<TelegramIntegrationService>();
        services.AddSingleton<ObsidianIntegrationService>();

        // ViewModels
        services.AddSingleton<PersonalitiesViewModel>();
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Views
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}

/// <summary>
/// Main window for the application
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = Application.Current?.Resources["MainViewModel"];
    }
}
