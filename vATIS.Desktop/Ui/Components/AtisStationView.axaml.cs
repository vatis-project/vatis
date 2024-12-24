using System;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Components;

public partial class AtisStationView : ReactiveUserControl<AtisStationViewModel>
{
    private bool mAirportConditionsInitialized;
    private bool mNotamsInitialized;

    public AtisStationView()
    {
        InitializeComponent();
        
        TypeAtisLetter.KeyDown += TypeAtisLetterOnKeyDown;
        AtisLetter.DoubleTapped += AtisLetterOnDoubleTapped;
        AtisLetter.Click += AtisLetterOnClick;
        AtisLetter.PointerPressed += AtisLetterOnPointerPressed;
    }

    private void AtisLetterOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel == null)
            return;
        
        if (ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
            return;
        
        if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
        {
            ViewModel.IsAtisLetterInputMode = true;
        }
    }
    
    private async void AtisLetterOnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if(ViewModel == null)
                return;
            
            if (ViewModel.NetworkConnectionStatus == NetworkConnectionStatus.Observer)
                return;

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
            TypeAtisLetter.Text = "A";
            ViewModel.AtisLetter = 'A';
        }
        else if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            ViewModel.IsAtisLetterInputMode = false;
            if (char.TryParse(TypeAtisLetter.Text?.ToUpperInvariant(), out var letter))
            {
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
    }

    private void AirportConditions_OnTextChanged(object? sender, EventArgs e)
    {
        if (ViewModel?.SelectedAtisPreset == null)
            return;

        if (!AirportConditions.TextArea.IsFocused)
            return;

        if (mAirportConditionsInitialized)
        {
            ViewModel.HasUnsavedAirportConditions = true;
        }

        mAirportConditionsInitialized = true;
    }

    private void NotamFreeText_OnTextChanged(object? sender, EventArgs e)
    {
        if (ViewModel?.SelectedAtisPreset == null)
            return;

        if (!NotamFreeText.TextArea.IsFocused)
            return;
        
        if (mNotamsInitialized)
        {
            ViewModel.HasUnsavedNotams = true;
        }

        mNotamsInitialized = true;
    }
}