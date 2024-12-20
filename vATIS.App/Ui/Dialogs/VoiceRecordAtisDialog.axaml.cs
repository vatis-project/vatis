using System;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class VoiceRecordAtisDialog : ReactiveWindow<VoiceRecordAtisDialogViewModel>, ICloseable
{
    public VoiceRecordAtisDialog(VoiceRecordAtisDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public VoiceRecordAtisDialog()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ViewModel!.DialogOwner = this;
    }
}