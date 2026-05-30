namespace MemoryWeave.Client.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Utility class for managing application settings
/// </summary>
public class AppSettings
{
    [JsonPropertyName("api_base_url")]
    public string ApiBaseUrl { get; set; } = "http://localhost:8000";

    [JsonPropertyName("telegram_phone")]
    public string? TelegramPhone { get; set; }

    [JsonPropertyName("obsidian_vault_path")]
    public string? ObsidianVaultPath { get; set; }

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Light";

    [JsonPropertyName("auto_sync")]
    public bool AutoSync { get; set; } = true;

    [JsonPropertyName("sync_interval_minutes")]
    public int SyncIntervalMinutes { get; set; } = 30;

    [JsonPropertyName("log_level")]
    public string LogLevel { get; set; } = "Information";

    [JsonPropertyName("last_active_personality_id")]
    public int? LastActivePersonalityId { get; set; }
}

/// <summary>
/// Settings manager for loading and saving application configuration
/// </summary>
public class SettingsManager
{
    private readonly string _settingsPath;
    private readonly ILogger<SettingsManager> _logger;
    private AppSettings _settings;

    public AppSettings Current => _settings;

    public SettingsManager(ILogger<SettingsManager> logger)
    {
        _logger = logger;
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MemoryWeave",
            "settings.json"
        );
        _settings = new AppSettings();
        Load();
    }

    /// <summary>
    /// Load settings from file
    /// </summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation("Settings file not found, using defaults");
                _settings = new AppSettings();
                Save();
                return;
            }

            var json = File.ReadAllText(_settingsPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, options);
            _settings = loaded ?? new AppSettings();
            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading settings: {ex.Message}");
            _settings = new AppSettings();
        }
    }

    /// <summary>
    /// Save settings to file
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsPath, json);
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Update a specific setting
    /// </summary>
    public void UpdateSetting(Action<AppSettings> updateAction)
    {
        updateAction(_settings);
        Save();
    }
}
