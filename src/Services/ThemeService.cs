using MauiFlow.Models;

namespace MauiFlow.Services
{
    public class ThemeService
    {
        public void ApplyTheme(AppTheme theme)
        {
            switch (theme)
            {
                case AppTheme.Light:
                    Application.Current.UserAppTheme = Microsoft.Maui.ApplicationModel.AppTheme.Light;
                    break;
                case AppTheme.Dark:
                    Application.Current.UserAppTheme = Microsoft.Maui.ApplicationModel.AppTheme.Dark;
                    break;
                case AppTheme.System:
                default:
                    Application.Current.UserAppTheme = Microsoft.Maui.ApplicationModel.AppTheme.Unspecified;
                    break;
            }
        }

        public AppTheme GetCurrentTheme()
        {
            return Application.Current.UserAppTheme switch
            {
                Microsoft.Maui.ApplicationModel.AppTheme.Light => AppTheme.Light,
                Microsoft.Maui.ApplicationModel.AppTheme.Dark => AppTheme.Dark,
                Microsoft.Maui.ApplicationModel.AppTheme.Unspecified => AppTheme.System,
                _ => AppTheme.System
            };
        }
    }
}