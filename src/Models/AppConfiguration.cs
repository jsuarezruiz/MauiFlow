namespace MauiFlow.Models
{
    public class AppConfiguration
    {
        public AppTheme Theme { get; set; } = AppTheme.System;
        public string DefaultProjectPath { get; set; } = string.Empty;
    }
}