// <copyright file="StaticConverters.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Vatsim.Vatis.Ui.Controls.GroupBox;

/// <summary>
/// Provides a collection of static converters for various data transformations
/// involving Avalonia types such as <see cref="Avalonia.Rect"/>, <see cref="Avalonia.Media.CombinedGeometry"/>,
/// <see cref="Avalonia.Thickness"/>, and <see cref="Avalonia.CornerRadius"/>.
/// </summary>
public static class StaticConverters
{
    /// <summary>
    /// Gets a mechanism to generate a <see cref="Avalonia.Media.CombinedGeometry"/>
    /// by combining multiple <see cref="Avalonia.Rect"/> instances.
    /// This converter is typically used to compute an outer border that excludes specific internal areas,
    /// such as a header or other margin-defined regions.
    /// </summary>
    public static FuncMultiValueConverter<Rect?, CombinedGeometry> ConvertBoundsToOuterBorder { get; } =
        new(
            rects =>
            {
                var arr = rects.Cast<Rect>().ToList() ?? throw new NullReferenceException();
                return arr.Count < 3
                    ? throw new ArgumentOutOfRangeException(nameof(rects), @"Amount of given Bounds was less than 3")
                    : new CombinedGeometry(
                        GeometryCombineMode.Exclude,
                        new RectangleGeometry(arr[0]),
                        new RectangleGeometry(arr[1]),
                        new TranslateTransform(-arr[2].Left, -arr[2].Top));
            });

    /// <summary>
    /// Gets a converter that transforms an <see cref="Avalonia.Thickness"/> into an <see cref="Avalonia.Rect"/>.
    /// This converter is typically used to calculate a bounding rectangle based on thickness values,
    /// such as margins or paddings, and apply adjustments to visual elements.
    /// </summary>
    public static FuncValueConverter<Thickness?, Rect> ThicknessRectConverter { get; } =
        new(
            thickness =>
            {
                if (thickness is { } thick)
                {
                    return new Rect(thick.Left, thick.Top, thick.Right - thick.Left, thick.Top - thick.Bottom);
                }

                throw new InvalidCastException();
            });

    /// <summary>
    /// Gets a converter that determines the <see cref="Avalonia.CornerRadius"/>
    /// based on the specified <see cref="OperatingSystem"/> and its version.
    /// This converter is typically used to apply platform-specific UI corner radius
    /// styling, such as adapting to specific Windows versions or build thresholds.
    /// </summary>
    public static FuncValueConverter<OperatingSystem?, CornerRadius> OsBorderCornerRadius { get; } = new(
        osVersion =>
            new CornerRadius(
                osVersion?.Platform switch
                {
                    PlatformID.Win32NT => osVersion.Version.Major switch
                    {
                        /* check radius via Windows' presentationsettings.exe */

                        // XP and older. XP may have used 3, but older ones were probably 0.
                        5 => 0,
                        6 => osVersion.Version.Minor switch
                        {
                            // Vista, 7 are 3
                            0 or 1 => 3,

                            // 8, 8.1 are...unknown
                            _ => 0,
                        },

                        // 10 and later. My copies of Win11 use 0.
                        10 => osVersion.Version.Build switch
                        {
                            var win11 when win11 >= 22000 => 0,
                            _ => 0,
                        },
                        _ => 0,
                    },
                    _ => 0,
                }));

    /// <summary>
    /// Gets a converter that transforms multiple double values or objects, typically from bindings,
    /// into an <see cref="Avalonia.Thickness"/> value.
    /// The resulting <see cref="Avalonia.Thickness"/> is constructed based on the provided
    /// values for left, top, right, and bottom padding or margins.
    /// </summary>
    public static FuncMultiValueConverter<object?, Thickness> ConvertDoublesToThickness { get; } = new(
        src =>
        {
            List<double> doubles = new();
            foreach (var nd in src)
            {
                if (nd is double d)
                {
                    doubles.Add(d);
                }
                else
                {
                    throw new InvalidCastException();
                }
            }

            return doubles.Count switch
            {
                1 => new Thickness(doubles[0]),
                2 => new Thickness(doubles[0], doubles[1]),
                4 => new Thickness(doubles[0], doubles[1], doubles[2], doubles[3]),
                _ => throw new IndexOutOfRangeException(
                    "Source data must be doubles. Thickness' constructors only allow for 1, 2, or 4 arguments"),
            };
        });

    /// <summary>
    /// Gets a value converter that calculates half of the height of a given <see cref="Avalonia.Rect"/> instance.
    /// The converter retrieves the height of the provided <see cref="Avalonia.Rect"/> and returns its value divided by two.
    /// An exception is thrown if the input <see cref="Avalonia.Rect"/> is null or invalid.
    /// </summary>
    public static FuncValueConverter<Rect?, double> BoundsToHeaderHeightDividedByTwo { get; } = new(
        rect =>
            rect.HasValue ? rect.Value.Height / 2 : throw new InvalidCastException());
}
