using MauiFlow.ViewModels;

namespace MauiFlow.Views
{
    public partial class MainView : ContentPage
    {
        public MainView(MainViewModel mainViewModel)
        {
            InitializeComponent();

            BindingContext = mainViewModel;
        }
    }
}