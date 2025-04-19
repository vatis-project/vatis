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
    private DialogResult _dialogResult;
    private string? _airportIdentifier;
    private string? _stationName;
    private string? _frequency;
    private AtisType _atisType = AtisType.Combined;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewAtisStationDialogViewModel"/> class.
    /// </summary>
    public NewAtisStationDialogViewModel()
    {
        CancelButtonCommand = ReactiveCommand.Create<ICloseable>(HandleCancelButtonCommand);
        OkButtonCommand = ReactiveCommand.Create<ICloseable>(HandleOkButtonCommand);
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
        get => _dialogResult;
        set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    /// <summary>
    /// Gets or sets the airport identifier associated with the ATIS station.
    /// </summary>
    public string? AirportIdentifier
    {
        get => _airportIdentifier;
        set => this.RaiseAndSetIfChanged(ref _airportIdentifier, value);
    }

    /// <summary>
    /// Gets or sets the name of the station for the ATIS configuration.
    /// </summary>
    public string? StationName
    {
        get => _stationName;
        set => this.RaiseAndSetIfChanged(ref _stationName, value);
    }

    /// <summary>
    /// Gets or sets the frequency of the ATIS station.
    /// </summary>
    public string? Frequency
    {
        get => _frequency;
        set => this.RaiseAndSetIfChanged(ref _frequency, value);
    }

    /// <summary>
    /// Gets or sets the current ATIS type represented by the view model.
    /// </summary>
    public AtisType AtisType
    {
        get => _atisType;
        set => this.RaiseAndSetIfChanged(ref _atisType, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CancelButtonCommand.Dispose();
        OkButtonCommand.Dispose();

        GC.SuppressFinalize(this);
    }

    private void HandleOkButtonCommand(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Ok);
        DialogResult = DialogResult.Ok;
        if (!HasErrors)
        {
            window.Close();
        }
    }

    private void HandleCancelButtonCommand(ICloseable window)
    {
        DialogResultChanged?.Invoke(this, DialogResult.Cancel);
        DialogResult = DialogResult.Cancel;
        window.Close();
    }
}
