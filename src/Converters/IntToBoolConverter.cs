using System.Globalization;

namespace MauiFlow.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        /// <summary>
        /// Converts an integer value to a boolean by comparing it to a target value provided as a parameter.
        /// Returns true if the values match; otherwise, returns false.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string paramString && int.TryParse(paramString, out int targetValue))
            {
                return intValue == targetValue;
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported for IntToBoolConverter.");
        }
    }
}
