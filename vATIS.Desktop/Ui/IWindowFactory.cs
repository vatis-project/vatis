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
    MainWindow CreateMainWindow();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="ProfileListDialog"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="ProfileListDialog"/> class.</returns>
    ProfileListDialog CreateProfileListDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="SettingsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="SettingsDialog"/> class.</returns>
    SettingsDialog CreateSettingsDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="CompactWindow"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="CompactWindow"/> class initialized with the associated ViewModel.</returns>
    CompactWindow CreateCompactWindow();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="AtisConfigurationWindow"/> class.</returns>
    AtisConfigurationWindow CreateProfileConfigurationWindow();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="UserInputDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="UserInputDialog"/> class.</returns>
    UserInputDialog CreateUserInputDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="NewAtisStationDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="NewAtisStationDialog"/> class.</returns>
    NewAtisStationDialog CreateNewAtisStationDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="VoiceRecordAtisDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="VoiceRecordAtisDialog"/> class.</returns>
    VoiceRecordAtisDialog CreateVoiceRecordAtisDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="TransitionLevelDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="TransitionLevelDialog"/> class.</returns>
    TransitionLevelDialog CreateTransitionLevelDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="NewContractionDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="NewContractionDialog"/> class.</returns>
    NewContractionDialog CreateNewContractionDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="StaticAirportConditionsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="StaticAirportConditionsDialog"/> class.</returns>
    StaticAirportConditionsDialog CreateStaticAirportConditionsDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="StaticNotamsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="StaticNotamsDialog"/> class.</returns>
    StaticNotamsDialog CreateStaticNotamsDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="StaticDefinitionEditorDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="StaticDefinitionEditorDialog"/> class.</returns>
    StaticDefinitionEditorDialog CreateStaticDefinitionEditorDialog();

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="SortPresetsDialog"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="SortPresetsDialog"/> class.</returns>
    SortPresetsDialog CreateSortPresetsDialog();
}
