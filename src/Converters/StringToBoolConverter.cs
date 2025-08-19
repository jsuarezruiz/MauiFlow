using System.Globalization;

namespace MauiFlow.Converters
{
    /// <summary>
    /// A value converter that converts between <see cref="string"/> and <see cref="bool"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>Convert:</b> Returns <c>true</c> if the input is a non-empty, non-whitespace string; otherwise <c>false</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>ConvertBack:</b> Returns <c>"true"</c> if the input is <c>true</c>, or an empty string if <c>false</c>.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public class StringToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "true" : string.Empty;
            }

            return string.Empty;
        }
    }
}
