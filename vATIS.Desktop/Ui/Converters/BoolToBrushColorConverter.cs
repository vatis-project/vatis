// <copyright file="BoolToBrushColorConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Vatsim.Vatis.Ui.Converters;

/// <summary>
/// Provides a value conversion from a boolean to an <see cref="IBrush"/>
/// using specified colors for true and false values.
/// </summary>
public class BoolToBrushColorConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the brush color that represents the "true" value in the conversion.
    /// </summary>
    public IBrush TrueColor { get; set; } = Brushes.Black;

    /// <summary>
    /// Gets or sets the brush color that represents the "false" value in the conversion.
    /// </summary>
    public IBrush FalseColor { get; set; } = Brushes.White;

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
        {
            return booleanValue ? this.TrueColor : this.FalseColor;
        }

        return this.FalseColor;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
