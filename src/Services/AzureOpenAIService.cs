using Azure;
using Azure.AI.OpenAI;

namespace MauiFlow.Services
{
    public class AzureOpenAIService
    {
        private readonly SettingsService _settingsService;

        public AzureOpenAIService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// Sends a prompt to the configured Azure OpenAI deployment and retrieves the response.
        /// </summary>
        /// <param name="prompt">The user prompt text to evaluate.</param>
        /// <returns>
        /// A tuple with:
        /// <list type="bullet">
        /// <item><description><c>Success</c>: Whether the request succeeded.</description></item>
        /// <item><description><c>Result</c>: The AI-generated text if successful.</description></item>
        /// <item><description><c>ErrorMessage</c>: An error description if failed.</description></item>
        /// </list>
        /// </returns>
        public async Task<(bool Success, string Result, string ErrorMessage)> EvaluatePromptAsync(string prompt)
        {
            try
            {
                var config = await _settingsService.LoadSettingsAsync();
                if (string.IsNullOrEmpty(config.ApiKey) ||
                    string.IsNullOrEmpty(config.Endpoint) ||
                    string.IsNullOrEmpty(config.DeploymentName))
                {
                    return (false, null, "Azure OpenAI settings are incomplete. Please configure in settings.");
                }

                // Use the Azure AI client with only Endpoint + API Key
                var client = new AzureOpenAIClient(new Uri(config.Endpoint), new AzureKeyCredential(config.ApiKey));

                var deploymentName = config.DeploymentName;

                ChatClient chatClient = client.GetChatClient(deploymentName);
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(@"You are a senior software engineer specialized in building .NET MAUI applications.
                    Your task is to create a complete .NET MAUI app based on the requirements provided. Follow these principles:
    
                    1. Use .NET MAUI Core and the .NET MAUI Community Toolkit freely to enhance functionality and UI.
                    2. Always create a MainPage as the entry point of the app.
                    3. Design the UI to be visually appealing, responsive, and user-friendly. 
                    4. Implement the full logic of the app, including data handling, event responses, navigation, and services.
                    5. Keep it simple. Avoid MVVM pattern. Implement the logic in the code-behind.
                    6. Avoid using DisplayAlert unless absolutely necessary.
                    7. Avoid placeholder code—write real implementations.
                    8. Include comments for non-obvious logic.
                    9. Ensure cross-platform compatibility.
                    10. Use best practices for performance, readability, and maintainability.
    
                    IMPORTANT CODE STRUCTURE REQUIREMENTS:
                    11. If you need model classes (like Task, FoodItem, etc.), define them INSIDE the MainPage.xaml.cs file within the same namespace as the MainPage class, NOT in separate files or namespaces.
                    12. Do NOT use custom namespaces like 'TodoApp.Models' or 'RestaurantApp.Models' - keep everything in the main namespace.
                    13. Place all model classes BEFORE the MainPage class definition in the same file.
                    14. **CONSTRUCTOR RESTRICTIONS: Do NOT call any methods that access UI elements in the constructor. Keep the constructor minimal.**
    
                    CRITICAL CONTENTVIEW COMPATIBILITY REQUIREMENTS:
                    15. **Your code will be converted from ContentPage to ContentView during compilation. Be aware of these differences:**
                        - **DO NOT use OnTapped() override** - Instead use TapGestureRecognizer:
                          ```csharp
                          // DON'T DO THIS:
                          protected override void OnTapped() { /* code */ }
      
                          // DO THIS INSTEAD:
                          private void OnTapped(object sender, EventArgs e) { /* code */ }
                          // And add in constructor or initialization: 
                          var tapGesture = new TapGestureRecognizer();
                          tapGesture.Tapped += OnTapped;
                          this.GestureRecognizers.Add(tapGesture);
                          ```
                        - **DO NOT use OnAppearing() override** - Instead use Loaded event:
                          ```csharp
                          // DON'T DO THIS:
                          protected override void OnAppearing() { /* code */ }
      
                          // DO THIS INSTEAD:
                          private void OnLoaded(object sender, EventArgs e) { /* code */ }
                          // And add: this.Loaded += OnLoaded;
                          ```
                        - **DO NOT use OnDisappearing() override** - Instead use Unloaded event:
                          ```csharp
                          // DON'T DO THIS:
                          protected override void OnDisappearing() { /* code */ }
      
                          // DO THIS INSTEAD:
                          private void OnUnloaded(object sender, EventArgs e) { /* code */ }
                          // And add: this.Unloaded += OnUnloaded;
                          ```
                        - **Navigation methods like OnNavigatedTo, OnNavigatedFrom, OnBackButtonPressed are NOT available**
                        - **ContentView does not have Title property** - this is removed automatically

                    Example structure for MainPage.xaml.cs:
                    ```csharp
                    using System.Collections.ObjectModel;
                    using Microsoft.Maui.Controls;
    
                    namespace YourApp
                    {
                        // Model classes go here
                        public class Task
                        {
                            public string Name { get; set; }
                            public bool IsCompleted { get; set; }
                        }
        
                        // Main page class
                        public partial class MainPage : ContentPage
                        {
                            // Your implementation here
                        }
                    }
                    ```
                    "),
                    new UserChatMessage(prompt)
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.3f,
                    MaxOutputTokenCount = 2048,
                    TopP = 1.0f,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);
                string result = completion.Content[0].Text.Trim();

                if (string.IsNullOrWhiteSpace(result))
                    return (false, null, "No result returned from Azure OpenAI.");

                return (true, result, null);
            }
            catch (RequestFailedException ex)
            {
                return (false, null, $"Azure AI request failed: {ex.Message} (Status: {ex.Status})");
            }
            catch (Exception ex)
            {
                return (false, null, $"Failed to evaluate expression: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests the connection to Azure OpenAI using the provided API key, endpoint, and deployment name.
        /// </summary>
        /// <param name="apiKey">The Azure OpenAI API key.</param>
        /// <param name="endpoint">The Azure OpenAI endpoint URI.</param>
        /// <param name="deploymentName">The model deployment name configured in Azure.</param>
        /// <returns>
        /// <c>true</c> if a response is received successfully from the service; otherwise <c>false</c>.
        /// </returns>
        public async Task<bool> TestConnectionAsync(string apiKey, string endpoint, string deploymentName)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKey) ||
                    string.IsNullOrEmpty(endpoint) ||
                    string.IsNullOrEmpty(deploymentName))
                {
                    return false;
                }

                var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

                ChatClient chatClient = client.GetChatClient(deploymentName);

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a helpful assistant."),
                    new UserChatMessage("Ping")
                };

                ChatCompletion response = await chatClient.CompleteChatAsync(messages);
                return response?.Content.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}