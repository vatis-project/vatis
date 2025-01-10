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