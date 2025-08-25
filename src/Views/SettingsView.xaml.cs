using MauiFlow.ViewModels;

namespace MauiFlow.Views;

public partial class SettingsView : ContentView
{
    private SettingsViewModel _viewModel;

    public SettingsView() : this(GetSettingsViewModel())
    {
        // Constructor chaining, calls the parameterized constructor
    }

    public SettingsView(SettingsViewModel settingsViewModel)
    {
        InitializeComponent();
        _viewModel = settingsViewModel;
        BindingContext = _viewModel;
        
        // Set initial tab state
        UpdateTabAppearance(true);
        
        // Subscribe to property changes to update theme picker
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Set initial theme picker selection
        SetThemePickerSelection();
    }

    static SettingsViewModel GetSettingsViewModel()
    {
        return Application.Current?.Handler?.MauiContext?.Services?.GetService<SettingsViewModel>()
            ?? throw new InvalidOperationException("Could not resolve SettingsViewModel");
    }

    private void OnAzureOpenAITabTapped(object sender, EventArgs e)
    {
        AzureOpenAIContent.IsVisible = true;
        AppSettingsContent.IsVisible = false;
        UpdateTabAppearance(true);
    }

    private void OnAppSettingsTabTapped(object sender, EventArgs e)
    {
        AzureOpenAIContent.IsVisible = false;
        AppSettingsContent.IsVisible = true;
        UpdateTabAppearance(false);
    }

    private void UpdateTabAppearance(bool isAzureOpenAIActive)
    {
        if (isAzureOpenAIActive)
        {
            AzureOpenAITab.BackgroundColor = Color.FromArgb("#512BD4");
            AppSettingsTab.BackgroundColor = Colors.Transparent;
        }
        else
        {
            AzureOpenAITab.BackgroundColor = Colors.Transparent;
            AppSettingsTab.BackgroundColor = Color.FromArgb("#512BD4");
        }
    }

    private void SetThemePickerSelection()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_viewModel?.ThemeOptions != null)
            {
                var selectedOption = _viewModel.ThemeOptions.FirstOrDefault(o => o.Value == _viewModel.SelectedTheme);
                if (selectedOption != null)
                {
                    ThemePicker.SelectedItem = selectedOption;
                }
            }
        });
    }

    private void OnThemePickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (ThemePicker.SelectedItem is ThemeOption selectedOption)
        {
            _viewModel.SelectedTheme = selectedOption.Value;
        }
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.SelectedTheme) || e.PropertyName == nameof(_viewModel.ThemeOptions))
        {
            SetThemePickerSelection();
        }
    }
}