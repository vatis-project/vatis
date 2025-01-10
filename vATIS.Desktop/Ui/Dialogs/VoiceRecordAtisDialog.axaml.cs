using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class VoiceRecordAtisDialog : ReactiveWindow<VoiceRecordAtisDialogViewModel>, ICloseable
{
    public VoiceRecordAtisDialog(VoiceRecordAtisDialogViewModel viewModel)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;
        this.Closed += this.OnClosed;
    }

    public VoiceRecordAtisDialog()
    {
        this.InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.ViewModel?.Dispose();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        this.ViewModel!.DialogOwner = this;
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.PositionChanged += this.OnPositionChanged;
        if (this.DataContext is VoiceRecordAtisDialogViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (this.DataContext is VoiceRecordAtisDialogViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}