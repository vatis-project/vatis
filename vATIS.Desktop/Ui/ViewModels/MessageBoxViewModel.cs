// <copyright file="MessageBoxViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.ComponentModel;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;

namespace Vatsim.Vatis.Ui.ViewModels;

public class MessageBoxViewModel : ReactiveViewModelBase, IDisposable
{
    public Window? Owner { get; init; }

    public ReactiveCommand<ICloseable, Unit> YesButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> NoButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }


    private string _caption = "vATIS";
    public string Caption
    {
        get => _caption;
        set => this.RaiseAndSetIfChanged(ref _caption, value);
    }

    private string? _message;
    public string? Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    private readonly MessageBoxButton _button;
    public MessageBoxButton Button
    {
        get => _button;
        init
        {
            _button = value;
            SetButtons();
            this.RaiseAndSetIfChanged(ref _button, value);
        }
    }

    private readonly MessageBoxIcon _icon;
    public MessageBoxIcon Icon
    {
        get => _icon;
        init
        {
            _icon = value;
            SetIcon();
            this.RaiseAndSetIfChanged(ref _icon, value);
        }
    }

    private MessageBoxResult _result;
    public MessageBoxResult Result
    {
        get => _result;
        private set => this.RaiseAndSetIfChanged(ref _result, value);
    }

    private bool _isOkVisible;
    public bool IsOkVisible
    {
        get => _isOkVisible;
        set => this.RaiseAndSetIfChanged(ref _isOkVisible, value);
    }

    private bool _isYesVisible;
    public bool IsYesVisible
    {
        get => _isYesVisible;
        set => this.RaiseAndSetIfChanged(ref _isYesVisible, value);
    }

    private bool _isNoVisible;
    public bool IsNoVisible
    {
        get => _isNoVisible;
        set => this.RaiseAndSetIfChanged(ref _isNoVisible, value);
    }

    private bool _isCancelVisible;
    public bool IsCancelVisible
    {
        get => _isCancelVisible;
        set => this.RaiseAndSetIfChanged(ref _isCancelVisible, value);
    }

    private string? _iconPath;
    public string? IconPath
    {
        get => _iconPath;
        set => this.RaiseAndSetIfChanged(ref _iconPath, value);
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        YesButtonCommand.Dispose();
        NoButtonCommand.Dispose();
        OkButtonCommand.Dispose();
        CancelButtonCommand.Dispose();
    }
}
