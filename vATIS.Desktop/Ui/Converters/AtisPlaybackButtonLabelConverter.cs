// <copyright file="AtisPlaybackButtonLabelConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Vatsim.Vatis.Ui.Converters;

/// <summary>
/// Converts a boolean value to a specific string label for audio playback button.
/// Implements the <see cref="Avalonia.Data.Converters.IValueConverter"/> interface.
/// </summary>
public class AtisPlaybackButtonLabelConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool flag)
        {
            return flag ? "Stop Playback" : "Listen";
        }

        return "Listen";
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
