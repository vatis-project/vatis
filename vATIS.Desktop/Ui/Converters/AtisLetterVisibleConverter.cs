// <copyright file="AtisLetterVisibleConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Vatsim.Vatis.Networking;

namespace Vatsim.Vatis.Ui.Converters;

/// <summary>
/// Provides functionality to convert a <see cref="NetworkConnectionStatus"/>
/// value to a boolean indicating whether the ATIS letter should be visible.
/// </summary>
public class AtisLetterVisibleConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetworkConnectionStatus status)
        {
            return status is NetworkConnectionStatus.Connected or NetworkConnectionStatus.Observer;
        }

        return false;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
