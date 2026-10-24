// <copyright file="CompletionSuggestionResult.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// The suggestions available at a position, and where the word being replaced starts.
/// </summary>
/// <param name="StartOffset">The offset at which the partly typed word starts.</param>
/// <param name="Items">The suggestions that start with what has been typed so far.</param>
public sealed record CompletionSuggestionResult(int StartOffset, IReadOnlyList<CompletionSuggestion> Items);
