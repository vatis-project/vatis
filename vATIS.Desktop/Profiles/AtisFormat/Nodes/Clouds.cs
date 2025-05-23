// <copyright file="Clouds.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Converter;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the clouds component of the ATIS format.
/// </summary>
public class Clouds : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Clouds"/> class.
    /// </summary>
    public Clouds()
    {
        Template = new Template { Text = "{clouds}", Voice = "{clouds}", };
    }

    /// <summary>
    /// Gets or sets a value indicating whether to identify the ceiling layer.
    /// </summary>
    public bool IdentifyCeilingLayer { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to identify the ceiling layer in the text ATIS.
    /// </summary>
    public bool IdentifyCeilingLayerTextAtis { get; set; }

    /// <summary>
    /// Gets or sets the prefix used to identify the ceiling layer in the text ATIS.
    /// </summary>
    public string? TextAtisCeilingPrefix { get; set; } = "CIG";

    /// <summary>
    /// Gets or sets the cloud type used to determine the ceiling.
    /// </summary>
    public List<string> CloudCeilingLayerTypes { get; set; } = ["BKN", "OVC"];

    /// <summary>
    /// Gets or sets a value indicating whether to convert cloud heights to metric units.
    /// </summary>
    public bool ConvertToMetric { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the altitude is in hundreds.
    /// </summary>
    public bool IsAltitudeInHundreds { get; set; }

    /// <summary>
    /// Gets or sets the undetermined layer altitude.
    /// </summary>
    public UndeterminedLayer UndeterminedLayerAltitude { get; set; } = new("undetermined", "undetermined");

    /// <summary>
    /// Gets or sets the automatic CB detection text and voice ATIS values.
    /// </summary>
    public AutomaticCbDetection AutomaticCbDetection { get; set; } = new("//////CB", "RADAR DETECTED C-B CLOUDS");

    /// <summary>
    /// Gets or sets the dictionary of cloud types.
    /// </summary>
    [JsonConverter(typeof(CloudTypeConverter))]
    public Dictionary<string, CloudType> Types { get; set; } = new()
    {
        { "FEW", new CloudType("FEW{altitude}", "few clouds at {altitude}") },
        { "SCT", new CloudType("SCT{altitude}{convective}", "{altitude} scattered {convective}") },
        { "BKN", new CloudType("BKN{altitude}{convective}", "{altitude} broken {convective}") },
        { "OVC", new CloudType("OVC{altitude}{convective}", "{altitude} overcast {convective}") },
        { "VV", new CloudType("VV{altitude}", "indefinite ceiling {altitude}") },
        { "NSC", new CloudType("NSC", "no significant clouds") },
        { "NCD", new CloudType("NCD", "no clouds detected") },
        { "CLR", new CloudType("CLR", "sky clear below one-two thousand") },
        { "SKC", new CloudType("SKC", "sky clear") },
    };

    /// <summary>
    /// Gets or sets the dictionary of convective cloud types.
    /// </summary>
    public Dictionary<string, string> ConvectiveTypes { get; set; } = new()
    {
        { "CB", "cumulonimbus" },
        { "TCU", "towering cumulus" },
    };

    /// <summary>
    /// Creates a new instance of <see cref="Clouds"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Clouds"/> instance that is a copy of this instance.</returns>
    public Clouds Clone()
    {
        return (Clouds)MemberwiseClone();
    }
}
