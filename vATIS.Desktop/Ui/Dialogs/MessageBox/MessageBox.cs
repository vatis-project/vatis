using System.Threading.Tasks;
using Avalonia.Controls;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs.MessageBox;

public static class MessageBox
{
    public static async Task<MessageBoxResult> Show(string messageBoxText, string caption)
    {
        return await ShowCore(messageBoxText, caption, MessageBoxButton.Ok, MessageBoxIcon.None);
    }

    public static async Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button)
    {
        return await ShowCore(messageBoxText, caption, button, MessageBoxIcon.None);
    }

    public static async Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button,
        MessageBoxIcon icon)
    {
        return await ShowCore(messageBoxText, caption, button, icon);
    }

    public static async Task<MessageBoxResult> ShowDialog(Window owner, string messageBoxText, string caption)
    {
        return await ShowDialogCore(owner, messageBoxText, caption, MessageBoxButton.Ok, MessageBoxIcon.None);
    }

    public static async Task<MessageBoxResult> ShowDialog(Window owner, string messageBoxText, string caption,
        MessageBoxButton button)
    {
        return await ShowDialogCore(owner, messageBoxText, caption, button, MessageBoxIcon.None);
    }

    public static async Task<MessageBoxResult> ShowDialog(Window owner, string messageBoxText, string caption,
        MessageBoxButton button, MessageBoxIcon icon)
    {
        return await ShowDialogCore(owner, messageBoxText, caption, button, icon);
    }

    private static Task<MessageBoxResult> ShowCore(string messageBoxText, string caption, MessageBoxButton button,
        MessageBoxIcon icon)
    {
        var viewModel = new MessageBoxViewModel()
        {
            Caption = caption,
            Message = messageBoxText,
            Button = button,
            Icon = icon
        };

        var window = new MessageBoxView
        {
            DataContext = viewModel
        };

        var tcs = new TaskCompletionSource<MessageBoxResult>();

        window.Closed += delegate { tcs.TrySetResult(viewModel.Result); };
        window.Topmost = true;
        window.ShowInTaskbar = true;
        window.Show();

        return tcs.Task;
    }

    private static Task<MessageBoxResult> ShowDialogCore(Window owner, string messageBoxText, string caption,
        MessageBoxButton button, MessageBoxIcon icon)
    {
        var viewModel = new MessageBoxViewModel
        {
            Caption = caption,
            Message = messageBoxText,
            Button = button,
            Icon = icon,
            Owner = owner
        };

        var window = new MessageBoxView
        {
            DataContext = viewModel,
            Topmost = owner.Topmost
        };

        return window.ShowDialog<MessageBoxResult>(owner);
    }
}