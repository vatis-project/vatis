// <copyright file="ContractionsViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Provides a view model for managing contractions within the ATIS configuration.
/// </summary>
public class ContractionsViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly IAppConfig _appConfig;
    private readonly IWindowFactory _windowFactory;
    private readonly ISessionManager _sessionManager;
    private readonly IProfileRepository _profileRepository;
    private IDialogOwner? _dialogOwner;
    private AtisStation? _selectedStation;
    private bool _hasUnsavedChanges;
    private ObservableCollection<ContractionMeta>? _contractions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractionsViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">The factory used to create windows.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="sessionManager">The session manager providing the current profile.</param>
    /// <param name="profileRepository">The repository used to persist the profile.</param>
    public ContractionsViewModel(
        IWindowFactory windowFactory,
        IAppConfig appConfig,
        ISessionManager sessionManager,
        IProfileRepository profileRepository)
    {
        _windowFactory = windowFactory;
        _appConfig = appConfig;
        _sessionManager = sessionManager;
        _profileRepository = profileRepository;

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleAtisStationChanged);
        NewContractionCommand = ReactiveCommand.CreateFromTask(() => ShowContractionDialog(null));
        EditContractionCommand = ReactiveCommand.CreateFromTask<ContractionMeta>(HandleEditContraction);
        DeleteContractionCommand = ReactiveCommand.CreateFromTask<ContractionMeta>(HandleDeleteContraction);

        _disposables.Add(AtisStationChanged);
        _disposables.Add(NewContractionCommand);
        _disposables.Add(EditContractionCommand);
        _disposables.Add(DeleteContractionCommand);
    }

    /// <summary>
    /// Gets the command to handle changes to the ATIS station.
    /// </summary>
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    /// <summary>
    /// Gets the command used to add a new contraction.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewContractionCommand { get; }

    /// <summary>
    /// Gets the command used to edit the selected contraction.
    /// </summary>
    public ReactiveCommand<ContractionMeta, Unit> EditContractionCommand { get; }

    /// <summary>
    /// Gets the command to delete a selected contraction.
    /// </summary>
    public ReactiveCommand<ContractionMeta, Unit> DeleteContractionCommand { get; }

    /// <summary>
    /// Gets or sets the currently selected ATIS station associated with the view model.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes in the view model.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets the collection of contractions associated with the ATIS configuration.
    /// </summary>
    public ObservableCollection<ContractionMeta>? Contractions
    {
        get => _contractions;
        set => this.RaiseAndSetIfChanged(ref _contractions, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Sets the dialog owner for the current instance.
    /// </summary>
    /// <param name="owner">The <see cref="IDialogOwner"/> to associate with this instance.</param>
    public void SetDialogOwner(IDialogOwner? owner)
    {
        _dialogOwner = owner;
    }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        SelectedStation = station;
        Contractions = new ObservableCollection<ContractionMeta>(station.Contractions);
    }

    private async Task HandleDeleteContraction(ContractionMeta? item)
    {
        if (item == null || Contractions == null || _dialogOwner == null || SelectedStation == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)_dialogOwner,
                "Are you sure you want to delete the selected contraction?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (Contractions.Remove(item))
            {
                SelectedStation.Contractions.Remove(item);
                _appConfig.SaveConfig();
                EventBus.Instance.Publish(new ContractionsUpdated(SelectedStation.Id));
            }
        }
    }

    private Task HandleEditContraction(ContractionMeta? item)
    {
        return item == null ? Task.CompletedTask : ShowContractionDialog(item);
    }

    /// <summary>
    /// Shows the contraction dialog to add a new contraction, or to edit <paramref name="existing"/>.
    /// </summary>
    private async Task ShowContractionDialog(ContractionMeta? existing)
    {
        if (_dialogOwner == null || SelectedStation == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow })
        {
            return;
        }

        var dialog = _windowFactory.CreateNewContractionDialog();
        dialog.Topmost = mainWindow.Topmost;
        if (dialog.DataContext is not NewContractionDialogViewModel context)
        {
            return;
        }

        context.Title = existing == null ? "New Contraction" : "Edit Contraction";
        context.ShowAddToAllStations = existing == null;
        context.Variable = existing?.VariableName ?? string.Empty;
        context.IsRegex = existing?.IsRegex ?? false;
        context.Pattern = existing?.Pattern ?? string.Empty;
        context.Text = existing?.Text ?? string.Empty;
        context.Spoken = existing?.Voice ?? string.Empty;

        context.DialogResultChanged += (_, dialogResult) =>
        {
            if (dialogResult != DialogResult.Ok)
            {
                return;
            }

            context.ClearAllErrors();

            if (string.IsNullOrEmpty(context.Variable))
            {
                context.RaiseError("Variable", "Value is required");
            }

            if (string.IsNullOrWhiteSpace(context.Text))
            {
                context.RaiseError("Text", "Value is required.");
            }

            if (string.IsNullOrWhiteSpace(context.Spoken))
            {
                context.RaiseError("Spoken", "Value is required.");
            }

            if (context.IsRegex)
            {
                foreach (var (field, message) in ContractionMatcher.Validate(
                             context.Pattern, context.Text, context.Spoken, requirePattern: true))
                {
                    context.RaiseError(field == "Voice" ? "Spoken" : field, message);
                }
            }

            if (SelectedStation.Contractions.Any(x => x != existing && x.VariableName == context.Variable))
            {
                context.RaiseError("Variable", "A contraction with this variable name already exist.");
            }

            if (context.HasErrors)
            {
                return;
            }

            var contraction = existing ?? new ContractionMeta();
            contraction.VariableName = context.Variable;
            contraction.Text = context.Text;
            contraction.Voice = context.Spoken;
            contraction.IsRegex = context.IsRegex;
            contraction.Pattern = context.IsRegex ? context.Pattern : null;

            if (existing == null)
            {
                SelectedStation.Contractions.Add(contraction);
            }

            _appConfig.SaveConfig();

            if (existing == null && context.AddToAllStations)
            {
                AddToOtherStations(contraction);
            }

            Contractions = new ObservableCollection<ContractionMeta>(SelectedStation.Contractions);
            EventBus.Instance.Publish(new ContractionsUpdated(SelectedStation.Id));
        };

        await dialog.ShowDialog((Window)_dialogOwner);
    }

    private void AddToOtherStations(ContractionMeta contraction)
    {
        var profile = _sessionManager.CurrentProfile;
        if (profile?.Stations == null || SelectedStation == null)
        {
            return;
        }

        var updatedStationIds = new List<string>();
        foreach (var station in profile.Stations)
        {
            if (station.Id == SelectedStation.Id ||
                station.Contractions.Any(x => x.VariableName == contraction.VariableName))
            {
                continue;
            }

            station.Contractions.Add(contraction.Clone());
            updatedStationIds.Add(station.Id);
        }

        if (updatedStationIds.Count == 0)
        {
            return;
        }

        _profileRepository.Save(profile);
        foreach (var id in updatedStationIds)
        {
            EventBus.Instance.Publish(new ContractionsUpdated(id));
        }
    }
}
