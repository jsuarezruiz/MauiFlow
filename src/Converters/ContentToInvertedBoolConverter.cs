using System.Globalization;

namespace MauiFlow.Converters
{
    /// <summary>
    /// Returns true if the bound content is null; otherwise false.
    /// </summary>
    public class ContentToInvertedBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
                return string.IsNullOrWhiteSpace(str);

            return value is null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported for ContentToInvertedBoolConverter.");
        }
    }
}