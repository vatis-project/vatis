// <copyright file="ReleaseNoteSection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// A titled group of release note entries, such as "Added" or "Fixed".
/// </summary>
/// <param name="Title">The section title.</param>
/// <param name="Items">The entries in the section, as text that may contain backticked code.</param>
public record ReleaseNoteSection(string Title, IReadOnlyList<string> Items);
