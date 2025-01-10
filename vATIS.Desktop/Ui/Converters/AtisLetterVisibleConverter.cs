using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Vatsim.Vatis.Networking;

namespace Vatsim.Vatis.Ui.Converters;

public class AtisLetterVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetworkConnectionStatus status)
        {
            return status is NetworkConnectionStatus.Connected or NetworkConnectionStatus.Observer;
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}