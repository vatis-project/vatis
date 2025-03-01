// <copyright file="ReleaseNotesDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog that displays the release notes for the installed version.
/// </summary>
public partial class ReleaseNotesDialog : ReactiveWindow<ReleaseNotesDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseNotesDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with the view.</param>
    public ReleaseNotesDialog(ReleaseNotesDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseNotesDialog"/> class.
    /// </summary>
    public ReleaseNotesDialog()
    {
        InitializeComponent();
    }

    private void CloseWindow(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Ok);
    }
}
