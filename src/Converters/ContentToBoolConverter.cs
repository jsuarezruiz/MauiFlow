using System.Globalization;

namespace MauiFlow.Converters
{
    /// <summary>
    /// Converts a content value to a <see cref="bool"/> for use in bindings.
    /// Returns <c>true</c> if the value is non-null and, in the case of a string,
    /// not empty or whitespace. Returns <c>false</c> otherwise.
    /// </summary>
    public class ContentToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
                return !string.IsNullOrWhiteSpace(str);

            return value is not null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported for ContentToBoolConverter.");
        }
    }
}
