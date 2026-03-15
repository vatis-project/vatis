// <copyright file="DatisReplacementsViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Provides a view model for managing D-ATIS text replacement rules within the ATIS configuration.
/// </summary>
public class DatisReplacementsViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly IAppConfig _appConfig;
    private AtisStation? _selectedStation;
    private ObservableCollection<DatisTextReplacement>? _replacements;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatisReplacementsViewModel"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration.</param>
    public DatisReplacementsViewModel(IAppConfig appConfig)
    {
        _appConfig = appConfig;

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleAtisStationChanged);
        AddReplacementCommand = ReactiveCommand.Create(HandleAddReplacement);
        DeleteReplacementCommand = ReactiveCommand.Create<DatisTextReplacement>(HandleDeleteReplacement);
        CellEditEndingCommand = ReactiveCommand.Create(HandleCellEditEnding);

        _disposables.Add(AtisStationChanged);
        _disposables.Add(AddReplacementCommand);
        _disposables.Add(DeleteReplacementCommand);
        _disposables.Add(CellEditEndingCommand);
    }

    /// <summary>
    /// Gets the command to handle changes to the ATIS station.
    /// </summary>
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    /// <summary>
    /// Gets the command to add a new replacement rule.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddReplacementCommand { get; }

    /// <summary>
    /// Gets the command to delete a replacement rule.
    /// </summary>
    public ReactiveCommand<DatisTextReplacement, Unit> DeleteReplacementCommand { get; }

    /// <summary>
    /// Gets the command executed when a cell edit operation ends.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CellEditEndingCommand { get; }

    /// <summary>
    /// Gets or sets the currently selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    /// <summary>
    /// Gets or sets the collection of D-ATIS text replacement rules.
    /// </summary>
    public ObservableCollection<DatisTextReplacement>? Replacements
    {
        get => _replacements;
        set => this.RaiseAndSetIfChanged(ref _replacements, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        SelectedStation = station;
        Replacements = new ObservableCollection<DatisTextReplacement>(station.DatisTextReplacements);
    }

    private void HandleAddReplacement()
    {
        if (SelectedStation == null || Replacements == null)
        {
            return;
        }

        var replacement = new DatisTextReplacement();
        SelectedStation.DatisTextReplacements.Add(replacement);
        Replacements.Add(replacement);
        _appConfig.SaveConfig();
    }

    private void HandleDeleteReplacement(DatisTextReplacement? item)
    {
        if (item == null || SelectedStation == null || Replacements == null)
        {
            return;
        }

        if (Replacements.Remove(item))
        {
            SelectedStation.DatisTextReplacements.Remove(item);
            _appConfig.SaveConfig();
        }
    }

    private void HandleCellEditEnding()
    {
        if (SelectedStation == null)
        {
            return;
        }

        _appConfig.SaveConfig();
    }
}
