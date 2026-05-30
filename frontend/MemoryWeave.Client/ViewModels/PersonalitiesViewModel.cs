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
/// ViewModel for managing personalities/profiles
/// </summary>
public class PersonalitiesViewModel : ReactiveObject
{
    private readonly PersonalityService _personalityService;
    private readonly ILogger<PersonalitiesViewModel> _logger;

    private PersonalityModel? _selectedPersonality;
    private bool _isLoading = false;
    private string? _errorMessage;
    private string _newPersonalityName = string.Empty;

    public ObservableCollection<PersonalityModel> Personalities { get; }
    public ReactiveCommand<Unit, Unit> LoadPersonalitiesCommand { get; }
    public ReactiveCommand<Unit, Unit> CreatePersonalityCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    public PersonalityModel? SelectedPersonality
    {
        get => _selectedPersonality;
        set => this.RaiseAndSetIfChanged(ref _selectedPersonality, value);
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

    public string NewPersonalityName
    {
        get => _newPersonalityName;
        set => this.RaiseAndSetIfChanged(ref _newPersonalityName, value);
    }

    public PersonalitiesViewModel(PersonalityService personalityService, ILogger<PersonalitiesViewModel> logger)
    {
        _personalityService = personalityService;
        _logger = logger;
        Personalities = new ObservableCollection<PersonalityModel>();

        LoadPersonalitiesCommand = ReactiveCommand.CreateFromTask(LoadPersonalities);
        CreatePersonalityCommand = ReactiveCommand.CreateFromTask(CreatePersonality);
        RefreshCommand = ReactiveCommand.CreateFromTask(async () => await LoadPersonalities());
    }

    private async Task LoadPersonalities()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var response = await _personalityService.GetAllPersonalitiesAsync();

            if (response.Success && response.Data != null)
            {
                Personalities.Clear();
                foreach (var personality in response.Data)
                {
                    Personalities.Add(personality);
                }
                _logger.LogInformation($"Loaded {Personalities.Count} personalities");
            }
            else
            {
                ErrorMessage = response.Error ?? "Failed to load personalities";
                _logger.LogError(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError($"Error loading personalities: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreatePersonality()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewPersonalityName))
            {
                ErrorMessage = "Please enter a personality name";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            var request = new CreatePersonalityRequest
            {
                Name = NewPersonalityName,
                IsActive = true
            };

            var response = await _personalityService.CreatePersonalityAsync(request);

            if (response.Success && response.Data != null)
            {
                Personalities.Add(response.Data);
                NewPersonalityName = string.Empty;
                _logger.LogInformation($"Created personality: {response.Data.Name}");
            }
            else
            {
                ErrorMessage = response.Error ?? "Failed to create personality";
                _logger.LogError(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError($"Error creating personality: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
