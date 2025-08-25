using MauiFlow.Models;
using MauiFlow.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MauiFlow.ViewModels
{
    public class SettingsViewModel : BindableObject
    {
        LLMConfiguration _configuration;
        AppConfiguration _appConfiguration;

        readonly SettingsService _settingsService;
        readonly AzureOpenAIService _azureOpenAIService;
        readonly AlertService _alertService;
        readonly ThemeService _themeService;

        string _apiKey;
        string _endpoint;
        string _deploymentName;
        AppTheme _selectedTheme;
        string _defaultProjectPath;

        public SettingsViewModel(
            SettingsService settingsService, 
            AzureOpenAIService azureOpenAIService,
            AlertService alertService,
            ThemeService themeService)
        {
            _settingsService = settingsService;
            _azureOpenAIService = azureOpenAIService;
            _alertService = alertService;
            _themeService = themeService;

            _configuration = new LLMConfiguration();
            _appConfiguration = new AppConfiguration();

            // Initialize theme options
            ThemeOptions = new ObservableCollection<ThemeOption>
            {
                new ThemeOption { Name = "System", Value = AppTheme.System },
                new ThemeOption { Name = "Light", Value = AppTheme.Light },
                new ThemeOption { Name = "Dark", Value = AppTheme.Dark }
            };

            // Initialize commands
            LoadSettingsCommand = new Command(async () => await LoadSettingsAsync());
            SaveSettingsCommand = new Command(async () => await SaveSettingsAsync());
            TestConnectionCommand = new Command(async () => await TestConnectionAsync());
            SaveAppSettingsCommand = new Command(async () => await SaveAppSettingsAsync());
            BrowseProjectPathCommand = new Command(async () => await BrowseProjectPathAsync());

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
        /// Gets or sets the selected theme for the application.
        /// </summary>
        public AppTheme SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                _selectedTheme = value;
                OnPropertyChanged();
                // Apply theme immediately when changed
                _themeService.ApplyTheme(value);
            }
        }

        /// <summary>
        /// Gets or sets the default project path.
        /// </summary>
        public string DefaultProjectPath
        {
            get => _defaultProjectPath;
            set
            {
                _defaultProjectPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the collection of available theme options.
        /// </summary>
        public ObservableCollection<ThemeOption> ThemeOptions { get; }

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
        /// Command that saves the app settings.
        /// </summary>
        public ICommand SaveAppSettingsCommand { get; }

        /// <summary>
        /// Command that opens a folder browser for project path selection.
        /// </summary>
        public ICommand BrowseProjectPathCommand { get; }

        /// <summary>
        /// Loads settings from the <see cref="SettingsService"/> into the ViewModel properties.
        /// </summary>
        async Task LoadSettingsAsync()
        {
            _configuration = await _settingsService.LoadSettingsAsync();
            _appConfiguration = await _settingsService.LoadAppSettingsAsync();

            ApiKey = _configuration.ApiKey;
            Endpoint = _configuration.Endpoint;
            DeploymentName = _configuration.DeploymentName;
            
            SelectedTheme = _appConfiguration.Theme;
            DefaultProjectPath = _appConfiguration.DefaultProjectPath;
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
            await _alertService.ShowAlertAsync("Information", "Azure OpenAI settings saved successfully!");
        }

        /// <summary>
        /// Saves the app settings.
        /// </summary>
        async Task SaveAppSettingsAsync()
        {
            // Update app configuration object with latest values
            _appConfiguration.Theme = SelectedTheme;
            _appConfiguration.DefaultProjectPath = DefaultProjectPath;

            // Save app settings
            await _settingsService.SaveAppSettingsAsync(_appConfiguration);
            await _alertService.ShowAlertAsync("Information", "App settings saved successfully!");
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

        async Task BrowseProjectPathAsync()
        {
            try
            {
                // For now, use a prompt dialog. In a real app, you'd use a folder picker
                var result = await _alertService.ShowPromptAsync(
                    "Default Project Path", 
                    "Enter the default project path:", 
                    "OK", 
                    "Cancel", 
                    "C:\\Projects\\", 
                    -1, 
                    null, 
                    DefaultProjectPath);

                if (!string.IsNullOrEmpty(result))
                {
                    DefaultProjectPath = result;
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowAlertAsync("Error", $"Error selecting folder: {ex.Message}");
            }
        }
    }

    public class ThemeOption
    {
        public string Name { get; set; }
        public AppTheme Value { get; set; }
    }
}