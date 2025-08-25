namespace MauiFlow.Services
{
    /// <summary>
    /// Mock implementation of AlertService for testing.
    /// </summary>
    public class MockAlertService
    {
        public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            await Task.Delay(100);
            System.Diagnostics.Debug.WriteLine($"Alert: {title} - {message}");
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
        {
            await Task.Delay(100);
            System.Diagnostics.Debug.WriteLine($"Confirmation: {title} - {message}");
            return true;
        }

        public async Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons)
        {
            await Task.Delay(100);
            System.Diagnostics.Debug.WriteLine($"Action Sheet: {title}");
            return buttons?.FirstOrDefault() ?? cancel;
        }

        public async Task<string> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = "", int maxLength = -1, Keyboard keyboard = null, string initialValue = "")
        {
            await Task.Delay(100);
            System.Diagnostics.Debug.WriteLine($"Prompt: {title} - {message}");
            return "Mock Response";
        }
    }
}