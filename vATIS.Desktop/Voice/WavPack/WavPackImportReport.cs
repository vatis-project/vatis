// <copyright file="WavPackImportReport.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// The outcome of a legacy ATIS.txt import.
/// </summary>
/// <param name="ClipCount">Number of manifest entries written.</param>
/// <param name="Skipped">Legacy entries that could not be converted, with reasons.</param>
/// <param name="Duplicates">Manifest keys defined more than once (the first definition is kept).</param>
public record WavPackImportReport(int ClipCount, IReadOnlyList<string> Skipped, IReadOnlyList<string> Duplicates);
