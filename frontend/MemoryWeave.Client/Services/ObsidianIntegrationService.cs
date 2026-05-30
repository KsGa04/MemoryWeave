namespace MemoryWeave.Client.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for integrating with Obsidian vault
/// </summary>
public class ObsidianIntegrationService
{
    private readonly ILogger<ObsidianIntegrationService> _logger;
    private string? _vaultPath;
    private FileSystemWatcher? _fileWatcher;

    public event EventHandler<FileSystemEventArgs>? FileChanged;

    public ObsidianIntegrationService(ILogger<ObsidianIntegrationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if vault is connected
    /// </summary>
    public bool IsConnected => !string.IsNullOrEmpty(_vaultPath) && Directory.Exists(_vaultPath);

    /// <summary>
    /// Connect to Obsidian vault
    /// </summary>
    public bool Connect(string vaultPath)
    {
        try
        {
            if (!Directory.Exists(vaultPath))
            {
                _logger.LogError($"Vault path does not exist: {vaultPath}");
                return false;
            }

            _vaultPath = vaultPath;
            _logger.LogInformation($"Connected to Obsidian vault: {vaultPath}");
            
            // Start file watcher
            StartWatching();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to connect to Obsidian vault: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disconnect from Obsidian vault
    /// </summary>
    public bool Disconnect()
    {
        try
        {
            _fileWatcher?.Dispose();
            _vaultPath = null;
            _logger.LogInformation("Disconnected from Obsidian vault");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to disconnect from Obsidian vault: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Read a markdown file from the vault
    /// </summary>
    public async Task<string?> ReadFileAsync(string relativeFilePath)
    {
        try
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Obsidian vault is not connected");
                return null;
            }

            var fullPath = Path.Combine(_vaultPath!, relativeFilePath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning($"File not found: {fullPath}");
                return null;
            }

            return await File.ReadAllTextAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to read file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Write a markdown file to the vault
    /// </summary>
    public async Task<bool> WriteFileAsync(string relativeFilePath, string content)
    {
        try
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Obsidian vault is not connected");
                return false;
            }

            var fullPath = Path.Combine(_vaultPath!, relativeFilePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content);
            _logger.LogInformation($"Written file: {relativeFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to write file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Start watching for file changes
    /// </summary>
    private void StartWatching()
    {
        if (string.IsNullOrEmpty(_vaultPath) || !Directory.Exists(_vaultPath))
            return;

        _fileWatcher = new FileSystemWatcher(_vaultPath)
        {
            Filter = "*.md",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        _fileWatcher.Changed += (s, e) => FileChanged?.Invoke(this, e);
        _fileWatcher.Created += (s, e) => FileChanged?.Invoke(this, e);
        _fileWatcher.EnableRaisingEvents = true;

        _logger.LogInformation("Started watching Obsidian vault for changes");
    }
}
