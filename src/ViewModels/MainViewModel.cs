using CommunityToolkit.Maui.Storage;
using MauiFlow.Helpers;
using MauiFlow.Models;
using MauiFlow.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MauiFlow.ViewModels
{
    public class MainViewModel : BindableObject
    {
        const int MIN_PROMPT_LENGTH = 10;
        const int MAX_PROMPT_LENGTH = 4000;
        const string XAML_FILE_KEY = "MainPage.xaml";
        const string CODE_BEHIND_FILE_KEY = "MainPage.xaml.cs";
        const string CSHARP_FILE_KEY = "MainPage.cs";

        readonly AlertService _alertService;
        readonly AzureOpenAIService _azureOpenAIService;
        readonly AppHistoryService _appHistoryService;

        string _userPrompt = string.Empty;
        string _aiResponse = string.Empty;
        string _xamlCode = string.Empty;
        string _cSharpCode = string.Empty;
        bool _isSettingsVisible;
        bool _hasChatStarted;
        bool _isProcessingRequest;
        ObservableCollection<ChatMessage> _chatMessages;
        View _generatedPreview;
        ObservableCollection<AppHistoryItem> _appHistory;
        AppHistoryItem _selectedHistoryItem;

        public MainViewModel(
            AlertService alertService, 
            AzureOpenAIService azureOpenAIService,
            AppHistoryService appHistoryService)
        {
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _azureOpenAIService = azureOpenAIService ?? throw new ArgumentNullException(nameof(azureOpenAIService));
            _appHistoryService = appHistoryService ?? throw new ArgumentNullException(nameof(appHistoryService));

            _chatMessages = new ObservableCollection<ChatMessage>();
            _appHistory = new ObservableCollection<AppHistoryItem>();

            InitializeCommands();
            _ = LoadAppHistoryAsync(); // Load history in background
        }

        /// <summary>
        /// Gets or sets the user's input prompt for AI generation.
        /// </summary>
        public string UserPrompt
        {
            get => _userPrompt;
            set
            {
                _userPrompt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSubmitPrompt));
                RefreshCommandCanExecute();
            }
        }

        /// <summary>
        /// Gets or sets the AI's response to the user prompt.
        /// </summary>
        public string AIResponse
        {
            get => _aiResponse;
            set
            {
                _aiResponse = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the generated XAML code.
        /// </summary>
        public string XAMLCode
        {
            get => _xamlCode;
            set
            {
                _xamlCode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the generated C# code.
        /// </summary>
        public string CSharpCode
        {
            get => _cSharpCode;
            set
            {
                _cSharpCode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the settings panel is visible.
        /// </summary>
        public bool IsSettingsVisible
        {
            get => _isSettingsVisible;
            set
            {
                _isSettingsVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether a chat session has been initiated.
        /// </summary>
        public bool HasChatStarted
        {
            get => _hasChatStarted;
            set
            {
                _hasChatStarted = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether the application is currently processing a request.
        /// </summary>
        public bool IsProcessingRequest
        {
            get => _isProcessingRequest;
            set
            {
                _isProcessingRequest = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSubmitPrompt));
                RefreshCommandCanExecute();
            }
        }

        /// <summary>
        /// Gets or sets the collection of chat messages between user and AI.
        /// </summary>
        public ObservableCollection<ChatMessage> ChatMessages
        {
            get => _chatMessages;
            set
            {
                _chatMessages = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the generated UI preview view.
        /// </summary>
        public View GeneratedPreview
        {
            get => _generatedPreview;
            set
            {
                _generatedPreview = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets whether the user can currently submit a prompt.
        /// </summary>
        public bool CanSubmitPrompt => !IsProcessingRequest;

        /// <summary>
        /// Gets or sets the collection of saved app history items.
        /// </summary>
        public ObservableCollection<AppHistoryItem> AppHistory
        {
            get => _appHistory;
            set
            {
                _appHistory = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the currently selected app history item.
        /// </summary>
        public AppHistoryItem SelectedHistoryItem
        {
            get => _selectedHistoryItem;
            set
            {
                if (_selectedHistoryItem != value)
                {
                    // Deselect previous item
                    if (_selectedHistoryItem != null)
                        _selectedHistoryItem.IsSelected = false;

                    _selectedHistoryItem = value;
                    
                    // Select new item
                    if (_selectedHistoryItem != null)
                        _selectedHistoryItem.IsSelected = true;

                    OnPropertyChanged();
                    LoadSelectedAppCommand?.Execute(null);
                }
            }
        }

        /// <summary>
        /// Command to toggle the settings panel visibility.
        /// </summary>
        public ICommand ToggleSettingsCommand { get; set; }

        /// <summary>
        /// Command to evaluate the user's prompt using AI.
        /// </summary>
        public ICommand EvaluatePromptCommand { get; set; }

        /// <summary>
        /// Command to copy the generated XAML code to clipboard.
        /// </summary>
        public ICommand CopyXamlCommand { get; set; }

        /// <summary>
        /// Command to copy the generated C# code to clipboard.
        /// </summary>
        public ICommand CopyCSharpCommand { get; set; }

        /// <summary>
        /// Command to reset the application state.
        /// </summary>
        public ICommand ResetCommand { get; set; }

        /// <summary>
        /// Command to create a new app (clear current state).
        /// </summary>
        public ICommand NewAppCommand { get; set; }

        /// <summary>
        /// Command to load a selected app from history.
        /// </summary>
        public ICommand LoadSelectedAppCommand { get; set; }

        /// <summary>
        /// Command to save the current app to history.
        /// </summary>
        public ICommand SaveCurrentAppCommand { get; set; }

        /// <summary>
        /// Initializes all command objects.
        /// </summary>
        void InitializeCommands()
        {
            ToggleSettingsCommand = new Command(ToggleSettingsVisibility);
            EvaluatePromptCommand = new Command(async (parameter) => await ProcessUserPromptAsync(parameter as string), (obj) => CanExecuteEvaluatePrompt());
            CopyXamlCommand = new Command(async () => await CopyToClipboard(XAMLCode));
            CopyCSharpCommand = new Command(async () => await CopyToClipboard(CSharpCode));
            ResetCommand = new Command(ResetApplicationState);
            NewAppCommand = new Command(CreateNewApp);
            LoadSelectedAppCommand = new Command(async () => await LoadSelectedAppAsync());
            SaveCurrentAppCommand = new Command(async () => await SaveCurrentAppAsync(), () => CanSaveCurrentApp());
        }

        /// <summary>
        /// Toggles the visibility of the settings panel.
        /// </summary>
        void ToggleSettingsVisibility() => IsSettingsVisible = !IsSettingsVisible;

        /// <summary>
        /// Determines whether the evaluate prompt command can be executed.
        /// </summary>
        /// <returns>True if the command can be executed; otherwise, false.</returns>
        bool CanExecuteEvaluatePrompt() => CanSubmitPrompt;

        /// <summary>
        /// Processes the user's prompt by sending it to AI and handling the response.
        /// </summary>
        async Task ProcessUserPromptAsync(string parameter)
        {
            if (!string.IsNullOrEmpty(parameter))
                UserPrompt = parameter;

            if (IsProcessingRequest || !IsValidPrompt(UserPrompt))
                return;

            try
            {
                BeginProcessing();
                await SendPromptToAIAsync();
            }
            catch (HttpRequestException httpEx)
            {
                await HandleNetworkErrorAsync(httpEx);
            }
            catch (TaskCanceledException timeoutEx) when (timeoutEx.CancellationToken.IsCancellationRequested)
            {
                await HandleTimeoutErrorAsync(timeoutEx);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                await HandleInvalidOperationErrorAsync(invalidOpEx);
            }
            catch (UnauthorizedAccessException authEx)
            {
                await HandleAuthorizationErrorAsync(authEx);
            }
            catch (Exception generalEx)
            {
                await HandleGeneralErrorAsync(generalEx);
            }
            finally
            {
                CompleteProcessing();
            }
        }

        /// <summary>
        /// Begins the processing state and initializes chat if needed.
        /// </summary>
        void BeginProcessing()
        {
            IsProcessingRequest = true;
            HasChatStarted = true;
            ClearPreviousResults();

            AddUserMessageToChat(UserPrompt);
        }

        /// <summary>
        /// Sends the user's prompt to the AI service and processes the response.
        /// </summary>
        async Task SendPromptToAIAsync()
        {
            var aiResponseMessage = AddAIResponsePlaceholderToChat();

            var aiResult = await _azureOpenAIService.EvaluatePromptAsync(UserPrompt);

            if (aiResult.Success && !string.IsNullOrEmpty(aiResult.Result))
            {
                await ProcessSuccessfulAIResponseAsync(aiResult.Result, aiResponseMessage);
            }
            else
            {
                await HandleAIServiceErrorAsync(aiResult.ErrorMessage, aiResponseMessage);
            }
        }

        /// <summary>
        /// Processes a successful AI response by updating UI and attempting compilation.
        /// </summary>
        async Task ProcessSuccessfulAIResponseAsync(string aiResult, ChatMessage responseMessage)
        {
            AIResponse = aiResult;
            responseMessage.Content = aiResult;

            try
            {
                var extractedFiles = FileHelper.ExtractFiles(AIResponse);
                await AttemptUICompilationAsync(extractedFiles);
            }
            catch (ArgumentException argEx)
            {
                await _alertService.ShowAlertAsync("Invalid File Format",
                    $"The generated files have an invalid format: {argEx.Message}");
            }
            catch (Exception ex)
            {
                await _alertService.ShowAlertAsync("File Processing Error",
                    $"Failed to process generated files: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to compile and preview the generated UI files.
        /// </summary>
        async Task AttemptUICompilationAsync(Dictionary<string, string> extractedFiles)
        {
            extractedFiles.TryGetValue(XAML_FILE_KEY, out var xamlContent);

            // Try primary code-behind file first, then fallback to MainPage.cs
            string? codeContent = null;
            if (!extractedFiles.TryGetValue(CODE_BEHIND_FILE_KEY, out codeContent))
            {
                extractedFiles.TryGetValue(CSHARP_FILE_KEY, out codeContent);
            }

            try
            {
                var compilationResult = await CompilerHelper.CompileAsync(xamlContent, codeContent);

                if (compilationResult.Success && compilationResult.ContentView?.Content is not null)
                {
                    XAMLCode = xamlContent;
                    CSharpCode = codeContent;
                    GeneratedPreview = compilationResult.ContentView.Content;
                    
                    // Auto-save to history when successfully generating a new app
                    _ = Task.Run(async () => await SaveCurrentAppAsync());
                    
                    UserPrompt = string.Empty;
                }
                else
                {
                    await HandleCompilationErrorAsync(compilationResult.ErrorMessage);
                }
            }
            catch (ArgumentException argEx)
            {
                await HandleCompilationErrorAsync($"Invalid XAML format: {argEx.Message}");
            }
            catch (XamlParseException xamlEx)
            {
                await HandleCompilationErrorAsync($"XAML parsing failed: {xamlEx.Message}");
            }
            catch (Exception compileEx)
            {
                await HandleCompilationErrorAsync($"Compilation failed: {compileEx.Message}");
            }
        }

        /// <summary>
        /// Handles compilation errors by showing appropriate user feedback.
        /// </summary>
        async Task HandleCompilationErrorAsync(string errorMessage)
        {
            var displayMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? "Unknown compilation error occurred."
                : errorMessage;

            await _alertService.ShowAlertAsync("UI Compilation Error",
                $"Failed to compile generated UI:\n{displayMessage}");
        }

        /// <summary>
        /// Adds a user message to the chat collection.
        /// </summary>
        void AddUserMessageToChat(string message) =>
            ChatMessages.Add(new ChatMessage { Sender = true, Content = message });

        /// <summary>
        /// Adds a placeholder AI response message to the chat and returns it for later updates.
        /// </summary>
        ChatMessage AddAIResponsePlaceholderToChat()
        {
            var responseMessage = new ChatMessage { Sender = false, Content = string.Empty };
            ChatMessages.Add(responseMessage);
            return responseMessage;
        }

        /// <summary>
        /// Copies the provided text to the clipboard.
        /// </summary>
        /// <param name="text">The text to copy.</param>
        async Task CopyToClipboard(string text)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    await Clipboard.Default.SetTextAsync(text);
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that might occur during copy operation
                System.Diagnostics.Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears previous AI response and preview results.
        /// </summary>
        void ClearPreviousResults()
        {
            AIResponse = string.Empty;
            GeneratedPreview = null;
        }

        /// <summary>
        /// Completes the processing state.
        /// </summary>
        void CompleteProcessing() => IsProcessingRequest = false;

        /// <summary>
        /// Refreshes the CanExecute state of commands that depend on current state.
        /// </summary>
        void RefreshCommandCanExecute()
        {
            if (EvaluatePromptCommand is Command command)
                command.ChangeCanExecute();
            if (SaveCurrentAppCommand is Command saveCommand)
                saveCommand.ChangeCanExecute();
        }

        /// <summary>
        /// Creates a new app by resetting the application state.
        /// </summary>
        void CreateNewApp()
        {
            ResetApplicationState();
        }

        /// <summary>
        /// Loads the app history from storage.
        /// </summary>
        async Task LoadAppHistoryAsync()
        {
            try
            {
                var history = await _appHistoryService.LoadHistoryAsync();
                AppHistory = history;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading app history: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the selected app from history into the current session.
        /// </summary>
        async Task LoadSelectedAppAsync()
        {
            if (SelectedHistoryItem == null)
                return;

            try
            {
                // Load the app data
                UserPrompt = SelectedHistoryItem.Description;
                XAMLCode = SelectedHistoryItem.XamlCode;
                CSharpCode = SelectedHistoryItem.CsharpCode;
                
                // Load chat history
                ChatMessages.Clear();
                foreach (var message in SelectedHistoryItem.ChatHistory)
                {
                    ChatMessages.Add(message);
                }

                HasChatStarted = ChatMessages.Count > 0;

                // Try to compile and show the preview if we have code
                if (!string.IsNullOrEmpty(XAMLCode) || !string.IsNullOrEmpty(CSharpCode))
                {
                    var extractedFiles = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(XAMLCode))
                        extractedFiles[XAML_FILE_KEY] = XAMLCode;
                    if (!string.IsNullOrEmpty(CSharpCode))
                        extractedFiles[CODE_BEHIND_FILE_KEY] = CSharpCode;

                    await AttemptUICompilationAsync(extractedFiles);
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowAlertAsync("Error", $"Failed to load app: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the current app state to history.
        /// </summary>
        async Task SaveCurrentAppAsync()
        {
            if (!CanSaveCurrentApp())
                return;

            try
            {
                var appName = _appHistoryService.GenerateAppName(UserPrompt);
                
                await _appHistoryService.AddAppToHistoryAsync(
                    AppHistory,
                    appName,
                    UserPrompt,
                    ChatMessages,
                    XAMLCode,
                    CSharpCode);

                await _alertService.ShowAlertAsync("Success", $"App '{appName}' saved to history!");
            }
            catch (Exception ex)
            {
                await _alertService.ShowAlertAsync("Error", $"Failed to save app: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines whether the current app can be saved.
        /// </summary>
        bool CanSaveCurrentApp()
        {
            return HasChatStarted && 
                   (!string.IsNullOrEmpty(XAMLCode) || !string.IsNullOrEmpty(CSharpCode)) &&
                   !string.IsNullOrEmpty(UserPrompt);
        }

        /// <summary>
        /// Handles network-related errors during AI communication.
        /// </summary>
        async Task HandleNetworkErrorAsync(HttpRequestException httpEx) =>
            await _alertService.ShowAlertAsync("Network Error",
                "Unable to connect to AI service. Please check your internet connection and try again.");

        /// <summary>
        /// Handles timeout errors during AI communication.
        /// </summary>
        async Task HandleTimeoutErrorAsync(TaskCanceledException timeoutEx) =>
            await _alertService.ShowAlertAsync("Timeout Error",
                "The request took too long to process. Please try again with a shorter prompt.");

        /// <summary>
        /// Handles invalid operation errors.
        /// </summary>
        async Task HandleInvalidOperationErrorAsync(InvalidOperationException invalidOpEx) =>
            await _alertService.ShowAlertAsync("Configuration Error",
                "There's an issue with the application configuration. Please check your settings.");

        /// <summary>
        /// Handles authorization errors.
        /// </summary>
        async Task HandleAuthorizationErrorAsync(UnauthorizedAccessException authEx) =>
            await _alertService.ShowAlertAsync("Authorization Error",
                "Access denied. Please check your API credentials in settings.");

        /// <summary>
        /// Handles general unexpected errors.
        /// </summary>
        async Task HandleGeneralErrorAsync(Exception generalEx) =>
            await _alertService.ShowAlertAsync("Unexpected Error",
                $"An unexpected error occurred: {generalEx.Message}");

        /// <summary>
        /// Handles errors from the AI service.
        /// </summary>
        async Task HandleAIServiceErrorAsync(string errorMessage, ChatMessage responseMessage)
        {
            var displayMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? "An unknown error occurred while generating the app."
                : errorMessage;

            await _alertService.ShowAlertAsync("AI Service Error", displayMessage);
            responseMessage.Content = "Sorry, I encountered an error processing your request.";
            AIResponse = string.Empty;
        }

        /// <summary>
        /// Validates whether the provided prompt meets the application's requirements.
        /// </summary>
        /// <param name="prompt">The prompt to validate.</param>
        /// <returns>True if the prompt is valid; otherwise, false.</returns>
        public bool IsValidPrompt(string prompt) =>
            !string.IsNullOrWhiteSpace(prompt) &&
            prompt.Length >= MIN_PROMPT_LENGTH &&
            prompt.Length <= MAX_PROMPT_LENGTH;

        /// <summary>
        /// Resets the application to its initial state.
        /// </summary>
        public void ResetApplicationState()
        {
            UserPrompt = string.Empty;
            AIResponse = string.Empty;
            IsSettingsVisible = false;
            IsProcessingRequest = false;
            HasChatStarted = false;
            XAMLCode = string.Empty;
            CSharpCode = string.Empty;
            GeneratedPreview = null;
            ChatMessages.Clear();
        }

        /// <summary>
        /// Gets detailed validation information for a prompt.
        /// </summary>
        /// <param name="prompt">The prompt to validate.</param>
        /// <returns>A validation result with details about any issues.</returns>
        public ValidationResult ValidatePromptWithDetails(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return new ValidationResult(false, "Prompt cannot be empty.");

            if (prompt.Length < MIN_PROMPT_LENGTH)
                return new ValidationResult(false, $"Prompt must be at least {MIN_PROMPT_LENGTH} characters.");

            if (prompt.Length > MAX_PROMPT_LENGTH)
                return new ValidationResult(false, $"Prompt cannot exceed {MAX_PROMPT_LENGTH} characters.");

            return new ValidationResult(true, "Prompt is valid.");
        }

        /// <summary>
        /// Represents the result of prompt validation.
        /// </summary>
        /// <param name="IsValid">Whether the prompt is valid.</param>
        /// <param name="Message">Validation message.</param>
        public record ValidationResult(bool IsValid, string Message);
    }
}
