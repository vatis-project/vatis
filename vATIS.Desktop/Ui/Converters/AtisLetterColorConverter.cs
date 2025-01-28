// <copyright file="AtisLetterColorConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Vatsim.Vatis.Networking;

namespace Vatsim.Vatis.Ui.Converters;

/// <summary>
/// Provides a converter for defining the ATIS letter color in the tab according to the network connection status.
/// </summary>
public class AtisLetterColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetworkConnectionStatus status)
        {
            return status switch
            {
                NetworkConnectionStatus.Connected => Brushes.Aqua,
                NetworkConnectionStatus.Observer => Brushes.Coral,
                _ => Brushes.Aqua
            };
        }

        return Brushes.Aqua;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
