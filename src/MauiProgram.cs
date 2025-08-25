using CommunityToolkit.Maui;
using Indiko.Maui.Controls.Markdown;
using MauiFlow.Services;
using MauiFlow.ViewModels;
using MauiFlow.Views;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;

namespace MauiFlow
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.UseMauiCommunityToolkit();
            builder.ConfigureSyncfusionToolkit();
            builder.UseMarkdownView();

            builder.Services.AddSingleton<AlertService>();
            builder.Services.AddSingleton<AzureOpenAIService>();
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<ThemeService>();

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>()
                ;
            builder.Services.AddSingleton<MainView>();
            builder.Services.AddSingleton<SettingsView>();

            return builder.Build();
        }
    }
}
