namespace MemoryWeave.Client.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemoryWeave.Client.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing personalities (profiles) via the backend API
/// </summary>
public class PersonalityService
{
    private readonly ApiService _apiService;
    private readonly ILogger<PersonalityService> _logger;

    public PersonalityService(ApiService apiService, ILogger<PersonalityService> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    /// <summary>
    /// Get all personalities
    /// </summary>
    public async Task<ApiResponse<List<PersonalityModel>>> GetAllPersonalitiesAsync(int skip = 0, int limit = 10, bool? isActive = null)
    {
        try
        {
            var endpoint = $"/api/personalities?skip={skip}&limit={limit}";
            if (isActive.HasValue)
            {
                endpoint += $"&is_active={isActive.Value.ToString().ToLower()}";
            }

            return await _apiService.GetAsync<List<PersonalityModel>>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting personalities: {ex.Message}");
            return new ApiResponse<List<PersonalityModel>>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Get a specific personality by ID
    /// </summary>
    public async Task<ApiResponse<PersonalityModel>> GetPersonalityAsync(int personalityId)
    {
        try
        {
            return await _apiService.GetAsync<PersonalityModel>($"/api/personalities/{personalityId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting personality {personalityId}: {ex.Message}");
            return new ApiResponse<PersonalityModel>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Get detailed personality information with statistics
    /// </summary>
    public async Task<ApiResponse<PersonalityDetailedModel>> GetPersonalityDetailedAsync(int personalityId)
    {
        try
        {
            return await _apiService.GetAsync<PersonalityDetailedModel>($"/api/personalities/{personalityId}/detailed");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting detailed personality {personalityId}: {ex.Message}");
            return new ApiResponse<PersonalityDetailedModel>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Create a new personality
    /// </summary>
    public async Task<ApiResponse<PersonalityModel>> CreatePersonalityAsync(CreatePersonalityRequest request)
    {
        try
        {
            _logger.LogInformation($"Creating personality: {request.Name}");
            return await _apiService.PostAsync<PersonalityModel>("/api/personalities", request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating personality: {ex.Message}");
            return new ApiResponse<PersonalityModel>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Update an existing personality
    /// </summary>
    public async Task<ApiResponse<PersonalityModel>> UpdatePersonalityAsync(int personalityId, UpdatePersonalityRequest request)
    {
        try
        {
            _logger.LogInformation($"Updating personality: {personalityId}");
            return await _apiService.PatchAsync<PersonalityModel>($"/api/personalities/{personalityId}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating personality {personalityId}: {ex.Message}");
            return new ApiResponse<PersonalityModel>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Delete a personality
    /// </summary>
    public async Task<ApiResponse<object>> DeletePersonalityAsync(int personalityId)
    {
        try
        {
            _logger.LogInformation($"Deleting personality: {personalityId}");
            return await _apiService.DeleteAsync<object>($"/api/personalities/{personalityId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting personality {personalityId}: {ex.Message}");
            return new ApiResponse<object>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Get statistics for a personality
    /// </summary>
    public async Task<ApiResponse<Dictionary<string, object>>> GetPersonalityStatsAsync(int personalityId)
    {
        try
        {
            return await _apiService.GetAsync<Dictionary<string, object>>($"/api/personalities/{personalityId}/stats");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting personality stats: {ex.Message}");
            return new ApiResponse<Dictionary<string, object>>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
