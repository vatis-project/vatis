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
/// Factory class for creating and managing various application windows and dialogs.
/// </summary>
internal class WindowFactory : IWindowFactory
{
    private readonly ServiceProvider provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowFactory"/> class.
    /// </summary>
    /// <param name="provider">The service provider context.</param>
    public WindowFactory(ServiceProvider provider)
    {
        this.provider = provider;
    }

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="MainWindow"/> class with its associated view model.</returns>
    public MainWindow CreateMainWindow()
    {
        var viewModel = this.provider.GetService<MainWindowViewModel>();
        return new MainWindow(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ProfileListDialog"/> window.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="ProfileListDialog"/> configured with its required dependencies.
    /// </returns>
    public ProfileListDialog CreateProfileListDialog()
    {
        var viewModel = this.provider.GetService<ProfileListViewModel>();
        return new ProfileListDialog(viewModel);
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="SettingsDialog"/> class.
    /// </summary>
    /// <returns>A new <see cref="SettingsDialog"/> instance initialized with its corresponding view model.</returns>
    public SettingsDialog CreateSettingsDialog()
    {
        var viewModel = this.provider.GetService<SettingsDialogViewModel>();
        return new SettingsDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="CompactWindow"/> class.
    /// </summary>
    /// <returns>A new instance of <see cref="CompactWindow"/> initialized with the associated ViewModel.</returns>
    public CompactWindow CreateCompactWindow()
    {
        var viewModel = this.provider.GetService<CompactWindowViewModel>();
        return new CompactWindow(viewModel);
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="AtisConfigurationWindow"/> configured with its corresponding view model.</returns>
    public AtisConfigurationWindow CreateProfileConfigurationWindow()
    {
        var viewModel = this.provider.GetService<AtisConfigurationWindowViewModel>();
        return new AtisConfigurationWindow(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="UserInputDialog"/>.
    /// </summary>
    /// <returns>A new <see cref="UserInputDialog"/> object configured with its respective view model.</returns>
    public UserInputDialog CreateUserInputDialog()
    {
        var viewModel = this.provider.GetService<UserInputDialogViewModel>();
        return new UserInputDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NewAtisStationDialog"/> window.
    /// </summary>
    /// <returns>A new instance of the <see cref="NewAtisStationDialog"/> class.</returns>
    public NewAtisStationDialog CreateNewAtisStationDialog()
    {
        var viewModel = this.provider.GetService<NewAtisStationDialogViewModel>();
        return new NewAtisStationDialog(viewModel);
    }

    /// <summary>
    /// Creates an instance of the <see cref="VoiceRecordAtisDialog"/> class.
    /// </summary>
    /// <returns>A new <see cref="VoiceRecordAtisDialog"/> instance.</returns>
    public VoiceRecordAtisDialog CreateVoiceRecordAtisDialog()
    {
        var viewModel = this.provider.GetService<VoiceRecordAtisDialogViewModel>();
        return new VoiceRecordAtisDialog(viewModel);
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="TransitionLevelDialog"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="TransitionLevelDialog"/>.</returns>
    public TransitionLevelDialog CreateTransitionLevelDialog()
    {
        var viewModel = this.provider.GetService<TransitionLevelDialogViewModel>();
        return new TransitionLevelDialog(viewModel);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NewContractionDialog"/> using its associated view model.
    /// </summary>
    /// <returns>A new instance of the <see cref="NewContractionDialog"/> configured with the required view model.</returns>
    public NewContractionDialog CreateNewContractionDialog()
    {
        var viewModel = this.provider.GetService<NewContractionDialogViewModel>();
        return new NewContractionDialog(viewModel);
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="StaticAirportConditionsDialog"/> class.
    /// </summary>
    /// <returns>A newly created instance of <see cref="StaticAirportConditionsDialog"/>.</returns>
    public StaticAirportConditionsDialog CreateStaticAirportConditionsDialog()
    {
        var viewModel = this.provider.GetService<StaticAirportConditionsDialogViewModel>();
        return new StaticAirportConditionsDialog(viewModel);
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="StaticNotamsDialog"/> class.
    /// </summary>
    /// <returns>A new <see cref="StaticNotamsDialog"/> configured with its corresponding view model.</returns>
    public StaticNotamsDialog CreateStaticNotamsDialog()
    {
        var viewModel = this.provider.GetService<StaticNotamsDialogViewModel>();
        return new StaticNotamsDialog(viewModel);
    }

    /// <summary>
    /// Creates and returns an instance of the <see cref="StaticDefinitionEditorDialog"/> window.
    /// </summary>
    /// <returns>An instance of the <see cref="StaticDefinitionEditorDialog"/>.</returns>
    public StaticDefinitionEditorDialog CreateStaticDefinitionEditorDialog()
    {
        var viewModel = this.provider.GetService<StaticDefinitionEditorDialogViewModel>();
        return new StaticDefinitionEditorDialog(viewModel);
    }

    /// <summary>
    /// Creates and initializes an instance of the <see cref="SortPresetsDialog"/> class.
    /// </summary>
    /// <returns>A new <see cref="SortPresetsDialog"/> instance initialized with its required dependencies.</returns>
    public SortPresetsDialog CreateSortPresetsDialog()
    {
        var viewModel = this.provider.GetService<SortPresetsDialogViewModel>();
        return new SortPresetsDialog(viewModel);
    }
}
