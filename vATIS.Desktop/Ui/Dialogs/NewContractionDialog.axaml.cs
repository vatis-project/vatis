// <copyright file="NewContractionDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.Xaml.Interactivity;
using Slugify;
using Vatsim.Vatis.Ui.Behaviors;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog for creating a new contraction.
/// </summary>
public partial class NewContractionDialog : ReactiveWindow<NewContractionDialogViewModel>, ICloseable
{
    private static readonly SlugHelper s_slug = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NewContractionDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The ViewModel used to manage the dialog.</param>
    public NewContractionDialog(NewContractionDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
        AddUppercaseHandlers();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewContractionDialog"/> class.
    /// </summary>
    public NewContractionDialog()
    {
        InitializeComponent();
        AddUppercaseHandlers();
    }

    // Text and Voice are upper-cased as typed, except for regex contractions where the case is significant.
    private void AddUppercaseHandlers()
    {
        foreach (var name in new[] { "String", "Spoken" })
        {
            this.FindControl<TextBox>(name)?.AddHandler(
                InputElement.TextInputEvent,
                (_, e) =>
                {
                    if ((ViewModel ?? DataContext as NewContractionDialogViewModel) is not { IsRegex: true })
                    {
                        e.Text = e.Text?.ToUpperInvariant();
                    }
                },
                RoutingStrategies.Tunnel);

            if (this.FindControl<TextBox>(name) is { } textBox)
            {
                Interaction.GetBehaviors(textBox).Add(
                    new TextBoxFunctionCompletionBehavior { IsApplicable = () => (ViewModel ?? DataContext as NewContractionDialogViewModel) is { IsRegex: true } });
            }
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private void Variable_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox textBox)
        {
            textBox.Text = s_slug.GenerateSlug(textBox.Text ?? string.Empty).ToUpperInvariant().Replace("-", "_");
        }
    }
}
