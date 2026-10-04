// <copyright file="WavPackClipEntry.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// An editable row in the voice pack manifest: a spoken phrase and the WAV file played for it.
/// </summary>
public class WavPackClipEntry : ReactiveObject
{
    private string _key = string.Empty;
    private string _file = string.Empty;

    /// <summary>
    /// Gets or sets the spoken word or phrase.
    /// </summary>
    public string Key
    {
        get => _key;
        set => this.RaiseAndSetIfChanged(ref _key, value);
    }

    /// <summary>
    /// Gets or sets the WAV file path, relative to the pack folder.
    /// </summary>
    public string File
    {
        get => _file;
        set => this.RaiseAndSetIfChanged(ref _file, value);
    }
}
