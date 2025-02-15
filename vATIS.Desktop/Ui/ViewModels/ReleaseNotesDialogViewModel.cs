// <copyright file="ReleaseNotesDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the <see cref="ReleaseNotesDialog"/>.
/// </summary>
public class ReleaseNotesDialogViewModel : ReactiveViewModelBase
{
    private string? _releaseNotes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseNotesDialogViewModel"/> class.
    /// </summary>
    public ReleaseNotesDialogViewModel()
    {
    }

    /// <summary>
    /// Gets or sets the release notes Markdown text.
    /// </summary>
    public string? ReleaseNotes
    {
        get => _releaseNotes;
        set => this.RaiseAndSetIfChanged(ref _releaseNotes, value);
    }
}
