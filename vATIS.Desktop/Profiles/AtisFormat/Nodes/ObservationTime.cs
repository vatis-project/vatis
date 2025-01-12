// <copyright file="ObservationTime.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes.Converter;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the observation time component of the ATIS format.
/// </summary>
public class ObservationTime : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObservationTime"/> class.
    /// </summary>
    public ObservationTime()
    {
        Template = new Template { Text = "{time}Z", Voice = "{time} ZULU {special}", };
    }

    /// <summary>
    /// Gets or sets the standard update times for the observation.
    /// </summary>
    [JsonConverter(typeof(ObservationTimeConverter))]
    public List<int>? StandardUpdateTime { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="ObservationTime"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="ObservationTime"/> instance that is a copy of this instance.</returns>
    public ObservationTime Clone()
    {
        return (ObservationTime)MemberwiseClone();
    }
}
