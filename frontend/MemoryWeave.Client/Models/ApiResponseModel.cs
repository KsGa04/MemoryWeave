namespace MemoryWeave.Client.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Generic API response wrapper for error handling
/// </summary>
public class ApiResponse<T>
{
    /// <summary>Whether the request was successful</summary>
    public bool Success { get; set; } = true;

    /// <summary>The response data</summary>
    public T? Data { get; set; }

    /// <summary>Error message if request failed</summary>
    public string? Error { get; set; }

    /// <summary>HTTP status code</summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>Timestamp of the response</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Error response model matching Python backend format
/// </summary>
public class ErrorResponse
{
    /// <summary>Error message</summary>
    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// Pagination wrapper for list responses
/// </summary>
public class PaginatedResponse<T>
{
    /// <summary>Total number of items</summary>
    public int Total { get; set; }

    /// <summary>Number of items skipped</summary>
    public int Skip { get; set; }

    /// <summary>Number of items returned</summary>
    public int Limit { get; set; }

    /// <summary>The items in this page</summary>
    public List<T> Items { get; set; } = new();
}

/// <summary>
/// Statistics response
/// </summary>
public class StatsResponse
{
    /// <summary>System status</summary>
    public string Status { get; set; } = "healthy";

    /// <summary>Status message</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Additional data</summary>
    public Dictionary<string, object>? Data { get; set; }
}
