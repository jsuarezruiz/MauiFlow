using MauiFlow.Services;

namespace MauiFlow.Services
{
    /// <summary>
    /// Mock implementation of Azure OpenAI Service for testing the history functionality.
    /// </summary>
    public class MockAzureOpenAIService
    {
        private readonly SettingsService _settingsService;

        public MockAzureOpenAIService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// Mock implementation that returns a sample XAML/C# app for testing.
        /// </summary>
        public async Task<(bool Success, string Result, string ErrorMessage)> EvaluatePromptAsync(string prompt)
        {
            // Simulate async operation
            await Task.Delay(1000);

            var sampleXamlApp = $@"**MainPage.xaml**
```xml
<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage xmlns=""http://schemas.microsoft.com/dotnet/maui/global""
             xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
             x:Class=""TestApp.MainPage""
             Title=""Sample App - {prompt.Substring(0, Math.Min(20, prompt.Length))}"">
    <ScrollView>
        <VerticalStackLayout Spacing=""25"" Padding=""30,0""
                             VerticalOptions=""Center"">
            <Label Text=""Welcome to your {prompt.Substring(0, Math.Min(10, prompt.Length))} app!""
                   FontSize=""32""
                   HorizontalOptions=""Center"" />

            <Label Text=""This is a sample app generated based on your prompt:""
                   FontSize=""16""
                   HorizontalOptions=""Center"" />

            <Label Text=""{prompt}""
                   FontSize=""14""
                   FontAttributes=""Italic""
                   HorizontalOptions=""Center""
                   Margin=""20"" />

            <Button Text=""Click Me!""
                    Clicked=""OnCounterClicked""
                    HorizontalOptions=""Center"" />

            <Label x:Name=""CounterLabel""
                   Text=""Clicked 0 times""
                   FontSize=""18""
                   HorizontalOptions=""Center"" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

**MainPage.xaml.cs**
```csharp
namespace TestApp
{{
    public partial class MainPage : ContentPage
    {{
        private int _count = 0;

        public MainPage()
        {{
            InitializeComponent();
        }}

        private void OnCounterClicked(object sender, EventArgs e)
        {{
            _count++;
            CounterLabel.Text = $""Clicked {{_count}} times"";
        }}
    }}
}}
```";

            return (true, sampleXamlApp, string.Empty);
        }

        /// <summary>
        /// Mock test connection that always returns true.
        /// </summary>
        public async Task<bool> TestConnectionAsync(string apiKey, string endpoint, string deploymentName)
        {
            await Task.Delay(500);
            return true;
        }
    }
}