using System;
using Avalonia;
using Avalonia.Controls;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs.MessageBox;

public partial class MessageBoxView : Window, ICloseable
{
    public MessageBoxView()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DataContext is MessageBoxViewModel vm)
        {
            vm.Dispose();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(CenterWindow);
    }

    private void CenterWindow()
    {
        if (DataContext is MessageBoxViewModel { Owner: not null } viewModel)
        {
            var owner = viewModel.Owner;
            var ownerPosition = owner.Position;

            Position = new PixelPoint(
                (int)(ownerPosition.X + (owner.Width - Width) / 2),
                (int)(ownerPosition.Y + (owner.Height - Height) / 2));
        }
    }
}