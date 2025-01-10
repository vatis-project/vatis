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
    private readonly MessageBoxButton button;
    private readonly MessageBoxIcon icon;
    private string caption = "vATIS";
    private string? iconPath;
    private bool isCancelVisible;
    private bool isNoVisible;
    private bool isOkVisible;
    private bool isYesVisible;
    private string? message;
    private MessageBoxResult result;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxViewModel"/> class.
    /// </summary>
    public MessageBoxViewModel()
    {
        this.YesButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleYesButtonCommand);
        this.NoButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleNoButtonCommand);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButtonCommand);
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCancelButtonCommand);
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
        get => this.caption;
        set => this.RaiseAndSetIfChanged(ref this.caption, value);
    }

    /// <summary>
    /// Gets or sets the message content displayed in the message box.
    /// </summary>
    public string? Message
    {
        get => this.message;
        set => this.RaiseAndSetIfChanged(ref this.message, value);
    }

    /// <summary>
    /// Gets the button configuration for the message box.
    /// </summary>
    public MessageBoxButton Button
    {
        get => this.button;
        init
        {
            this.button = value;
            this.SetButtons();
            this.RaiseAndSetIfChanged(ref this.button, value);
        }
    }

    /// <summary>
    /// Gets the icon to display on the message box window.
    /// </summary>
    public MessageBoxIcon Icon
    {
        get => this.icon;
        init
        {
            this.icon = value;
            this.SetIcon();
            this.RaiseAndSetIfChanged(ref this.icon, value);
        }
    }

    /// <summary>
    /// Gets the result of the message box interaction.
    /// </summary>
    public MessageBoxResult Result
    {
        get => this.result;
        private set => this.RaiseAndSetIfChanged(ref this.result, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the "OK" button is visible in the message box.
    /// </summary>
    public bool IsOkVisible
    {
        get => this.isOkVisible;
        set => this.RaiseAndSetIfChanged(ref this.isOkVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the "Yes" button is visible in the message box UI.
    /// </summary>
    public bool IsYesVisible
    {
        get => this.isYesVisible;
        set => this.RaiseAndSetIfChanged(ref this.isYesVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the "No" button is visible in the message box.
    /// </summary>
    public bool IsNoVisible
    {
        get => this.isNoVisible;
        set => this.RaiseAndSetIfChanged(ref this.isNoVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Cancel button is visible.
    /// </summary>
    public bool IsCancelVisible
    {
        get => this.isCancelVisible;
        set => this.RaiseAndSetIfChanged(ref this.isCancelVisible, value);
    }

    /// <summary>
    /// Gets or sets the file path to the icon displayed in the message box.
    /// </summary>
    public string? IconPath
    {
        get => this.iconPath;
        set => this.RaiseAndSetIfChanged(ref this.iconPath, value);
    }

    /// <inheritdoc/>
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
                this.IconPath = string.Empty;
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
