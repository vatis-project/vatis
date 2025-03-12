// <copyright file="AtisPresetMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a class that returns a list of available presets.
/// </summary>
public class AtisPresetMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "atisPresets";

    /// <summary>
    /// Gets or sets a list of ATIS preset names.
    /// </summary>
    [JsonPropertyName("presets")]
    public List<string>? Presets { get; set; }
}
