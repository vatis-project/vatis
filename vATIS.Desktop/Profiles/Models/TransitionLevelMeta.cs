// <copyright file="TransitionLevelMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata for a transition level, including the lower level, upper level, and altitude.
/// </summary>
public record TransitionLevelMeta(int Low, int High, int Altitude) : ReactiveRecord;
