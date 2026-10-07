// <copyright file="ISpokenTextSource.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using AvaloniaEdit.CodeCompletion;

namespace Vatsim.Vatis.Ui.Common;

/// <summary>
/// Provides the contraction and spoken-text data used to annotate text editors.
/// </summary>
public interface ISpokenTextSource
{
    /// <summary>
    /// Gets the contraction variables, where the text is the variable name and the description is the spoken value.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData { get; }

    /// <summary>
    /// Gets the contractions built into the ATIS Hub, mapping an abbreviation to its spoken expansion.
    /// </summary>
    public IReadOnlyDictionary<string, string> BuiltInContractions { get; }

    /// <summary>
    /// Gets the spoken form of a built-in template token.
    /// </summary>
    /// <param name="token">The template token.</param>
    /// <returns>The spoken text, or null if it could not be determined.</returns>
    public string? GetSpokenText(string token);
}
