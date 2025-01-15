// <copyright file="ConvectiveCloudTypeMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata information for a convective cloud type, including a key and its corresponding value.
/// </summary>
public record ConvectiveCloudTypeMeta(string Key, string Value) : ReactiveRecord;
