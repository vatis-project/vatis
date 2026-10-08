// <copyright file="SetAtisLetterMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Represents a message sent over the websocket to change the ATIS letter of a station.
/// </summary>
public class SetAtisLetterMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "setAtisLetter";

    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    [JsonPropertyName("value")]
    public SetAtisLetterMessagePayload? Payload { get; set; }

    /// <summary>
    /// Represents the message payload.
    /// </summary>
    public class SetAtisLetterMessagePayload
    {
        /// <summary>
        /// Gets or sets the unique station ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the station identifier.
        /// </summary>
        [JsonPropertyName("station")]
        public string? Station { get; set; }

        /// <summary>
        /// Gets or sets the station ATIS type. Defaults to <c>Combined</c>.
        /// </summary>
        [JsonPropertyName("atisType")]
        [JsonConverter(typeof(JsonStringEnumConverter<AtisType>))]
        public AtisType AtisType { get; set; } = AtisType.Combined;

        /// <summary>
        /// Gets or sets the letter to set.
        /// </summary>
        [JsonPropertyName("letter")]
        public string? Letter { get; set; }

        /// <summary>
        /// Gets or sets the direction to move the letter in, as an alternative to <see cref="Letter"/>.
        /// </summary>
        [JsonPropertyName("step")]
        [JsonConverter(typeof(JsonStringEnumConverter<AtisLetterStep>))]
        public AtisLetterStep? Step { get; set; }

        /// <summary>
        /// Checks that the payload names one station and one change.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the payload is not a valid request.</exception>
        public void Validate()
        {
            if (string.IsNullOrEmpty(Id) == string.IsNullOrEmpty(Station))
                throw new ArgumentException("Provide either id or station.");

            if (Letter is null == Step is null)
                throw new ArgumentException("Provide either letter or step.");

            if (Letter is not null && (Letter.Length != 1 || !char.IsAsciiLetter(Letter[0])))
                throw new ArgumentException($"Invalid letter: {Letter}");
        }

        /// <summary>
        /// Works out the letter a station moves to from its current one.
        /// </summary>
        /// <param name="current">The station's current letter.</param>
        /// <param name="range">The station's code range.</param>
        /// <returns>The new letter.</returns>
        /// <exception cref="ArgumentException">Thrown if the requested letter is outside the code range.</exception>
        public char Resolve(char current, CodeRangeMeta range)
        {
            if (Letter is not null)
            {
                var letter = char.ToUpperInvariant(Letter[0]);
                if (letter < range.Low || letter > range.High)
                {
                    throw new ArgumentException(
                        $"Letter {letter} is outside the code range {range.Low}-{range.High}.");
                }

                return letter;
            }

            // Steps wrap around the code range, as the letter button in the station view does.
            // A letter left outside the range by a profile change starts again from its edge.
            return Step switch
            {
                AtisLetterStep.Next => current < range.Low || current >= range.High
                    ? range.Low
                    : (char)(current + 1),
                AtisLetterStep.Previous => current > range.High || current <= range.Low
                    ? range.High
                    : (char)(current - 1),
                _ => throw new ArgumentException("Provide either letter or step.")
            };
        }
    }
}
