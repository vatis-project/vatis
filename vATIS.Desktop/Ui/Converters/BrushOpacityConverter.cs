// <copyright file="BrushOpacityConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Vatsim.Vatis.Ui.Converters;

/// <summary>
/// A value converter that adjusts the opacity of a <see cref="SolidColorBrush"/>.
/// </summary>
public class BrushOpacityConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush solidBrush)
        {
            var opacity = 0.5; // Default opacity
            if (parameter != null && double.TryParse(parameter.ToString(), out var parsedOpacity))
            {
                opacity = parsedOpacity;
            }

            return new SolidColorBrush(solidBrush.Color, opacity);
        }

        return value;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
