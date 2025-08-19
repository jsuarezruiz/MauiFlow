using MauiFlow.ViewModels;
using MauiFlow.Views;

namespace MauiFlow
{
    public partial class App : Application
    {
        MainViewModel _mainViewModel;

        public App(MainViewModel mainViewModel)
        {
            InitializeComponent();

            _mainViewModel = mainViewModel;
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
    }
}