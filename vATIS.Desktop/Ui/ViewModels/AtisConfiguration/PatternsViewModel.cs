// <copyright file="PatternsViewModel.cs" company="Justin Shannon">
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
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Provides a view model for managing the regular expression patterns of an ATIS station.
/// </summary>
public class PatternsViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly IAppConfig _appConfig;
    private readonly IWindowFactory _windowFactory;
    private readonly ISessionManager _sessionManager;
    private readonly IProfileRepository _profileRepository;
    private IDialogOwner? _dialogOwner;
    private AtisStation? _selectedStation;
    private ObservableCollection<PatternMeta>? _patterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternsViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">The factory used to create windows.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="sessionManager">The session manager providing the current profile.</param>
    /// <param name="profileRepository">The repository used to persist the profile.</param>
    public PatternsViewModel(
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
        NewPatternCommand = ReactiveCommand.CreateFromTask(() => ShowPatternDialog(null));
        EditPatternCommand = ReactiveCommand.CreateFromTask<PatternMeta>(HandleEditPattern);
        DeletePatternCommand = ReactiveCommand.CreateFromTask<PatternMeta>(HandleDeletePattern);

        _disposables.Add(AtisStationChanged);
        _disposables.Add(NewPatternCommand);
        _disposables.Add(EditPatternCommand);
        _disposables.Add(DeletePatternCommand);
    }

    /// <summary>
    /// Gets the command to handle changes to the ATIS station.
    /// </summary>
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    /// <summary>
    /// Gets the command used to add a new pattern.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewPatternCommand { get; }

    /// <summary>
    /// Gets the command used to edit the selected pattern.
    /// </summary>
    public ReactiveCommand<PatternMeta, Unit> EditPatternCommand { get; }

    /// <summary>
    /// Gets the command to delete the selected pattern.
    /// </summary>
    public ReactiveCommand<PatternMeta, Unit> DeletePatternCommand { get; }

    /// <summary>
    /// Gets or sets the currently selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    /// <summary>
    /// Gets or sets the patterns of the selected station.
    /// </summary>
    public ObservableCollection<PatternMeta>? Patterns
    {
        get => _patterns;
        set => this.RaiseAndSetIfChanged(ref _patterns, value);
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
        Patterns = new ObservableCollection<PatternMeta>(station.Patterns);
    }

    private async Task HandleDeletePattern(PatternMeta? item)
    {
        if (item == null || Patterns == null || _dialogOwner == null || SelectedStation == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)_dialogOwner,
                "Are you sure you want to delete the selected pattern?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (Patterns.Remove(item))
            {
                SelectedStation.Patterns.Remove(item);
                _appConfig.SaveConfig();
            }
        }
    }

    private Task HandleEditPattern(PatternMeta? item)
    {
        return item == null ? Task.CompletedTask : ShowPatternDialog(item);
    }

    /// <summary>
    /// Shows the pattern dialog to add a new pattern, or to edit <paramref name="existing"/>.
    /// </summary>
    private async Task ShowPatternDialog(PatternMeta? existing)
    {
        if (_dialogOwner == null || SelectedStation == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow })
        {
            return;
        }

        var dialog = _windowFactory.CreateNewPatternDialog();
        dialog.Topmost = mainWindow.Topmost;
        if (dialog.DataContext is not NewPatternDialogViewModel context)
        {
            return;
        }

        context.Title = existing == null ? "New Pattern" : "Edit Pattern";
        context.ShowAddToAllStations = existing == null;
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

            if (string.IsNullOrWhiteSpace(context.Text))
            {
                context.RaiseError("Text", "Value is required.");
            }

            if (string.IsNullOrWhiteSpace(context.Spoken))
            {
                context.RaiseError("Spoken", "Value is required.");
            }

            foreach (var (field, message) in PatternMatcher.Validate(
                         context.Pattern, context.Text, context.Spoken, requirePattern: true))
            {
                context.RaiseError(field == "Voice" ? "Spoken" : field, message);
            }

            if (context.HasErrors)
            {
                return;
            }

            var pattern = existing ?? new PatternMeta();
            pattern.Pattern = context.Pattern;
            pattern.Text = context.Text;
            pattern.Voice = context.Spoken;

            if (existing == null)
            {
                SelectedStation.Patterns.Add(pattern);
            }

            _appConfig.SaveConfig();

            if (existing == null && context.AddToAllStations)
            {
                AddToOtherStations(pattern);
            }

            Patterns = new ObservableCollection<PatternMeta>(SelectedStation.Patterns);
        };

        await dialog.ShowDialog((Window)_dialogOwner);
    }

    private void AddToOtherStations(PatternMeta pattern)
    {
        var profile = _sessionManager.CurrentProfile;
        if (profile?.Stations == null || SelectedStation == null)
        {
            return;
        }

        var changed = false;
        foreach (var station in profile.Stations)
        {
            if (station.Id == SelectedStation.Id || station.Patterns.Any(x => x.Pattern == pattern.Pattern))
            {
                continue;
            }

            station.Patterns.Add(pattern.Clone());
            changed = true;
        }

        if (changed)
        {
            _profileRepository.Save(profile);
        }
    }
}
