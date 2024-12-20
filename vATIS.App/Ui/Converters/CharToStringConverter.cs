using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Vatsim.Vatis.Ui.Converters;
public class CharToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is char c && c != '\0')
        {
            return c.ToString();
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrEmpty(s))
        {
            return s[0];
        }

        return default(char);
    }
}
