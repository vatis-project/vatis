// <copyright file="WavPackJsonContext.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Source-generated JSON context for voice pack manifests.
/// </summary>
[JsonSourceGenerationOptions(ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true, WriteIndented = true)]
[JsonSerializable(typeof(WavPackManifest))]
internal partial class WavPackJsonContext : JsonSerializerContext;
