using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs.MessageBox;

public partial class MessageBoxView : Window, ICloseable
{
    public MessageBoxView()
    {
        this.InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (this.DataContext is MessageBoxViewModel vm)
        {
            vm.Dispose();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        Dispatcher.UIThread.InvokeAsync(this.CenterWindow);
    }

    private void CenterWindow()
    {
        if (this.DataContext is MessageBoxViewModel { Owner: not null } viewModel)
        {
            var owner = viewModel.Owner;
            var ownerPosition = owner.Position;

            this.Position = new PixelPoint(
                (int)(ownerPosition.X + ((owner.Width - this.Width) / 2)),
                (int)(ownerPosition.Y + ((owner.Height - this.Height) / 2)));
        }
    }
}