// <copyright file="NewAtisStationDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the dialog used to create a new ATIS station.
/// </summary>
public class NewAtisStationDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private string? airportIdentifier;
    private AtisType atisType = AtisType.Combined;
    private DialogResult dialogResult;
    private string? stationName;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewAtisStationDialogViewModel"/> class.
    /// </summary>
    public NewAtisStationDialogViewModel()
    {
        this.CancelButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleCancelButtonCommand);
        this.OkButtonCommand = ReactiveCommand.Create<ICloseable>(this.HandleOkButtonCommand);
    }

    /// <summary>
    /// Raised when the dialog result changes.
    /// </summary>
    public event EventHandler<DialogResult>? DialogResultChanged;

    /// <summary>
    /// Gets the command associated with the cancel button functionality.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelButtonCommand { get; }

    /// <summary>
    /// Gets the command associated with the OK button functionality.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> OkButtonCommand { get; }

    /// <summary>
    /// Gets or sets the result of the dialog.
    /// </summary>
    public DialogResult DialogResult
    {
        get => this.dialogResult;
        set => this.RaiseAndSetIfChanged(ref this.dialogResult, value);
    }

    /// <summary>
    /// Gets or sets the airport identifier associated with the ATIS station.
    /// </summary>
    public string? AirportIdentifier
    {
        get => this.airportIdentifier;
        set => this.RaiseAndSetIfChanged(ref this.airportIdentifier, value);
    }

    /// <summary>
    /// Gets or sets the name of the station for the ATIS configuration.
    /// </summary>
    public string? StationName
    {
        get => this.stationName;
        set => this.RaiseAndSetIfChanged(ref this.stationName, value);
    }

    /// <summary>
    /// Gets or sets the current ATIS type represented by the view model.
    /// </summary>
    public AtisType AtisType
    {
        get => this.atisType;
        set => this.RaiseAndSetIfChanged(ref this.atisType, value);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.CancelButtonCommand.Dispose();
        this.OkButtonCommand.Dispose();
    }

    private void HandleOkButtonCommand(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Ok);
        this.DialogResult = DialogResult.Ok;
        if (!this.HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCancelButtonCommand(ICloseable window)
    {
        this.DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        this.DialogResult = DialogResult.Cancel;
        window.Close();
    }
}
