// <copyright file="AtisStationView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Components;

/// <summary>
/// Represents a view for the ATIS station, providing user interaction and data display for the associated
/// <see cref="AtisStationViewModel"/>.
/// </summary>
public partial class AtisStationView : ReactiveUserControl<AtisStationViewModel>
{
    private bool airportConditionsInitialized;
    private bool notamsInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisStationView"/> class.
    /// </summary>
    public AtisStationView()
    {
        this.InitializeComponent();

        this.TypeAtisLetter.KeyDown += this.TypeAtisLetterOnKeyDown;
        this.AtisLetter.DoubleTapped += this.AtisLetterOnDoubleTapped;
        this.AtisLetter.Tapped += this.AtisLetterOnTapped;
        this.AtisLetter.PointerPressed += this.AtisLetterOnPointerPressed;

        this.Loaded += this.OnLoaded;
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        this.ViewModel.WhenAnyValue(x => x.IsAtisLetterInputMode).Subscribe(
            mode =>
            {
                if (mode)
                {
                    this.TypeAtisLetter.Text = string.Empty;
                    this.TypeAtisLetter.Focus();
                    this.TypeAtisLetter.SelectAll();
                }
            });

        if (this.ViewModel != null)
        {
            this.AirportConditions.TextArea.ReadOnlySectionProvider =
                new TextSegmentReadOnlySectionProvider<TextSegment>(this.ViewModel.ReadOnlyAirportConditions);
            this.NotamFreeText.TextArea.ReadOnlySectionProvider =
                new TextSegmentReadOnlySectionProvider<TextSegment>(this.ViewModel.ReadOnlyNotams);
            this.AirportConditions.TextArea.TextView.LineTransformers.Add(
                new ReadOnlySegmentTransformer(this.ViewModel.ReadOnlyAirportConditions));
            this.NotamFreeText.TextArea.TextView.LineTransformers.Add(
                new ReadOnlySegmentTransformer(this.ViewModel.ReadOnlyNotams));
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (this.ViewModel == null)
        {
            return;
        }

        this.NotamFreeText.Options.AllowScrollBelowDocument = false;
        this.NotamFreeText.TextArea.Caret.PositionChanged += (_, _) =>
        {
            foreach (var segment in this.ViewModel.ReadOnlyNotams)
            {
                // If caret is within or at the start of a read-only segment
                if (this.NotamFreeText.CaretOffset >= segment.StartOffset &&
                    this.NotamFreeText.CaretOffset <= segment.EndOffset)
                {
                    // Move caret to the end of the read-only segment
                    this.NotamFreeText.CaretOffset = segment.EndOffset;
                    break;
                }
            }
        };

        this.AirportConditions.Options.AllowScrollBelowDocument = false;
        this.AirportConditions.TextArea.Caret.PositionChanged += (_, _) =>
        {
            foreach (var segment in this.ViewModel.ReadOnlyAirportConditions)
            {
                // If caret is within or at the start of a read-only segment
                if (this.AirportConditions.CaretOffset >= segment.StartOffset &&
                    this.AirportConditions.CaretOffset <= segment.EndOffset)
                {
                    // Move caret to the end of the read-only segment
                    this.AirportConditions.CaretOffset = segment.EndOffset;
                    break;
                }
            }
        };
    }

    private async void AtisLetterOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (this.ViewModel == null)
            {
                return;
            }

            if (this.ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                return;
            }

            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                this.ViewModel.IsAtisLetterInputMode = true;
            }

            // If the shift key wasn't held down during a double tap it is likely
            // the user is just tapping/clicking really fast to advance the letter
            // so treat it like a single tap and increment.
            else
            {
                await this.ViewModel.AcknowledgeOrIncrementAtisLetterCommand.Execute();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in AtisLetterOnDoubleTapped");
        }
    }

    private async void AtisLetterOnTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (this.ViewModel == null)
            {
                return;
            }

            if (this.ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                return;
            }

            // If the shift key is held down it likely means the user is trying to
            // shift + double click to get into edit mode, so don't advance
            // the letter.
            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                return;
            }

            await this.ViewModel.AcknowledgeOrIncrementAtisLetterCommand.Execute();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in AtisLetterOnClick");
        }
    }

    private async void AtisLetterOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            if (this.ViewModel == null)
            {
                return;
            }

            if (this.ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                return;
            }

            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsRightButtonPressed)
            {
                await this.ViewModel.DecrementAtisLetterCommand.Execute();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in AtisLetterOnPointerPressed");
        }
    }

    private void TypeAtisLetterOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (this.ViewModel == null)
        {
            return;
        }

        if (this.ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            this.ViewModel.IsAtisLetterInputMode = false;
            this.TypeAtisLetter.Text = $"{this.ViewModel.AtisLetter}";
        }
        else if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            this.ViewModel.IsAtisLetterInputMode = false;
            if (char.TryParse(this.TypeAtisLetter.Text?.ToUpperInvariant(), out var letter))
            {
                if (letter > this.ViewModel.CodeRange.High)
                {
                    return;
                }

                if (letter < this.ViewModel.CodeRange.Low)
                {
                    return;
                }

                this.ViewModel.AtisLetter = letter;
            }
        }
    }

    private void AirportConditions_OnTextChanged(object? sender, EventArgs e)
    {
        if (this.ViewModel?.SelectedAtisPreset == null)
        {
            return;
        }

        if (!this.AirportConditions.TextArea.IsFocused)
        {
            return;
        }

        if (this.airportConditionsInitialized)
        {
            this.ViewModel.HasUnsavedAirportConditions = true;
        }

        this.airportConditionsInitialized = true;
    }

    private void NotamFreeText_OnTextChanged(object? sender, EventArgs e)
    {
        if (this.ViewModel?.SelectedAtisPreset == null)
        {
            return;
        }

        if (!this.NotamFreeText.TextArea.IsFocused)
        {
            return;
        }

        if (this.notamsInitialized)
        {
            this.ViewModel.HasUnsavedNotams = true;
        }

        this.notamsInitialized = true;
    }

    private class ReadOnlySegmentTransformer : DocumentColorizingTransformer
    {
        private readonly TextSegmentCollection<TextSegment> readOnlySegments;

        public ReadOnlySegmentTransformer(TextSegmentCollection<TextSegment> readOnlySegments)
        {
            this.readOnlySegments = readOnlySegments;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            foreach (var segment in this.readOnlySegments.FindOverlappingSegments(line.Offset, line.Length))
            {
                this.ChangeLinePart(
                    Math.Max(segment.StartOffset, line.Offset),
                    Math.Min(segment.EndOffset, line.Offset + line.Length),
                    element => { element.TextRunProperties.SetForegroundBrush(Brushes.Aqua); });
            }
        }
    }
}
