namespace MemoryWeave.Client.Services;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MemoryWeave.Client.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Base HTTP client service for communicating with the Python backend API
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly string _baseUrl;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger, string baseUrl = "http://localhost:8000")
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = baseUrl;
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Make a GET request to the API
    /// </summary>
    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            _logger.LogInformation($"GET {endpoint}");
            var response = await _httpClient.GetAsync(endpoint);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"GET request failed: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    /// <summary>
    /// Make a POST request to the API
    /// </summary>
    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            _logger.LogInformation($"POST {endpoint}");
            var content = data != null ? JsonContent.Create(data) : null;
            var response = await _httpClient.PostAsync(endpoint, content);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"POST request failed: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    /// <summary>
    /// Make a PATCH request to the API
    /// </summary>
    public async Task<ApiResponse<T>> PatchAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            _logger.LogInformation($"PATCH {endpoint}");
            var content = data != null ? JsonContent.Create(data) : null;
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint) { Content = content };
            var response = await _httpClient.SendAsync(request);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"PATCH request failed: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    /// <summary>
    /// Make a DELETE request to the API
    /// </summary>
    public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
    {
        try
        {
            _logger.LogInformation($"DELETE {endpoint}");
            var response = await _httpClient.DeleteAsync(endpoint);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"DELETE request failed: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 0
            };
        }
    }

    /// <summary>
    /// Handle HTTP response and deserialize JSON
    /// </summary>
    private async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        try
        {
            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    return new ApiResponse<T>
                    {
                        Success = true,
                        StatusCode = statusCode
                    };
                }

                var data = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new ApiResponse<T>
                {
                    Success = true,
                    Data = data,
                    StatusCode = statusCode
                };
            }
            else
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new ApiResponse<T>
                {
                    Success = false,
                    Error = error?.Detail ?? $"HTTP {statusCode}",
                    StatusCode = statusCode
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError($"JSON deserialization failed: {ex.Message}");
            return new ApiResponse<T>
            {
                Success = false,
                Error = $"Invalid response format: {ex.Message}",
                StatusCode = statusCode
            };
        }
    }
}
