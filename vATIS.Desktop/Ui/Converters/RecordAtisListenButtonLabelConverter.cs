using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Vatsim.Vatis.Ui.Converters;

public class RecordAtisListenButtonLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool flag)
        {
            return flag ? "Stop Playback" : "Listen";
        }

        return "Listen";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}