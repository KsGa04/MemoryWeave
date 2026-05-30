namespace MemoryWeave.Client.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemoryWeave.Client.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for chat interactions with the AI assistant
/// </summary>
public class ChatService
{
    private readonly ApiService _apiService;
    private readonly ILogger<ChatService> _logger;
    private readonly List<ChatMessageModel> _conversationHistory;

    public ChatService(ApiService apiService, ILogger<ChatService> logger)
    {
        _apiService = apiService;
        _logger = logger;
        _conversationHistory = new List<ChatMessageModel>();
    }

    /// <summary>
    /// Send a query to the AI assistant for a specific personality
    /// </summary>
    public async Task<ApiResponse<ChatQueryResponse>> QueryAsync(int personalityId, string query, int topK = 5, string? context = null)
    {
        try
        {
            _logger.LogInformation($"Querying AI assistant for personality {personalityId}: {query}");

            var request = new ChatQueryRequest
            {
                Query = query,
                PersonalityId = personalityId,
                Context = context,
                TopK = topK
            };

            var response = await _apiService.PostAsync<ChatQueryResponse>(
                $"/api/personalities/{personalityId}/chat",
                request
            );

            // Store in conversation history if successful
            if (response.Success && response.Data != null)
            {
                _conversationHistory.Add(new ChatMessageModel
                {
                    PersonalityId = personalityId,
                    Text = query,
                    IsUser = true,
                    Timestamp = DateTime.UtcNow
                });

                _conversationHistory.Add(new ChatMessageModel
                {
                    PersonalityId = personalityId,
                    Text = response.Data.Response,
                    IsUser = false,
                    CitedSourceIds = string.Join(",", response.Data.Sources ?? Array.Empty<string>()),
                    Timestamp = DateTime.UtcNow
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error querying AI: {ex.Message}");
            return new ApiResponse<ChatQueryResponse>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Get conversation history
    /// </summary>
    public IReadOnlyList<ChatMessageModel> GetConversationHistory() => _conversationHistory.AsReadOnly();

    /// <summary>
    /// Clear conversation history
    /// </summary>
    public void ClearHistory()
    {
        _conversationHistory.Clear();
        _logger.LogInformation("Conversation history cleared");
    }

    /// <summary>
    /// Get conversation history for a specific personality
    /// </summary>
    public IReadOnlyList<ChatMessageModel> GetPersonalityHistory(int personalityId)
    {
        return _conversationHistory.FindAll(m => m.PersonalityId == personalityId).AsReadOnly();
    }
}
