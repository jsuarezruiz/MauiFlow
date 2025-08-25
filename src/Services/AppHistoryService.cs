using System.Collections.ObjectModel;
using System.Text.Json;
using MauiFlow.Models;

namespace MauiFlow.Services
{
    /// <summary>
    /// Service for managing app history persistence using local JSON storage.
    /// </summary>
    public class AppHistoryService
    {
        const string HISTORY_FILE_NAME = "mauiflow_history.json";
        readonly string _historyFilePath;

        public AppHistoryService()
        {
            _historyFilePath = Path.Combine(FileSystem.AppDataDirectory, HISTORY_FILE_NAME);
        }

        /// <summary>
        /// Loads the app history from local storage.
        /// </summary>
        /// <returns>A collection of saved app history items.</returns>
        public async Task<ObservableCollection<AppHistoryItem>> LoadHistoryAsync()
        {
            try
            {
                if (!File.Exists(_historyFilePath))
                {
                    return new ObservableCollection<AppHistoryItem>();
                }

                var json = await File.ReadAllTextAsync(_historyFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new ObservableCollection<AppHistoryItem>();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var historyList = JsonSerializer.Deserialize<List<AppHistoryItem>>(json, options);
                return new ObservableCollection<AppHistoryItem>(historyList ?? new List<AppHistoryItem>());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading app history: {ex.Message}");
                return new ObservableCollection<AppHistoryItem>();
            }
        }

        /// <summary>
        /// Saves the app history to local storage.
        /// </summary>
        /// <param name="history">The collection of app history items to save.</param>
        public async Task SaveHistoryAsync(ObservableCollection<AppHistoryItem> history)
        {
            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(_historyFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(history.ToList(), options);
                await File.WriteAllTextAsync(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving app history: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a new app to the history and saves it.
        /// </summary>
        /// <param name="history">The current history collection.</param>
        /// <param name="appName">The name of the app.</param>
        /// <param name="description">The app description or initial prompt.</param>
        /// <param name="chatHistory">The chat messages for this app.</param>
        /// <param name="xamlCode">The generated XAML code.</param>
        /// <param name="csharpCode">The generated C# code.</param>
        /// <returns>The newly created history item.</returns>
        public async Task<AppHistoryItem> AddAppToHistoryAsync(
            ObservableCollection<AppHistoryItem> history,
            string appName,
            string description,
            ObservableCollection<ChatMessage> chatHistory,
            string xamlCode,
            string csharpCode)
        {
            var newApp = new AppHistoryItem
            {
                AppName = appName,
                Description = description,
                DateCreated = DateTime.Now,
                LastModified = DateTime.Now,
                ChatHistory = new ObservableCollection<ChatMessage>(chatHistory),
                XamlCode = xamlCode,
                CsharpCode = csharpCode
            };

            // Add to the beginning of the list so newest appears first
            history.Insert(0, newApp);

            await SaveHistoryAsync(history);
            return newApp;
        }

        /// <summary>
        /// Updates an existing app in the history and saves it.
        /// </summary>
        /// <param name="history">The current history collection.</param>
        /// <param name="app">The app to update.</param>
        public async Task UpdateAppInHistoryAsync(ObservableCollection<AppHistoryItem> history, AppHistoryItem app)
        {
            app.LastModified = DateTime.Now;
            await SaveHistoryAsync(history);
        }

        /// <summary>
        /// Removes an app from the history and saves the changes.
        /// </summary>
        /// <param name="history">The current history collection.</param>
        /// <param name="app">The app to remove.</param>
        public async Task RemoveAppFromHistoryAsync(ObservableCollection<AppHistoryItem> history, AppHistoryItem app)
        {
            history.Remove(app);
            await SaveHistoryAsync(history);
        }

        /// <summary>
        /// Generates a suitable app name from a user prompt.
        /// </summary>
        /// <param name="prompt">The user's input prompt.</param>
        /// <returns>A suitable app name.</returns>
        public string GenerateAppName(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "Untitled App";

            // Take first few words and clean them up
            var words = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .Take(3)
                              .Select(w => w.Trim().TrimEnd(',', '.', '!', '?'))
                              .Where(w => !string.IsNullOrEmpty(w));

            var appName = string.Join(" ", words);
            
            // Capitalize first letter
            if (!string.IsNullOrEmpty(appName))
            {
                appName = char.ToUpper(appName[0]) + appName.Substring(1);
            }

            return string.IsNullOrEmpty(appName) ? "Untitled App" : appName;
        }
    }
}