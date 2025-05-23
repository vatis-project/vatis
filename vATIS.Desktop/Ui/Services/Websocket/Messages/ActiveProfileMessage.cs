// <copyright file="ActiveProfileMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a websocket message that returns the active profile.
/// </summary>
public class ActiveProfileMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "activeProfile";

    /// <summary>
    /// Gets or sets the ID of the active profile.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the active profile.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
