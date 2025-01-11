// <copyright file="BitmapAssetValueConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Vatsim.Vatis.Ui.Extensions;

/// <summary>
/// Provides a value converter that converts a string representing an asset URI into a <see cref="Avalonia.Media.Imaging.Bitmap"/>.
/// </summary>
public class BitmapAssetValueConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string rawUri && targetType.IsAssignableFrom(typeof(Bitmap)))
        {
            if (string.IsNullOrEmpty(rawUri))
            {
                return null;
            }

            return new Bitmap(AssetLoader.Open(new Uri(rawUri)));
        }

        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
