// <copyright file="DatisReplacementsViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Provides a view model for managing D-ATIS text replacement rules within the ATIS configuration.
/// </summary>
public class DatisReplacementsViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private AtisStation? _selectedStation;
    private ObservableCollection<DatisTextReplacement>? _replacements;
    private string _prependAirportConditions = string.Empty;
    private string _appendAirportConditions = string.Empty;
    private string _prependNotams = string.Empty;
    private string _appendNotams = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatisReplacementsViewModel"/> class.
    /// </summary>
    /// <param name="profileRepository">The profile repository used to persist profile data.</param>
    /// <param name="sessionManager">The session manager for accessing the current profile.</param>
    public DatisReplacementsViewModel(IProfileRepository profileRepository, ISessionManager sessionManager)
    {
        _profileRepository = profileRepository;
        _sessionManager = sessionManager;

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

    /// <summary>
    /// Gets or sets text to prepend to D-ATIS airport conditions.
    /// </summary>
    public string PrependAirportConditions
    {
        get => _prependAirportConditions;
        set
        {
            this.RaiseAndSetIfChanged(ref _prependAirportConditions, value);
            if (SelectedStation != null)
            {
                SelectedStation.DatisPrependAirportConditions = value;
                SaveProfile();
            }
        }
    }

    /// <summary>
    /// Gets or sets text to append to D-ATIS airport conditions.
    /// </summary>
    public string AppendAirportConditions
    {
        get => _appendAirportConditions;
        set
        {
            this.RaiseAndSetIfChanged(ref _appendAirportConditions, value);
            if (SelectedStation != null)
            {
                SelectedStation.DatisAppendAirportConditions = value;
                SaveProfile();
            }
        }
    }

    /// <summary>
    /// Gets or sets text to prepend to D-ATIS NOTAMs.
    /// </summary>
    public string PrependNotams
    {
        get => _prependNotams;
        set
        {
            this.RaiseAndSetIfChanged(ref _prependNotams, value);
            if (SelectedStation != null)
            {
                SelectedStation.DatisPrependNotams = value;
                SaveProfile();
            }
        }
    }

    /// <summary>
    /// Gets or sets text to append to D-ATIS NOTAMs.
    /// </summary>
    public string AppendNotams
    {
        get => _appendNotams;
        set
        {
            this.RaiseAndSetIfChanged(ref _appendNotams, value);
            if (SelectedStation != null)
            {
                SelectedStation.DatisAppendNotams = value;
                SaveProfile();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SaveProfile()
    {
        if (_sessionManager.CurrentProfile != null)
        {
            _profileRepository.Save(_sessionManager.CurrentProfile);
        }
    }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        SelectedStation = station;
        Replacements = new ObservableCollection<DatisTextReplacement>(station.DatisTextReplacements);
        _prependAirportConditions = station.DatisPrependAirportConditions;
        this.RaisePropertyChanged(nameof(PrependAirportConditions));
        _appendAirportConditions = station.DatisAppendAirportConditions;
        this.RaisePropertyChanged(nameof(AppendAirportConditions));
        _prependNotams = station.DatisPrependNotams;
        this.RaisePropertyChanged(nameof(PrependNotams));
        _appendNotams = station.DatisAppendNotams;
        this.RaisePropertyChanged(nameof(AppendNotams));
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
        SaveProfile();
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
            SaveProfile();
        }
    }

    private void HandleCellEditEnding()
    {
        if (SelectedStation == null)
        {
            return;
        }

        SaveProfile();
    }
}
