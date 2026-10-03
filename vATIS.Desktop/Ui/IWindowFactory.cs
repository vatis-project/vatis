// <copyright file="IWindowFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Ui;

/// <summary>
/// Defines a factory interface for creating various UI windows and dialogs within the application.
/// </summary>
public interface IWindowFactory
{
    /// <summary>
    /// Creates and initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="MainWindow"/> class.</returns>
    public MainWindow CreateMainWindow();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="ProfileListDialog"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="ProfileListDialog"/> class.</returns>
    public ProfileListDialog CreateProfileListDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="SettingsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="SettingsDialog"/> class.</returns>
    public SettingsDialog CreateSettingsDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="MiniWindow"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="MiniWindow"/> class initialized with the associated ViewModel.</returns>
    public MiniWindow CreateMiniWindow();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="AtisConfigurationWindow"/> class.</returns>
    public AtisConfigurationWindow CreateProfileConfigurationWindow();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="UserInputDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="UserInputDialog"/> class.</returns>
    public UserInputDialog CreateUserInputDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="NewAtisStationDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="NewAtisStationDialog"/> class.</returns>
    public NewAtisStationDialog CreateNewAtisStationDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="VoiceRecordAtisDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="VoiceRecordAtisDialog"/> class.</returns>
    public VoiceRecordAtisDialog CreateVoiceRecordAtisDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="TransitionLevelDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="TransitionLevelDialog"/> class.</returns>
    public TransitionLevelDialog CreateTransitionLevelDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="NewContractionDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="NewContractionDialog"/> class.</returns>
    public NewContractionDialog CreateNewContractionDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="StaticAirportConditionsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="StaticAirportConditionsDialog"/> class.</returns>
    public StaticAirportConditionsDialog CreateStaticAirportConditionsDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="StaticNotamsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="StaticNotamsDialog"/> class.</returns>
    public StaticNotamsDialog CreateStaticNotamsDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="StaticDefinitionEditorDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="StaticDefinitionEditorDialog"/> class.</returns>
    public StaticDefinitionEditorDialog CreateStaticDefinitionEditorDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="SortPresetsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="SortPresetsDialog"/> class.</returns>
    public SortPresetsDialog CreateSortPresetsDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="SortAtisStationsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="SortAtisStationsDialog"/> class.</returns>
    public SortAtisStationsDialog CreateSortAtisStationsDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="ReleaseNotesDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="ReleaseNotesDialog"/> class.</returns>
    public ReleaseNotesDialog CreateReleaseNotesDialog();
}
