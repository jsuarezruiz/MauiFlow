using System.Globalization;

namespace MauiFlow.Converters
{
    /// <summary>
    /// A value converter that calculates the number of lines in a given string.
    /// Splits the input text by common newline characters (<c>\r\n</c>, <c>\r</c>, <c>\n</c>) 
    /// and returns the total count of lines.
    /// </summary>
    public class LineCountConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                // Split by line breaks and count non-empty lines
                var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                return lines.Length;
            }

            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Not typically needed for line count
            throw new NotSupportedException("ConvertBack is not supported for LineCountConverter.");
        }
    }
}
