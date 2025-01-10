using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Vatsim.Vatis.Ui.Converters;

public class BoolToBrushColorConverter : IValueConverter
{
    public IBrush TrueColor { get; set; } = Brushes.Black;

    public IBrush FalseColor { get; set; } = Brushes.White;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
        {
            return booleanValue ? this.TrueColor : this.FalseColor;
        }

        return this.FalseColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}