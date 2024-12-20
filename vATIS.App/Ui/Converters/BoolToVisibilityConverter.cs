using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Vatsim.Vatis.Ui.Converters;
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return false;

        bool val = (bool)value;
        if (Invert)
        {
            val = !val;
        }
        return val;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Two-way binding is not supported by " + GetType().Name);
    }
}