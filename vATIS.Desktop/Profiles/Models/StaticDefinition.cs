// <copyright file="StaticDefinition.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a static definition with text, ordinal, and enabled status.
/// </summary>
public class StaticDefinition : ReactiveObject
{
    private bool _enabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticDefinition"/> class.
    /// </summary>
    /// <param name="text">The text of the static definition.</param>
    /// <param name="ordinal">The ordinal position of the static definition.</param>
    /// <param name="enabled">A value indicating whether the static definition is enabled.</param>
    public StaticDefinition(string text, int ordinal, bool enabled = true)
    {
        Text = text;
        Ordinal = ordinal;
        Enabled = enabled;
    }

    /// <summary>
    /// Gets or sets the text of the static definition.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of the static definition.
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the static definition is enabled.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => this.RaiseAndSetIfChanged(ref _enabled, value);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return Text;
    }

    /// <summary>
    /// Creates a new instance of <see cref="StaticDefinition"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="StaticDefinition"/> instance that is a copy of this instance.</returns>
    public StaticDefinition Clone()
    {
        return new StaticDefinition(Text, Ordinal, Enabled);
    }
}
