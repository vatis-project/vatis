// <copyright file="EnumToBrushConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Vatsim.Vatis.Ui.Converters;

/// <summary>
/// Converts an enumeration value to a brush color based on whether it matches a specified target value.
/// </summary>
public class EnumToBrushConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the brush to use when the bound enum value matches the specified converter parameter.
    /// </summary>
    public IBrush Match { get; set; } = Brushes.White;

    /// <summary>
    /// Gets or sets the brush to use when the bound enum value does not match the specified converter parameter.
    /// </summary>
    public IBrush NoMatch { get; set; } = Brushes.Black;

    /// <summary>
    /// Converts an enum value to a brush based on whether it matches the provided parameter string.
    /// </summary>
    /// <param name="value">The enum value to evaluate.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The string representation of the enum value to match against.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>The <see cref="Match"/> brush if the enum value matches the parameter; otherwise, <see cref="NoMatch"/>.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return NoMatch;

        return value.Equals(parameter) ? Match : NoMatch;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
