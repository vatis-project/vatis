// <copyright file="CloudTypeMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata for cloud types in the system, with acronym, spoken, and text values.
/// </summary>
public record CloudTypeMeta(string Acronym, string Spoken, string Text) : ReactiveRecord;
