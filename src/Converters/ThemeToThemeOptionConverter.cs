using System.Globalization;
using MauiFlow.Models;
using MauiFlow.ViewModels;

namespace MauiFlow.Converters
{
    public class ThemeToThemeOptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppTheme theme && parameter is IEnumerable<ThemeOption> options)
            {
                return options.FirstOrDefault(o => o.Value == theme);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ThemeOption option)
            {
                return option.Value;
            }
            return AppTheme.System;
        }
    }
}