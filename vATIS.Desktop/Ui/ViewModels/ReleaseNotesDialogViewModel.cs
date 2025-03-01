// <copyright file="ReleaseNotesDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the <see cref="ReleaseNotesDialog"/>.
/// </summary>
public class ReleaseNotesDialogViewModel : ReactiveViewModelBase
{
    private string? _releaseNotes;
    private bool _suppressReleaseNotes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseNotesDialogViewModel"/> class.
    /// </summary>
    public ReleaseNotesDialogViewModel()
    {
        CloseWindowCommand = ReactiveCommand.Create<ICloseable>(HandleCloseWindow);
    }

    /// <summary>
    /// Gets the command that closes the current window or dialog.
    /// </summary>
    /// <value>
    /// A reactive command that takes an <see cref="ICloseable"/> instance as a parameter and performs the close operation.
    /// </value>
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    /// <summary>
    /// Gets or sets the release notes Markdown text.
    /// </summary>
    public string? ReleaseNotes
    {
        get => _releaseNotes;
        set => this.RaiseAndSetIfChanged(ref _releaseNotes, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the release notes window should be shown.
    /// When set to true, the release notes window will not be shown again.
    /// </summary>
    public bool SuppressReleaseNotes
    {
        get => _suppressReleaseNotes;
        set => this.RaiseAndSetIfChanged(ref _suppressReleaseNotes, value);
    }

    private void HandleCloseWindow(ICloseable window)
    {
        window.Close(DialogResult.Ok);
    }
}
