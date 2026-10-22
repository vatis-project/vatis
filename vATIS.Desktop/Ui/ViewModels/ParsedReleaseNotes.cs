// <copyright file="ParsedReleaseNotes.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// The release notes Markdown split into a version and its sections.
/// </summary>
/// <param name="Version">The version heading without a leading "v", if present.</param>
/// <param name="Sections">The sections in document order.</param>
public record ParsedReleaseNotes(string? Version, IReadOnlyList<ReleaseNoteSection> Sections);
