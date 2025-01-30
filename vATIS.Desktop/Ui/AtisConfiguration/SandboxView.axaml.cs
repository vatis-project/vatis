// <copyright file="SandboxView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Vatsim.Vatis.Ui.Common;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

/// <summary>
/// Represents a view for the sandbox section used within the ATIS configuration UI.
/// </summary>
public partial class SandboxView : ReactiveUserControl<SandboxViewModel>
{
    private bool _airportConditionsInitialized;
    private bool _notamsInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="SandboxView"/> class.
    /// </summary>
    public SandboxView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (ViewModel != null)
        {
            AirportConditions.TextArea.ReadOnlySectionProvider =
                new TextSegmentReadOnlySectionProvider<TextSegment>(ViewModel.ReadOnlyAirportConditions);
            NotamFreeText.TextArea.ReadOnlySectionProvider =
                new TextSegmentReadOnlySectionProvider<TextSegment>(ViewModel.ReadOnlyNotams);
            AirportConditions.TextArea.TextView.LineTransformers.Add(
                new ReadOnlySegmentTransformer(ViewModel.ReadOnlyAirportConditions));
            NotamFreeText.TextArea.TextView.LineTransformers.Add(
                new ReadOnlySegmentTransformer(ViewModel.ReadOnlyNotams));
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        NotamFreeText.TextArea.Caret.PositionChanged += (_, _) =>
        {
            if (ViewModel?.ReadOnlyNotams == null)
                return;

            foreach (var segment in ViewModel.ReadOnlyNotams)
            {
                // If caret is within or at the start of a read-only segment
                if (NotamFreeText.CaretOffset >= segment.StartOffset && NotamFreeText.CaretOffset <= segment.EndOffset)
                {
                    // Move caret to the end of the read-only segment
                    NotamFreeText.CaretOffset = segment.EndOffset;
                    break;
                }
            }
        };

        AirportConditions.TextArea.Caret.PositionChanged += (_, _) =>
        {
            if (ViewModel?.ReadOnlyAirportConditions == null)
                return;

            foreach (var segment in ViewModel.ReadOnlyAirportConditions)
            {
                // If caret is within or at the start of a read-only segment
                if (AirportConditions.CaretOffset >= segment.StartOffset &&
                    AirportConditions.CaretOffset <= segment.EndOffset)
                {
                    // Move caret to the end of the read-only segment
                    AirportConditions.CaretOffset = segment.EndOffset;
                    break;
                }
            }
        };
    }

    private void AirportConditions_OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SandboxViewModel vm)
        {
            if (vm.SelectedPreset == null)
                return;

            if (_airportConditionsInitialized)
            {
                if (!AirportConditions.TextArea.IsFocused)
                    return;

                vm.HasUnsavedAirportConditions = true;
            }

            _airportConditionsInitialized = true;
        }
    }

    private void NotamFreeText_OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SandboxViewModel vm)
        {
            if (vm.SelectedPreset == null)
                return;

            if (_notamsInitialized)
            {
                if (!NotamFreeText.TextArea.IsFocused)
                    return;

                vm.HasUnsavedNotams = true;
            }

            _notamsInitialized = true;
        }
    }
}
