using System.ComponentModel;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;

namespace Vatsim.Vatis.Ui.ViewModels;

public class MessageBoxViewModel : ReactiveViewModelBase
{
    public Window? Owner { get; set; }
    
    public ReactiveCommand<ICloseable, Unit> YesButtonCommand { get; private set; }
    public ReactiveCommand<ICloseable, Unit> NoButtonCommand { get; private set; }
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; private set; }
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; private set; }


    private string mCaption = "vATIS";
    public string Caption
    {
        get => mCaption;
        set => this.RaiseAndSetIfChanged(ref mCaption, value);
    }

    private string? mMessage;
    public string? Message
    {
        get => mMessage;
        set => this.RaiseAndSetIfChanged(ref mMessage, value);
    }

    private readonly MessageBoxButton mButton;
    public MessageBoxButton Button
    {
        get => mButton;
        init
        {
            mButton = value;
            SetButtons();
            this.RaiseAndSetIfChanged(ref mButton, value);
        }
    }

    private readonly MessageBoxIcon mIcon;
    public MessageBoxIcon Icon
    {
        get => mIcon;
        init
        {
            mIcon = value;
            SetIcon();
            this.RaiseAndSetIfChanged(ref mIcon, value);
        }
    }

    private MessageBoxResult mResult;
    public MessageBoxResult Result
    {
        get => mResult;
        private set => this.RaiseAndSetIfChanged(ref mResult, value);
    }

    private bool mIsOkVisible;
    public bool IsOkVisible
    {
        get => mIsOkVisible;
        set => this.RaiseAndSetIfChanged(ref mIsOkVisible, value);
    }

    private bool mIsYesVisible;
    public bool IsYesVisible
    {
        get => mIsYesVisible;
        set => this.RaiseAndSetIfChanged(ref mIsYesVisible, value);
    }

    private bool mIsNoVisible;
    public bool IsNoVisible
    {
        get => mIsNoVisible;
        set => this.RaiseAndSetIfChanged(ref mIsNoVisible, value);
    }

    private bool mIsCancelVisible;
    public bool IsCancelVisible
    {
        get => mIsCancelVisible;
        set => this.RaiseAndSetIfChanged(ref mIsCancelVisible, value);
    }

    private string? mIconPath;
    public string? IconPath
    {
        get => mIconPath;
        set => this.RaiseAndSetIfChanged(ref mIconPath, value);
    }

    public MessageBoxViewModel()
    {
        YesButtonCommand = ReactiveCommand.Create<ICloseable>(HandleYesButtonCommand);
        NoButtonCommand = ReactiveCommand.Create<ICloseable>(HandleNoButtonCommand);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButtonCommand);
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButtonCommand);
    }

    private void HandleYesButtonCommand(ICloseable window)
    {
        Result = MessageBoxResult.Yes;
        window.Close(Result);
    }

    private void HandleNoButtonCommand(ICloseable window)
    {
        Result = MessageBoxResult.No;
        window.Close(Result);
    }

    private void HandleOkButtonCommand(ICloseable window)
    {
        Result = MessageBoxResult.Ok;
        window.Close(Result);
    }

    private void HandleCancelButtonCommand(ICloseable window)
    {
        Result = MessageBoxResult.Cancel;
        window.Close(Result);
    }

    private void SetIcon()
    {
        switch (Icon)
        {
            case MessageBoxIcon.Error:
            case MessageBoxIcon.Information:
            case MessageBoxIcon.Question:
            case MessageBoxIcon.Warning:
                IconPath = $"avares://vATIS/Assets/DialogIcons/{Icon}.ico";
                break;
            case MessageBoxIcon.None:
                IconPath = "";
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(Icon));
        }
    }

    private void SetButtons()
    {
        switch (Button)
        {
            case MessageBoxButton.Ok:
                IsOkVisible = true;
                break;
            case MessageBoxButton.OkCancel:
                IsOkVisible = true;
                IsCancelVisible = true;
                break;
            case MessageBoxButton.YesNoCancel:
                IsYesVisible = true;
                IsNoVisible = true;
                IsCancelVisible = true;
                break;
            case MessageBoxButton.YesNo:
                IsYesVisible = true;
                IsNoVisible = true;
                break;
            case MessageBoxButton.None:
                IsOkVisible = false;
                IsYesVisible = false;
                IsNoVisible = false;
                IsCancelVisible = false;
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(Button));
        }
    }
}