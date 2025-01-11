// <copyright file="IWindowFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Ui;

public interface IWindowFactory
{
    MainWindow CreateMainWindow();
    ProfileListDialog CreateProfileListDialog();
    SettingsDialog CreateSettingsDialog();
    CompactWindow CreateCompactWindow();
    AtisConfigurationWindow CreateProfileConfigurationWindow();
    UserInputDialog CreateUserInputDialog();
    NewAtisStationDialog CreateNewAtisStationDialog();
    VoiceRecordAtisDialog CreateVoiceRecordAtisDialog();
    TransitionLevelDialog CreateTransitionLevelDialog();
    NewContractionDialog CreateNewContractionDialog();
    StaticAirportConditionsDialog CreateStaticAirportConditionsDialog();
    StaticNotamsDialog CreateStaticNotamsDialog();
    StaticDefinitionEditorDialog CreateStaticDefinitionEditorDialog();
    SortPresetsDialog CreateSortPresetsDialog();
}
