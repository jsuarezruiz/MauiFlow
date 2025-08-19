using MauiFlow.Models;

namespace MauiFlow.Services
{
    public class SettingsService
    {
        const string ApiKey = "AzureOpenAIApiKey";
        const string EndpointKey = "AzureOpenAIEndpoint";
        const string DeploymentNameKey = "AzureOpenAIDeploymentName";

        // Save settings
        public async Task SaveSettingsAsync(LLMConfiguration config)
        {
            await SecureStorage.SetAsync(ApiKey, config.ApiKey ?? string.Empty);
            await SecureStorage.SetAsync(EndpointKey, config.Endpoint ?? string.Empty);
            await SecureStorage.SetAsync(DeploymentNameKey, config.DeploymentName ?? string.Empty);
        }

        // Load settings
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
    }
}
