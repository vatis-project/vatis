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

/// <summary>
/// Provides a view model for configuring and managing a message box UI.
/// </summary>
public class MessageBoxViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly MessageBoxButton _button;
    private readonly MessageBoxIcon _icon;
    private MessageBoxResult _result;
    private string _caption = "vATIS";
    private string? _message;
    private bool _isOkVisible;
    private bool _isYesVisible;
    private bool _isNoVisible;
    private bool _isCancelVisible;
    private string? _iconPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxViewModel"/> class.
    /// </summary>
    public MessageBoxViewModel()
    {
        YesButtonCommand = ReactiveCommand.Create<ICloseable>(HandleYesButtonCommand);
        NoButtonCommand = ReactiveCommand.Create<ICloseable>(HandleNoButtonCommand);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButtonCommand);
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButtonCommand);
    }

    /// <summary>
    /// Gets the owner window for the message box.
    /// </summary>
    public Window? Owner { get; init; }

    /// <summary>
    /// Gets the command executed when the "Yes" button is clicked.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> YesButtonCommand { get; }

    /// <summary>
    /// Gets the command that is executed when the "No" button is clicked in the message box.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> NoButtonCommand { get; }

    /// <summary>
    /// Gets the command bound to the "OK" button, which handles the logic for when the "OK" button is clicked.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    /// <summary>
    /// Gets the command that is executed when the Cancel button is clicked.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    /// <summary>
    /// Gets or sets the caption text for the message box.
    /// </summary>
    public string Caption
    {
        get => _caption;
        set => this.RaiseAndSetIfChanged(ref _caption, value);
    }

    /// <summary>
    /// Gets or sets the message content displayed in the message box.
    /// </summary>
    public string? Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    /// <summary>
    /// Gets the button configuration for the message box.
    /// </summary>
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

    /// <summary>
    /// Gets the icon to display on the message box window.
    /// </summary>
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

    /// <summary>
    /// Gets the result of the message box interaction.
    /// </summary>
    public MessageBoxResult Result
    {
        get => _result;
        private set => this.RaiseAndSetIfChanged(ref _result, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the "OK" button is visible in the message box.
    /// </summary>
    public bool IsOkVisible
    {
        get => _isOkVisible;
        set => this.RaiseAndSetIfChanged(ref _isOkVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the "Yes" button is visible in the message box UI.
    /// </summary>
    public bool IsYesVisible
    {
        get => _isYesVisible;
        set => this.RaiseAndSetIfChanged(ref _isYesVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the "No" button is visible in the message box.
    /// </summary>
    public bool IsNoVisible
    {
        get => _isNoVisible;
        set => this.RaiseAndSetIfChanged(ref _isNoVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Cancel button is visible.
    /// </summary>
    public bool IsCancelVisible
    {
        get => _isCancelVisible;
        set => this.RaiseAndSetIfChanged(ref _isCancelVisible, value);
    }

    /// <summary>
    /// Gets or sets the file path to the icon displayed in the message box.
    /// </summary>
    public string? IconPath
    {
        get => _iconPath;
        set => this.RaiseAndSetIfChanged(ref _iconPath, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        YesButtonCommand.Dispose();
        NoButtonCommand.Dispose();
        OkButtonCommand.Dispose();
        CancelButtonCommand.Dispose();

        GC.SuppressFinalize(this);
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
