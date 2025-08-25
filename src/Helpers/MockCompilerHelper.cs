using MauiFlow.Models;

namespace MauiFlow.Helpers
{
    /// <summary>
    /// Mock implementation of CompilerHelper for testing the history functionality.
    /// </summary>
    public static class MockCompilerHelper
    {
        public static async Task<CompilerResult> CompileAsync(string xamlContent, string codeBehindContent = null)
        {
            await Task.Delay(500); // Simulate compilation time

            var result = new CompilerResult
            {
                Success = true,
                ErrorMessage = null
            };

            try
            {
                // Create a simple mock UI for demonstration
                var mockContentView = new ContentView
                {
                    Content = new StackLayout
                    {
                        Padding = new Thickness(20),
                        Spacing = 10,
                        Children =
                        {
                            new Label
                            {
                                Text = "📱 Mock App Preview",
                                FontSize = 18,
                                HorizontalOptions = LayoutOptions.Center,
                                FontAttributes = FontAttributes.Bold
                            },
                            new Label
                            {
                                Text = "This is a mock preview of your generated app.",
                                FontSize = 14,
                                HorizontalOptions = LayoutOptions.Center,
                                TextColor = Colors.Gray
                            },
                            new Button
                            {
                                Text = "Sample Button",
                                BackgroundColor = Colors.Purple,
                                TextColor = Colors.White,
                                CornerRadius = 8,
                                Margin = new Thickness(0, 10)
                            },
                            new Entry
                            {
                                Placeholder = "Sample Entry",
                                Margin = new Thickness(0, 5)
                            },
                            new Label
                            {
                                Text = $"Generated from XAML ({xamlContent?.Length ?? 0} chars)",
                                FontSize = 12,
                                TextColor = Colors.LightGray,
                                HorizontalOptions = LayoutOptions.Center
                            }
                        }
                    }
                };

                result.ContentView = mockContentView;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Mock compilation error: {ex.Message}";
            }

            return result;
        }
    }
}