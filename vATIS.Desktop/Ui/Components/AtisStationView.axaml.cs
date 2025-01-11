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

public partial class AtisStationView : ReactiveUserControl<AtisStationViewModel>
{
    private bool _airportConditionsInitialized;
    private bool _notamsInitialized;

    public AtisStationView()
    {
        InitializeComponent();

        TypeAtisLetter.KeyDown += TypeAtisLetterOnKeyDown;
        AtisLetter.DoubleTapped += AtisLetterOnDoubleTapped;
        AtisLetter.Tapped += AtisLetterOnTapped;
        AtisLetter.PointerPressed += AtisLetterOnPointerPressed;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        NotamFreeText.Options.AllowScrollBelowDocument = false;
        NotamFreeText.TextArea.Caret.PositionChanged += (_, _) =>
        {
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
        
        AirportConditions.Options.AllowScrollBelowDocument = false;
        AirportConditions.TextArea.Caret.PositionChanged += (_, _) =>
        {
            foreach (var segment in ViewModel.ReadOnlyAirportConditions)
            {
                // If caret is within or at the start of a read-only segment
                if (AirportConditions.CaretOffset >= segment.StartOffset && AirportConditions.CaretOffset <= segment.EndOffset)
                {
                    // Move caret to the end of the read-only segment
                    AirportConditions.CaretOffset = segment.EndOffset;
                    break;
                }
            }
        };
    }

    private async void AtisLetterOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

            if (ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                return;

            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                ViewModel.IsAtisLetterInputMode = true;
            }
            // If the shift key wasn't held down during a double tap it is likely
            // the user is just tapping/clicking really fast to advance the letter
            // so treat it like a single tap and increment.
            else
            {
                await ViewModel.AcknowledgeOrIncrementAtisLetterCommand.Execute();
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
            if (ViewModel == null)
                return;

            if (ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                return;

            // If the shift key is held down it likely means the user is trying to
            // shift + double click to get into edit mode, so don't advance
            // the letter.
            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                return;
            }

            await ViewModel.AcknowledgeOrIncrementAtisLetterCommand.Execute();
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
            if (ViewModel == null)
                return;

            if (ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                return;

            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsRightButtonPressed)
            {
                await ViewModel.DecrementAtisLetterCommand.Execute();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in AtisLetterOnPointerPressed");
        }
    }

    private void TypeAtisLetterOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null)
            return;

        if (ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            return;

        if (e.Key == Key.Escape)
        {
            ViewModel.IsAtisLetterInputMode = false;
            TypeAtisLetter.Text = $"{ViewModel.AtisLetter}";
        }
        else if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            ViewModel.IsAtisLetterInputMode = false;
            if (char.TryParse(TypeAtisLetter.Text?.ToUpperInvariant(), out var letter))
            {
                if (letter > ViewModel.CodeRange.High)
                {
                    return;
                }
                if (letter < ViewModel.CodeRange.Low)
                {
                    return;
                }
                ViewModel.AtisLetter = letter;
            }
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        ViewModel.WhenAnyValue(x => x.IsAtisLetterInputMode).Subscribe(mode =>
        {
            if (mode)
            {
                TypeAtisLetter.Text = "";
                TypeAtisLetter.Focus();
                TypeAtisLetter.SelectAll();
            }
        });

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
    
    private void AirportConditions_OnTextChanged(object? sender, EventArgs e)
    {
        if (ViewModel?.SelectedAtisPreset == null)
            return;

        if (!AirportConditions.TextArea.IsFocused)
            return;

        if (_airportConditionsInitialized)
        {
            ViewModel.HasUnsavedAirportConditions = true;
        }

        _airportConditionsInitialized = true;
    }

    private void NotamFreeText_OnTextChanged(object? sender, EventArgs e)
    {
        if (ViewModel?.SelectedAtisPreset == null)
            return;

        if (!NotamFreeText.TextArea.IsFocused)
            return;

        if (_notamsInitialized)
        {
            ViewModel.HasUnsavedNotams = true;
        }

        _notamsInitialized = true;
    }
    
    class ReadOnlySegmentTransformer : DocumentColorizingTransformer
    {
        private readonly TextSegmentCollection<TextSegment> _readOnlySegments;

        public ReadOnlySegmentTransformer(TextSegmentCollection<TextSegment> readOnlySegments)
        {
            _readOnlySegments = readOnlySegments;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            foreach (var segment in _readOnlySegments.FindOverlappingSegments(line.Offset, line.Length))
            {
                ChangeLinePart(
                    Math.Max(segment.StartOffset, line.Offset),
                    Math.Min(segment.EndOffset, line.Offset + line.Length),
                    element =>
                    {
                        element.TextRunProperties.SetForegroundBrush(Brushes.Aqua);
                    }
                );
            }
        }
    }
}
