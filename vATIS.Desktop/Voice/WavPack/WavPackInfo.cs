// <copyright file="WavPackInfo.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// A voice pack stored in vATIS's voice pack library.
/// </summary>
/// <param name="Id">The pack id, which is also the name of its folder in the library. Null for a pack outside the library.</param>
/// <param name="Folder">The pack folder.</param>
/// <param name="Name">The pack name.</param>
/// <param name="ClipCount">The number of clips in the manifest.</param>
/// <param name="DisplayName">The name shown in lists; includes the id when another pack has the same name.</param>
public record WavPackInfo(string? Id, string Folder, string Name, int ClipCount, string DisplayName);
