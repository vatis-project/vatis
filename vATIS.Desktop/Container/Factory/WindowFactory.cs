// <copyright file="WindowFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Ui;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Container.Factory;

/// <summary>
/// Provides factory methods to create various windows and dialogs in the application.
/// </summary>
internal class WindowFactory : IWindowFactory
{
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowFactory"/> class.
    /// </summary>
    /// <param name="provider">
    /// The service provider used for dependency resolution.
    /// </param>
    public WindowFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="MainWindow"/> class.
    /// </returns>
    public MainWindow CreateMainWindow()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<MainWindowViewModel>();
        return new MainWindow(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ProfileListDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="ProfileListDialog"/> class.
    /// </returns>
    public ProfileListDialog CreateProfileListDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<ProfileListViewModel>();
        return new ProfileListDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SettingsDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="SettingsDialog"/> class.
    /// </returns>
    public SettingsDialog CreateSettingsDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<SettingsDialogViewModel>();
        return new SettingsDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="MiniWindow"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="MiniWindow"/> class.
    /// </returns>
    public MiniWindow CreateMiniWindow()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<MiniWindowViewModel>();
        return new MiniWindow(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </returns>
    public AtisConfigurationWindow CreateProfileConfigurationWindow()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<AtisConfigurationWindowViewModel>();
        return new AtisConfigurationWindow(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="UserInputDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="UserInputDialog"/> class.
    /// </returns>
    public UserInputDialog CreateUserInputDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<UserInputDialogViewModel>();
        return new UserInputDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NewAtisStationDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="NewAtisStationDialog"/> class.
    /// </returns>
    public NewAtisStationDialog CreateNewAtisStationDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<NewAtisStationDialogViewModel>();
        return new NewAtisStationDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="VoiceRecordAtisDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="VoiceRecordAtisDialog"/> class.
    /// </returns>
    public VoiceRecordAtisDialog CreateVoiceRecordAtisDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<VoiceRecordAtisDialogViewModel>();
        return new VoiceRecordAtisDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TransitionLevelDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="TransitionLevelDialog"/> class.
    /// </returns>
    public TransitionLevelDialog CreateTransitionLevelDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<TransitionLevelDialogViewModel>();
        return new TransitionLevelDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NewContractionDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="NewContractionDialog"/> class.
    /// </returns>
    public NewContractionDialog CreateNewContractionDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<NewContractionDialogViewModel>();
        return new NewContractionDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="StaticAirportConditionsDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="StaticAirportConditionsDialog"/> class.
    /// </returns>
    public StaticAirportConditionsDialog CreateStaticAirportConditionsDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<StaticAirportConditionsDialogViewModel>();
        return new StaticAirportConditionsDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="StaticNotamsDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="StaticNotamsDialog"/> class.
    /// </returns>
    public StaticNotamsDialog CreateStaticNotamsDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<StaticNotamsDialogViewModel>();
        return new StaticNotamsDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="StaticDefinitionEditorDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="StaticDefinitionEditorDialog"/> class.
    /// </returns>
    public StaticDefinitionEditorDialog CreateStaticDefinitionEditorDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<StaticDefinitionEditorDialogViewModel>();
        return new StaticDefinitionEditorDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SortPresetsDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="SortPresetsDialog"/> class.
    /// </returns>
    public SortPresetsDialog CreateSortPresetsDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<SortPresetsDialogViewModel>();
        return new SortPresetsDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SortAtisStationsDialog"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="SortAtisStationsDialog"/> class.
    /// </returns>
    public SortAtisStationsDialog CreateSortAtisStationsDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<SortAtisStationsDialogViewModel>();
        return new SortAtisStationsDialog(viewModel);
    }

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="ReleaseNotesDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="ReleaseNotesDialog"/> class.</returns>
    public ReleaseNotesDialog CreateReleaseNotesDialog()
    {
        var scope = _provider.CreateScope();
        var viewModel = scope.GetService<ReleaseNotesDialogViewModel>();
        return new ReleaseNotesDialog(viewModel);
    }
}
