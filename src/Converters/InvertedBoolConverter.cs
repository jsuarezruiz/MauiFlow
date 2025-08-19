using System.Globalization;

namespace MauiFlow.Converters
{
    /// <summary>
    /// A value converter that inverts boolean values.
    /// Converts <c>true</c> to <c>false</c> and <c>false</c> to <c>true</c>.
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool boolValue && boolValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool boolValue && boolValue);
        }
    }
}