namespace MemoryWeave.Client.Utils;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Utility class for application configuration and initialization
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Get backend API URL from environment or use default
    /// </summary>
    public static string GetApiBaseUrl()
    {
        var apiUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        return apiUrl ?? "http://localhost:8000";
    }

    /// <summary>
    /// Check if backend API is accessible
    /// </summary>
    public static async Task<bool> CheckApiHealthAsync(string apiBaseUrl, ILogger logger)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync($"{apiBaseUrl}/api/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning($"API health check failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Format file size for display
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Format time span for display
    /// </summary>
    public static string FormatTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalSeconds < 60)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return dateTime.ToString("yyyy-MM-dd");
    }
}
