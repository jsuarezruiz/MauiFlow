using MauiFlow.Services;
using MauiFlow.ViewModels;
using MauiFlow.Views;

namespace MauiFlow
{
    public partial class App : Application
    {
        MainViewModel _mainViewModel;
        ThemeService _themeService;
        SettingsService _settingsService;

        public App(MainViewModel mainViewModel, ThemeService themeService, SettingsService settingsService)
        {
            InitializeComponent();

            _mainViewModel = mainViewModel;
            _themeService = themeService;
            _settingsService = settingsService;

            // Apply saved theme on startup
            _ = Task.Run(LoadAndApplyThemeAsync);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainView(_mainViewModel))
            {
                Title = "MauiFlow",
                MinimumWidth = 1200,
                MinimumHeight = 700,
            };
        }

        private async Task LoadAndApplyThemeAsync()
        {
            try
            {
                var appConfig = await _settingsService.LoadAppSettingsAsync();
                _themeService.ApplyTheme(appConfig.Theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
            }
        }
    }
}