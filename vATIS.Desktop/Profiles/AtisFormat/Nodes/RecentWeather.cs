// <copyright file="RecentWeather.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the recent weather component of the ATIS format.
/// </summary>
public class RecentWeather : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecentWeather"/> class.
    /// </summary>
    public RecentWeather()
    {
        Template = new Template
        {
            Text = "RECENT WEATHER {weather}",
            Voice = "RECENT WEATHER {weather}",
        };
    }

    /// <summary>
    /// Creates a new instance of <see cref="RecentWeather"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="RecentWeather"/> instance that is a copy of this instance.</returns>
    public RecentWeather Clone()
    {
        return (RecentWeather)MemberwiseClone();
    }
}
