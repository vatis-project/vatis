// <copyright file="ConnectButtonLabelConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Vatsim.Vatis.Networking;

namespace Vatsim.Vatis.Ui.Converters;

public class ConnectButtonLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetworkConnectionStatus flag)
        {
            return flag switch
            {
                NetworkConnectionStatus.Connected => "DISCONNECT",
                NetworkConnectionStatus.Connecting => "CONNECTING",
                NetworkConnectionStatus.Disconnected => "CONNECT",
                _ => "CONNECT"
            };
        }

        return "CONNECT";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
