using MauiFlow.ViewModels;

namespace MauiFlow.Views;

public partial class SettingsView : ContentView
{
    public SettingsView() : this(GetSettingsViewModel())
    {
        // Constructor chaining, calls the parameterized constructor
    }

    public SettingsView(SettingsViewModel settingsViewModel)
    {
        InitializeComponent();
        BindingContext = settingsViewModel;
    }

    static SettingsViewModel GetSettingsViewModel()
    {
        return Application.Current?.Handler?.MauiContext?.Services?.GetService<SettingsViewModel>()
            ?? throw new InvalidOperationException("Could not resolve SettingsViewModel");
    }
}