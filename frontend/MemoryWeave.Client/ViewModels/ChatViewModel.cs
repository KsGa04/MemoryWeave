namespace MemoryWeave.Client.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using MemoryWeave.Client.Models;
using MemoryWeave.Client.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for chat interface with AI assistant
/// </summary>
public class ChatViewModel : ReactiveObject
{
    private readonly ChatService _chatService;
    private readonly PersonalityService _personalityService;
    private readonly ILogger<ChatViewModel> _logger;

    private PersonalityModel? _activePersonality;
    private string _currentMessage = string.Empty;
    private bool _isLoading = false;
    private string? _errorMessage;

    public ObservableCollection<ChatMessageModel> Messages { get; }
    public ObservableCollection<PersonalityModel> Personalities { get; }
    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearHistoryCommand { get; }

    public PersonalityModel? ActivePersonality
    {
        get => _activePersonality;
        set => this.RaiseAndSetIfChanged(ref _activePersonality, value);
    }

    public string CurrentMessage
    {
        get => _currentMessage;
        set => this.RaiseAndSetIfChanged(ref _currentMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public ChatViewModel(ChatService chatService, PersonalityService personalityService, ILogger<ChatViewModel> logger)
    {
        _chatService = chatService;
        _personalityService = personalityService;
        _logger = logger;
        Messages = new ObservableCollection<ChatMessageModel>();
        Personalities = new ObservableCollection<PersonalityModel>();

        SendMessageCommand = ReactiveCommand.CreateFromTask(SendMessage, this.WhenAnyValue(x => x.CurrentMessage, m => !string.IsNullOrWhiteSpace(m) && _activePersonality != null));
        ClearHistoryCommand = ReactiveCommand.Create(ClearHistory);
    }

    public async Task LoadPersonalities()
    {
        try
        {
            var response = await _personalityService.GetAllPersonalitiesAsync();
            if (response.Success && response.Data != null)
            {
                Personalities.Clear();
                foreach (var personality in response.Data)
                {
                    Personalities.Add(personality);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading personalities: {ex.Message}");
        }
    }

    private async Task SendMessage()
    {
        if (ActivePersonality == null || string.IsNullOrWhiteSpace(CurrentMessage))
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Add user message to chat
            var userMessage = new ChatMessageModel
            {
                PersonalityId = ActivePersonality.Id,
                Text = CurrentMessage,
                IsUser = true,
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(userMessage);

            var query = CurrentMessage;
            CurrentMessage = string.Empty;

            // Send to AI
            var response = await _chatService.QueryAsync(ActivePersonality.Id, query);

            if (response.Success && response.Data != null)
            {
                // Add AI response to chat
                var aiMessage = new ChatMessageModel
                {
                    PersonalityId = ActivePersonality.Id,
                    Text = response.Data.Response,
                    IsUser = false,
                    CitedSourceIds = string.Join(",", response.Data.Sources ?? Array.Empty<string>()),
                    Timestamp = DateTime.UtcNow
                };
                Messages.Add(aiMessage);
                _logger.LogInformation($"Received response from AI (confidence: {response.Data.Confidence})");
            }
            else
            {
                ErrorMessage = response.Error ?? "Failed to get response from AI";
                _logger.LogError(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError($"Error sending message: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearHistory()
    {
        Messages.Clear();
        _chatService.ClearHistory();
        _logger.LogInformation("Conversation history cleared");
    }
}
