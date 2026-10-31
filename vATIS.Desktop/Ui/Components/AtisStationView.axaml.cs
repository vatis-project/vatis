// <copyright file="AtisStationView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Common;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Components;

/// <summary>
/// Represents a view for the ATIS station, providing user interaction and data display for the associated
/// <see cref="AtisStationViewModel"/>.
/// </summary>
public partial class AtisStationView : ReactiveUserControl<AtisStationViewModel>
{
    // The widest a message is allowed to be in the message menus before it wraps onto another line.
    private const double MaxMessageTextWidth = 420;
    private const double MaxOrderLabelWidth = 430;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisStationView"/> class.
    /// </summary>
    public AtisStationView()
    {
        InitializeComponent();

        // Don't leave empty space below the last line of the free-text boxes.
        AirportConditions.Options.AllowScrollBelowDocument = false;
        NotamText.Options.AllowScrollBelowDocument = false;

        TypeAtisLetter.KeyDown += TypeAtisLetterOnKeyDown;
        MetarInput.KeyDown += MetarInputOnKeyDown;
        MetarInput.LostFocus += MetarInputOnLostFocus;
        MetarInput.TextChanged += MetarInputOnTextChanged;
        AtisLetter.DoubleTapped += AtisLetterOnDoubleTapped;
        AtisLetter.Tapped += AtisLetterOnTapped;
        AtisLetter.PointerPressed += AtisLetterOnPointerPressed;
        AtisLetter.KeyDown += AtisLetterOnKeyDown;

        Loaded += OnLoaded;
    }

    /// <inheritdoc />
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

        ViewModel.WhenAnyValue(x => x.IsMetarInputMode).Subscribe(mode =>
        {
            if (mode)
            {
                MetarInput.Text = ViewModel?.Metar;
                ToolTip.SetTip(MetarInput, null);
                MetarInput.Focus();
                MetarInput.SelectAll();
            }
        });

        if (ViewModel != null)
        {
            if (ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                return;

            // Assign empty read-only section providers before setting new ones
            AirportConditions.TextArea.ReadOnlySectionProvider = new TextSegmentReadOnlySectionProvider<TextSegment>([]);
            NotamText.TextArea.ReadOnlySectionProvider = new TextSegmentReadOnlySectionProvider<TextSegment>([]);

            // Remove previous transformers
            ReadOnlySegmentTransformer.RemoveFrom(AirportConditions.TextArea.TextView);
            ReadOnlySegmentTransformer.RemoveFrom(NotamText.TextArea.TextView);

            // Assign new read-only sections
            AirportConditions.TextArea.ReadOnlySectionProvider =
                new TextSegmentReadOnlySectionProvider<TextSegment>(ViewModel.ReadOnlyAirportConditions);
            NotamText.TextArea.ReadOnlySectionProvider =
                new TextSegmentReadOnlySectionProvider<TextSegment>(ViewModel.ReadOnlyNotams);

            // Add new line transformers
            AirportConditions.TextArea.TextView.LineTransformers.Add(
                new ReadOnlySegmentTransformer(ViewModel.ReadOnlyAirportConditions));
            NotamText.TextArea.TextView.LineTransformers.Add(
                new ReadOnlySegmentTransformer(ViewModel.ReadOnlyNotams));
        }
    }

    private static Button CreateMoveButton(string glyph, bool enabled, Action onClick)
    {
        var button = new Button
        {
            Content = glyph,
            IsEnabled = enabled,
            Focusable = false,
            Width = 28,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = Brushes.Transparent,
            Opacity = enabled ? 1 : 0.25,
        };

        // A fixed width keeps the arrows in the same columns on every row, and the disabled theme would otherwise draw
        // a filled box behind the dimmed arrow.
        button.Resources["ButtonBackgroundDisabled"] = Brushes.Transparent;
        button.Resources["ButtonBorderBrushDisabled"] = Brushes.Transparent;
        button.Click += (_, _) => onClick();
        return button;
    }

    private Control CreateCheckMark(MenuItem item)
    {
        // The same check mark the menu draws for its own check box items.
        var check = new Path { Width = 16, Stretch = Stretch.None, HorizontalAlignment = HorizontalAlignment.Left };
        if (this.TryFindResource("CheckMarkPathData", ActualThemeVariant, out var geometry) && geometry is Geometry data)
        {
            check.Data = data;
        }

        check.Bind(Shape.FillProperty, item.GetObservable(TemplatedControl.ForegroundProperty));
        return check;
    }

