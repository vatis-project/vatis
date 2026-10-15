// <copyright file="RunwayVisualRange.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the runway visual range component of the ATIS format.
/// </summary>
public class RunwayVisualRange : BaseFormat
{
    /// <summary>
    /// Gets or sets the spoken text used to announce runway visual range.
    /// </summary>
    public string? SpokenText { get; set; } = "R-V-R";

    /// <summary>
    /// Gets or sets the neutral tendency.
    /// </summary>
    public string? NeutralTendency { get; set; } = "Neutral";

    /// <summary>
    /// Gets or sets the going up tendency.
    /// </summary>
    public string? GoingUpTendency { get; set; } = "Going Up";

    /// <summary>
    /// Gets or sets the going down tendency.
    /// </summary>
    public string? GoingDownTendency { get; set; } = "Going Down";

    /// <summary>
    /// Gets or sets the text ATIS output for each tendency, keyed by METAR tendency code (N, U, D).
    /// </summary>
    public Dictionary<string, string> TendencyText { get; set; } = new()
    {
        { "N", "N" },
        { "U", "U" },
        { "D", "D" },
    };

    /// <summary>
    /// Gets or sets the spoken phrases used to read the runway and RVR values. Placeholders: {value}, {min}, {max}.
    /// </summary>
    public Dictionary<string, string> VoicePhrases { get; set; } = DefaultVoicePhrases();

    /// <summary>
    /// Creates the default spoken phrases, in display order.
    /// </summary>
    /// <returns>The default phrase dictionary.</returns>
    public static Dictionary<string, string> DefaultVoicePhrases() => new()
    {
        { "Less than", "less than {value}" },
        { "More than", "more than {value}" },
        { "Variable", "variable between {min} and {max}" },
        { "Variable, less than", "variable from less than {min} to {max}" },
        { "Variable to greater than", "{min} variable to greater than {max}" },
        { "Variable, less than to greater than", "variable from less than {min} to greater than {max}" },
    };

    /// <summary>
    /// Gets the spoken phrase for the given key, falling back to the default.
    /// </summary>
    /// <param name="key">The phrase key.</param>
    /// <returns>The configured phrase, or the default if it is missing or null.</returns>
    public string GetVoicePhrase(string key)
    {
        return VoicePhrases.TryGetValue(key, out var value) && value != null
            ? value
            : DefaultVoicePhrases().GetValueOrDefault(key, string.Empty);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RunwayVisualRange"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="RunwayVisualRange"/> instance that is a copy of this instance.</returns>
    public RunwayVisualRange Clone()
    {
        return (RunwayVisualRange)MemberwiseClone();
    }
}
