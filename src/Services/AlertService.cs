namespace MauiFlow.Services
{
    public class AlertService
    {
        // Alert methods
        public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            try
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    await page.DisplayAlertAsync(title, message, cancel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing alert: {ex.Message}");
            }
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
        {
            try
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    return await page.DisplayAlertAsync(title, message, accept, cancel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing confirmation: {ex.Message}");
            }
            return false;
        }

        public async Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons)
        {
            try
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    return await page.DisplayActionSheetAsync(title, cancel, destruction, buttons);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing action sheet: {ex.Message}");
            }
            return cancel;
        }

        public async Task<string> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = "", int maxLength = -1, Keyboard keyboard = null, string initialValue = "")
        {
            try
            {
                var page = GetCurrentPage();
                if (page != null)
                {
                    return await page.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard ?? Keyboard.Default, initialValue);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing prompt: {ex.Message}");
            }
            return null;
        }

        // Helper method to get current page without using obsolete MainPage
        private static Page GetCurrentPage()
        {
            try
            {
                if (Application.Current?.Windows?.FirstOrDefault()?.Page is Page page)
                {
                    // Handle different page types
                    return page switch
                    {
                        Shell shell => shell.CurrentPage ?? shell,
                        NavigationPage navPage => navPage.CurrentPage ?? navPage,
                        TabbedPage tabbedPage => tabbedPage.CurrentPage ?? tabbedPage,
                        FlyoutPage flyoutPage => flyoutPage.Detail ?? flyoutPage,
                        _ => page
                    };
                }

                // Fallback for older MAUI versions or specific scenarios
#pragma warning disable CS0618 // Type or member is obsolete
                return Application.Current?.MainPage;
#pragma warning restore CS0618 // Type or member is obsolete
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current page: {ex.Message}");
                return null;
            }
        }
    }
}