    private void AirportConditionsMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        FillMenus(
            AirportConditionsStaticMenu, AirportConditionsConditionalMenu, AirportConditionsOrderMenu, notams: false);
    }

    private void NotamsMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        FillMenus(NotamsStaticMenu, NotamsConditionalMenu, NotamsOrderMenu, notams: true);
    }

    private void FillOrderMenu(MenuItem menu, bool notams)
    {
        if (ViewModel is not { } viewModel)
            return;

        var included = viewModel.GetIncludedDefinitions(notams);
        var items = new List<MenuItem>();
        for (var i = 0; i < included.Count; i++)
        {
            var definition = included[i];
            var up = CreateMoveButton("\u25B2", i > 0, () => MoveAndRefresh(menu, definition, notams, up: true));
            var down = CreateMoveButton(
                "\u25BC", i < included.Count - 1, () => MoveAndRefresh(menu, definition, notams, up: false));

            var label = new TextBlock
            {
                Text = definition.Text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = MaxOrderLabelWidth,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0),
            };

            // The arrows are docked first so they always sit at the right edge. A grid star column can't shrink below
            // the widest wrapped line of the label, which pushed the arrows of a wrapped row out of line.
            var arrows = new StackPanel { Orientation = Orientation.Horizontal, Children = { up, down } };
            DockPanel.SetDock(arrows, Dock.Right);
            var row = new DockPanel { Children = { arrows, label } };

            // The default menu item template reserves room for an icon, shortcut text and a submenu chevron, which
            // leaves a gap beside the arrows. Only the arrows do anything, so the row is the whole item.
            var content = new Border { Padding = new Thickness(12, 5), Child = row };
            items.Add(new MenuItem
            {
                StaysOpenOnClick = true,
                Template = new FuncControlTemplate<MenuItem>((_, _) => new ContentPresenter
                {
                    Content = content,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                }),
            });
        }

        if (items.Count == 0)
        {
            items.Add(new MenuItem { Header = "No messages", IsEnabled = false });
        }

        menu.ItemsSource = items;
    }

    private void MoveAndRefresh(MenuItem menu, StaticDefinition definition, bool notams, bool up)
    {
        ViewModel?.MoveDefinition(definition, notams, up);
        FillOrderMenu(menu, notams);
    }

    private void FillToggleMenu(
        MenuItem menu, MenuItem orderMenu, bool notams, IReadOnlyList<StaticDefinition> definitions,
        Func<StaticDefinition, bool> isChecked, Action<StaticDefinition, bool> setChecked, string emptyText)
    {
        var items = new List<MenuItem>();
        var checks = new List<(MenuItem Item, Control Check, StaticDefinition Definition)>();

        // The check column only shows while something is checked. Once it does, every item gets a check mark slot,
        // so the items line up and the menu does not shift as items are checked.
        void UpdateChecks()
        {
            var anyChecked = checks.Any(x => isChecked(x.Definition));
            foreach (var (item, check, definition) in checks)
            {
                check.Opacity = isChecked(definition) ? 1 : 0;
                item.Icon = anyChecked ? check : null;
            }
        }

        foreach (var definition in definitions)
        {
            var item = new MenuItem
            {

                // A text block instead of a string, so underscores are shown as they are, and a long message wraps
                // onto more lines instead of making the menu wider than the window.
                Header = new TextBlock
                {
                    Text = definition.Text,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = MaxMessageTextWidth,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                StaysOpenOnClick = true,
            };

            item.Click += (_, _) =>
            {
                setChecked(definition, !isChecked(definition));
                UpdateChecks();

                // The message was added to or removed from the ATIS, so the order list changed too.
                FillOrderMenu(orderMenu, notams);
            };
            items.Add(item);
            checks.Add((item, CreateCheckMark(item), definition));
        }

        UpdateChecks();

        if (items.Count == 0)
        {
            items.Add(new MenuItem { Header = emptyText, IsEnabled = false });
        }

        menu.ItemsSource = items;
    }

    private void FillMenus(MenuItem staticMenu, MenuItem conditionalMenu, MenuItem orderMenu, bool notams)
    {
        if (ViewModel is not { } viewModel)
            return;

        var station = viewModel.AtisStation;
        FillToggleMenu(
            staticMenu, orderMenu, notams, viewModel.GetUnconditionalDefinitions(notams),
            x => !station.HiddenDefinitions.Contains(x), (x, on) => viewModel.SetDefinitionHidden(x, !on),
            "No static messages");
        FillToggleMenu(
            conditionalMenu, orderMenu, notams, viewModel.GetConditionalDefinitions(notams),
            x => station.ForcedDefinitions.Contains(x), viewModel.SetDefinitionForced, "No conditional messages");
        FillOrderMenu(orderMenu, notams);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        NotamText.TextChanged += (_, _) => NotamsCaretPosition();
        NotamText.TextArea.Caret.PositionChanged += (_, _) => NotamsCaretPosition();
        NotamText.TextArea.GotFocus += (_, _) => NotamsCaretPosition();

        AirportConditions.TextChanged += (_, _) => AirportConditionsCaretPosition();
        AirportConditions.TextArea.Caret.PositionChanged += (_, _) => AirportConditionsCaretPosition();
        AirportConditions.TextArea.GotFocus += (_, _) => AirportConditionsCaretPosition();
    }

    private void NotamsCaretPosition()
    {
        if (ViewModel?.ReadOnlyNotams == null)
            return;

        foreach (var segment in ViewModel.ReadOnlyNotams)
        {
            // If caret is within or at the start of a read-only segment
            if (NotamText.CaretOffset >= segment.StartOffset && NotamText.CaretOffset <= segment.EndOffset)
            {
                if (ViewModel.AtisStation.NotamsBeforeFreeText)
                {
                    // Move caret to the end of the read-only segment
                    NotamText.CaretOffset = segment.EndOffset;
                }
                else
                {
                    // Move caret to the beginning of the read-only segment
                    NotamText.CaretOffset = segment.StartOffset;
                }

                break;
            }
        }
    }

    private void AirportConditionsCaretPosition()
    {
        if (ViewModel?.ReadOnlyAirportConditions == null)
            return;

        foreach (var segment in ViewModel.ReadOnlyAirportConditions)
        {
            // If caret is within or at the start of a read-only segment
            if (AirportConditions.CaretOffset >= segment.StartOffset && AirportConditions.CaretOffset <= segment.EndOffset)
            {
                if (ViewModel.AtisStation.AirportConditionsBeforeFreeText)
                {
                    // Move caret to the end of the read-only segment
                    AirportConditions.CaretOffset = segment.EndOffset;
                }
                else
                {
                    // Move caret to the beginning of the read-only segment
                    AirportConditions.CaretOffset = segment.StartOffset;
                }

                break;
            }
        }
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

            // If the shift key is held down, it likely means the user is trying to
            // shift + double click to get into edit mode, so don't advance
            // the letter.
            if ((e.KeyModifiers & KeyModifiers.Shift) != 0 &&
                ViewModel.NetworkConnectionStatus != NetworkConnectionStatus.Observer)
            {
                return;
            }

            if (ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            {
                await ViewModel.AcknowledgeAtisUpdateCommand.Execute();
            }
            else
            {
                // Only allow incrementing the ATIS letter for connected station.
                await ViewModel.AcknowledgeOrIncrementAtisLetterCommand.Execute();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in AtisLetterOnClick");
        }
    }

    private async void AtisLetterOnKeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

            var isObserver = ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer;
            var action = AtisLetterKeyboard.GetAction(e.Key, e.KeyModifiers, isObserver);
            if (action == AtisLetterKeyAction.None)
                return;

            e.Handled = true;

            switch (action)
            {
                case AtisLetterKeyAction.AcknowledgeOrIncrement:
                    // The same as a click: an observer can only acknowledge the update.
                    await (isObserver
                        ? ViewModel.AcknowledgeAtisUpdateCommand.Execute()
                        : ViewModel.AcknowledgeOrIncrementAtisLetterCommand.Execute());
                    break;
                case AtisLetterKeyAction.Decrement:
                    await ViewModel.DecrementAtisLetterCommand.Execute();
                    break;
                case AtisLetterKeyAction.EditLetter:
                    ViewModel.IsAtisLetterInputMode = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in AtisLetterOnKeyDown");
        }
    }

    // The letter button is hidden while the letter is typed, so focus has to be given back to it or it is lost.
    private void FocusAtisLetterAfterInput()
    {
        Dispatcher.UIThread.Post(() => AtisLetter.Focus(NavigationMethod.Tab));
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

    private void MetarInputOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null)
            return;

        if (e.Key == Key.Escape)
        {
            ViewModel.IsMetarInputMode = false;
            ViewModel.HasMetarInputError = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            if (!ViewModel.TryOverrideMetar(MetarInput.Text, out var error))
            {
                ViewModel.HasMetarInputError = true;
                ToolTip.SetTip(MetarInput, error);
                ToolTip.SetIsOpen(MetarInput, true);
            }

            e.Handled = true;
        }
    }

    private void MetarInputOnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.IsMetarInputMode = false;
            ViewModel.HasMetarInputError = false;
        }
    }

    private void MetarInputOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (ViewModel != null)
            ViewModel.HasMetarInputError = false;
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
            FocusAtisLetterAfterInput();
        }
        else if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            ViewModel.IsAtisLetterInputMode = false;
            FocusAtisLetterAfterInput();
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

    private void AirportConditions_OnTextChanged(object? sender, EventArgs e)
    {
        if (ViewModel?.SelectedAtisPreset == null)
            return;

        if (!AirportConditions.TextArea.IsFocused)
            return;

        ViewModel.HasUnsavedAirportConditions = true;
    }

    private void NotamText_OnTextChanged(object? sender, EventArgs e)
    {
        if (ViewModel?.SelectedAtisPreset == null)
            return;

        if (!NotamText.TextArea.IsFocused)
            return;

        ViewModel.HasUnsavedNotams = true;
    }
}
