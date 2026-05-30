namespace MemoryWeave.Client.ViewModels;

using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using MemoryWeave.Client.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for settings and configuration
/// </summary>
public class SettingsViewModel : ReactiveObject
{
    private readonly TelegramIntegrationService _telegramService;
    private readonly ObsidianIntegrationService _obsidianService;
    private readonly ILogger<SettingsViewModel> _logger;

    private bool _telegramConnected = false;
    private bool _obsidianConnected = false;
    private string _telegramPhone = string.Empty;
    private string _obsidianVaultPath = string.Empty;
    private string? _statusMessage;
    private bool _isSyncing = false;

    public ReactiveCommand<Unit, Unit> ConnectTelegramCommand { get; }
    public ReactiveCommand<Unit, Unit> DisconnectTelegramCommand { get; }
    public ReactiveCommand<Unit, Unit> ConnectObsidianCommand { get; }
    public ReactiveCommand<Unit, Unit> DisconnectObsidianCommand { get; }
    public ReactiveCommand<Unit, Unit> SyncDataCommand { get; }

    public bool TelegramConnected
    {
        get => _telegramConnected;
        set => this.RaiseAndSetIfChanged(ref _telegramConnected, value);
    }

    public bool ObsidianConnected
    {
        get => _obsidianConnected;
        set => this.RaiseAndSetIfChanged(ref _obsidianConnected, value);
    }

    public string TelegramPhone
    {
        get => _telegramPhone;
        set => this.RaiseAndSetIfChanged(ref _telegramPhone, value);
    }

    public string ObsidianVaultPath
    {
        get => _obsidianVaultPath;
        set => this.RaiseAndSetIfChanged(ref _obsidianVaultPath, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsSyncing
    {
        get => _isSyncing;
        set => this.RaiseAndSetIfChanged(ref _isSyncing, value);
    }

    public SettingsViewModel(TelegramIntegrationService telegramService, ObsidianIntegrationService obsidianService, ILogger<SettingsViewModel> logger)
    {
        _telegramService = telegramService;
        _obsidianService = obsidianService;
        _logger = logger;

        ConnectTelegramCommand = ReactiveCommand.CreateFromTask(ConnectTelegram);
        DisconnectTelegramCommand = ReactiveCommand.CreateFromTask(DisconnectTelegram);
        ConnectObsidianCommand = ReactiveCommand.CreateFromTask(ConnectObsidian);
        DisconnectObsidianCommand = ReactiveCommand.CreateFromTask(DisconnectObsidian);
        SyncDataCommand = ReactiveCommand.CreateFromTask(SyncData);
    }

    private async Task ConnectTelegram()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TelegramPhone))
            {
                StatusMessage = "Please enter Telegram phone number";
                return;
            }

            IsSyncing = true;
            StatusMessage = "Connecting to Telegram...";
            _logger.LogInformation($"Connecting Telegram: {TelegramPhone}");

            var result = await _telegramService.ConnectAsync(TelegramPhone);
            TelegramConnected = result;
            StatusMessage = result ? "Telegram connected successfully" : "Failed to connect Telegram";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError($"Telegram connection error: {ex.Message}");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task DisconnectTelegram()
    {
        try
        {
            IsSyncing = true;
            StatusMessage = "Disconnecting from Telegram...";
            var result = await _telegramService.DisconnectAsync();
            TelegramConnected = !result;
            StatusMessage = result ? "Disconnected from Telegram" : "Failed to disconnect";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError($"Disconnect error: {ex.Message}");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private async Task ConnectObsidian()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ObsidianVaultPath))
            {
                StatusMessage = "Please enter Obsidian vault path";
                return;
            }

            StatusMessage = "Connecting to Obsidian...";
            _logger.LogInformation($"Connecting Obsidian: {ObsidianVaultPath}");

            var result = _obsidianService.Connect(ObsidianVaultPath);
            ObsidianConnected = result;
            StatusMessage = result ? "Obsidian connected successfully" : "Failed to connect Obsidian";
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError($"Obsidian connection error: {ex.Message}");
        }
    }

    private async Task DisconnectObsidian()
    {
        try
        {
            StatusMessage = "Disconnecting from Obsidian...";
            var result = _obsidianService.Disconnect();
            ObsidianConnected = !result;
            StatusMessage = result ? "Disconnected from Obsidian" : "Failed to disconnect";
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError($"Disconnect error: {ex.Message}");
        }
    }

    private async Task SyncData()
    {
        try
        {
            IsSyncing = true;
            StatusMessage = "Syncing data...";
            _logger.LogInformation("Starting data synchronization");

            if (TelegramConnected)
            {
                StatusMessage = "Syncing Telegram messages...";
                _logger.LogInformation("Syncing Telegram");
                // TODO: Sync Telegram messages
            }

            if (ObsidianConnected)
            {
                StatusMessage = "Syncing Obsidian notes...";
                _logger.LogInformation("Syncing Obsidian");
                // TODO: Sync Obsidian notes
            }

            StatusMessage = "Synchronization completed";
            _logger.LogInformation("Data synchronization completed");
            await Task.Delay(2000); // Show message for 2 seconds
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync error: {ex.Message}";
            _logger.LogError($"Sync error: {ex.Message}");
        }
        finally
        {
            IsSyncing = false;
        }
    }
}
