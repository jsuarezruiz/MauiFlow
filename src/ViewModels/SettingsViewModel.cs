using MauiFlow.Models;
using MauiFlow.Services;
using System.Windows.Input;

namespace MauiFlow.ViewModels
{
    public class SettingsViewModel : BindableObject
    {
        LLMConfiguration _configuration;

        readonly SettingsService _settingsService;
        readonly AzureOpenAIService _azureOpenAIService;
        readonly AlertService _alertService;

        string _apiKey;
        string _endpoint;
        string _deploymentName;

        public SettingsViewModel(
            SettingsService settingsService, 
            AzureOpenAIService azureOpenAIService,
            AlertService alertService)
        {
            _settingsService = settingsService;
            _azureOpenAIService = azureOpenAIService;
            _alertService = alertService;

            _configuration = new LLMConfiguration();

            // Initialize commands
            LoadSettingsCommand = new Command(async () => await LoadSettingsAsync());
            SaveSettingsCommand = new Command(async () => await SaveSettingsAsync());
            TestConnectionCommand = new Command(async () => await TestConnectionAsync());

            // Automatically load settings in the background when ViewModel is created
            _ = Task.Run(LoadSettingsAsync);
        }

        /// <summary>
        /// Gets or sets the API key used to authenticate with Azure OpenAI.
        /// </summary>
        public string ApiKey
        {
            get => _apiKey;
            set
            {
                _apiKey = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the endpoint URL for the Azure OpenAI service.
        /// </summary>
        public string Endpoint
        {
            get => _endpoint;
            set
            {
                _endpoint = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the deployment name for the Azure OpenAI model.
        /// </summary>
        public string DeploymentName
        {
            get => _deploymentName;
            set
            {
                _deploymentName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command that loads saved settings from local storage.
        /// </summary>
        public ICommand LoadSettingsCommand { get; }

        /// <summary>
        /// Command that saves the current settings to local storage.
        /// </summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>
        /// Command that tests the connection to Azure OpenAI using the provided settings.
        /// </summary>
        public ICommand TestConnectionCommand { get; }

        /// <summary>
        /// Loads settings from the <see cref="SettingsService"/> into the ViewModel properties.
        /// </summary>
        async Task LoadSettingsAsync()
        {
            _configuration = await _settingsService.LoadSettingsAsync();

            ApiKey = _configuration.ApiKey;
            Endpoint = _configuration.Endpoint;
            DeploymentName = _configuration.DeploymentName;
        }

        /// <summary>
        /// Saves the current settings into persistent storage via the <see cref="SettingsService"/>.
        /// Validates that required fields are not empty.
        /// </summary>
        async Task SaveSettingsAsync()
        {
            // Validate input fields before saving
            if (string.IsNullOrWhiteSpace(ApiKey) || string.IsNullOrWhiteSpace(Endpoint) || string.IsNullOrWhiteSpace(DeploymentName))
            {
                await _alertService.ShowAlertAsync("Error", "API Key, Endpoint and DeploymentName must be set.");
                return;
            }

            // Update configuration object with latest values
            _configuration.ApiKey = ApiKey;
            _configuration.Endpoint = Endpoint;
            _configuration.DeploymentName = DeploymentName;

            // Save settings
            await _settingsService.SaveSettingsAsync(_configuration);
        }

        async Task TestConnectionAsync()
        {
            try
            {
                bool isConnected = await _azureOpenAIService.TestConnectionAsync(ApiKey, Endpoint, DeploymentName);

                if (isConnected)
                {
                    await _alertService.ShowAlertAsync("Information", "Connection successful!");
                }
                else
                {
                    await _alertService.ShowAlertAsync("Error", "Connection failed");
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowAlertAsync("Error", $"Connection test error: {ex.Message}");
            }
        }
    }
}