// <copyright file="CompletionSuggestion.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// A suggestion for the next piece of a station or weather condition.
/// </summary>
/// <param name="Text">The text shown in the list.</param>
/// <param name="InsertText">The text inserted when the suggestion is accepted.</param>
/// <param name="Description">A short explanation shown beside the text.</param>
/// <param name="ContinueAfter">Whether suggestions for the next piece should follow once it is inserted.</param>
public sealed record CompletionSuggestion(string Text, string InsertText, string Description, bool ContinueAfter = true);
