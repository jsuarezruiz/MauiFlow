using MauiFlow.Models;

namespace MauiFlow.Services
{
    public class SettingsService
    {
        const string ApiKey = "AzureOpenAIApiKey";
        const string EndpointKey = "AzureOpenAIEndpoint";
        const string DeploymentNameKey = "AzureOpenAIDeploymentName";
        const string AppThemeKey = "AppTheme";
        const string DefaultProjectPathKey = "DefaultProjectPath";

        // Save LLM settings
        public async Task SaveSettingsAsync(LLMConfiguration config)
        {
            await SecureStorage.SetAsync(ApiKey, config.ApiKey ?? string.Empty);
            await SecureStorage.SetAsync(EndpointKey, config.Endpoint ?? string.Empty);
            await SecureStorage.SetAsync(DeploymentNameKey, config.DeploymentName ?? string.Empty);
        }

        // Load LLM settings
        public async Task<LLMConfiguration> LoadSettingsAsync()
        {
            var config = new LLMConfiguration
            {
                ApiKey = await SecureStorage.GetAsync(ApiKey) ?? string.Empty,
                Endpoint = await SecureStorage.GetAsync(EndpointKey) ?? string.Empty,
                DeploymentName = await SecureStorage.GetAsync(DeploymentNameKey) ?? string.Empty
            };

            return config;
        }

        // Save app settings
        public async Task SaveAppSettingsAsync(AppConfiguration config)
        {
            await SecureStorage.SetAsync(AppThemeKey, config.Theme.ToString());
            await SecureStorage.SetAsync(DefaultProjectPathKey, config.DefaultProjectPath ?? string.Empty);
        }

        // Load app settings
        public async Task<AppConfiguration> LoadAppSettingsAsync()
        {
            var themeString = await SecureStorage.GetAsync(AppThemeKey) ?? AppTheme.System.ToString();
            Enum.TryParse<AppTheme>(themeString, out var theme);

            var config = new AppConfiguration
            {
                Theme = theme,
                DefaultProjectPath = await SecureStorage.GetAsync(DefaultProjectPathKey) ?? string.Empty
            };

            return config;
        }
    }
}
