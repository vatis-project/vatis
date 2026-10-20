// <copyright file="NewPatternDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Xaml.Interactivity;
using Vatsim.Vatis.Ui.Behaviors;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog for creating or editing a pattern.
/// </summary>
public partial class NewPatternDialog : ReactiveWindow<NewPatternDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewPatternDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The ViewModel used to manage the dialog.</param>
    public NewPatternDialog(NewPatternDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
        AddFunctionCompletion();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewPatternDialog"/> class.
    /// </summary>
    public NewPatternDialog()
    {
        InitializeComponent();
        AddFunctionCompletion();
    }

    // Suggests the replacement functions when the user types "$" in the Text or Voice box.
    private void AddFunctionCompletion()
    {
        foreach (var name in new[] { "String", "Spoken" })
        {
            if (this.FindControl<TextBox>(name) is { } textBox)
            {
                Interaction.GetBehaviors(textBox).Add(new TextBoxFunctionCompletionBehavior());
            }
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }
}
