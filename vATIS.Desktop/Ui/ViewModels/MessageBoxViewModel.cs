using System;
using System.ComponentModel;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;

namespace Vatsim.Vatis.Ui.ViewModels;

public class MessageBoxViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly MessageBoxButton _button;

    private readonly MessageBoxIcon _icon;

    private string _caption = "vATIS";

    private string? _iconPath;

    private bool _isCancelVisible;

    private bool _isNoVisible;

    private bool _isOkVisible;

    private bool _isYesVisible;

    private string? _message;

    private MessageBoxResult _result;

    public MessageBoxViewModel()
    {
        this.YesButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleYesButtonCommand);
        this.NoButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleNoButtonCommand);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButtonCommand);
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCancelButtonCommand);
    }

    public Window? Owner { get; init; }

    public ReactiveCommand<ICloseable, Unit> YesButtonCommand { get; }

    public ReactiveCommand<ICloseable, Unit> NoButtonCommand { get; }

    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    public string Caption
    {
        get => this._caption;
        set => this.RaiseAndSetIfChanged(ref this._caption, value);
    }

    public string? Message
    {
        get => this._message;
        set => this.RaiseAndSetIfChanged(ref this._message, value);
    }

    public MessageBoxButton Button
    {
        get => this._button;
        init
        {
            this._button = value;
            this.SetButtons();
            this.RaiseAndSetIfChanged(ref this._button, value);
        }
    }

    public MessageBoxIcon Icon
    {
        get => this._icon;
        init
        {
            this._icon = value;
            this.SetIcon();
            this.RaiseAndSetIfChanged(ref this._icon, value);
        }
    }

    public MessageBoxResult Result
    {
        get => this._result;
        private set => this.RaiseAndSetIfChanged(ref this._result, value);
    }

    public bool IsOkVisible
    {
        get => this._isOkVisible;
        set => this.RaiseAndSetIfChanged(ref this._isOkVisible, value);
    }

    public bool IsYesVisible
    {
        get => this._isYesVisible;
        set => this.RaiseAndSetIfChanged(ref this._isYesVisible, value);
    }

    public bool IsNoVisible
    {
        get => this._isNoVisible;
        set => this.RaiseAndSetIfChanged(ref this._isNoVisible, value);
    }

    public bool IsCancelVisible
    {
        get => this._isCancelVisible;
        set => this.RaiseAndSetIfChanged(ref this._isCancelVisible, value);
    }

    public string? IconPath
    {
        get => this._iconPath;
        set => this.RaiseAndSetIfChanged(ref this._iconPath, value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.YesButtonCommand.Dispose();
        this.NoButtonCommand.Dispose();
        this.OkButtonCommand.Dispose();
        this.CancelButtonCommand.Dispose();
    }

    private void HandleYesButtonCommand(ICloseable window)
    {
        this.Result = MessageBoxResult.Yes;
        window.Close(this.Result);
    }

    private void HandleNoButtonCommand(ICloseable window)
    {
        this.Result = MessageBoxResult.No;
        window.Close(this.Result);
    }

    private void HandleOkButtonCommand(ICloseable window)
    {
        this.Result = MessageBoxResult.Ok;
        window.Close(this.Result);
    }

    private void HandleCancelButtonCommand(ICloseable window)
    {
        this.Result = MessageBoxResult.Cancel;
        window.Close(this.Result);
    }

    private void SetIcon()
    {
        switch (this.Icon)
        {
            case MessageBoxIcon.Error:
            case MessageBoxIcon.Information:
            case MessageBoxIcon.Question:
            case MessageBoxIcon.Warning:
                this.IconPath = $"avares://vATIS/Assets/DialogIcons/{this.Icon}.ico";
                break;
            case MessageBoxIcon.None:
                this.IconPath = "";
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(this.Icon));
        }
    }

    private void SetButtons()
    {
        switch (this.Button)
        {
            case MessageBoxButton.Ok:
                this.IsOkVisible = true;
                break;
            case MessageBoxButton.OkCancel:
                this.IsOkVisible = true;
                this.IsCancelVisible = true;
                break;
            case MessageBoxButton.YesNoCancel:
                this.IsYesVisible = true;
                this.IsNoVisible = true;
                this.IsCancelVisible = true;
                break;
            case MessageBoxButton.YesNo:
                this.IsYesVisible = true;
                this.IsNoVisible = true;
                break;
            case MessageBoxButton.None:
                this.IsOkVisible = false;
                this.IsYesVisible = false;
                this.IsNoVisible = false;
                this.IsCancelVisible = false;
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(this.Button));
        }
    }
}