using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui;

public interface IViewModelFactory
{
    AtisStationViewModel CreateAtisStationViewModel(AtisStation station);
    ContractionsViewModel CreateContractionsViewModel();
    FormattingViewModel CreateFormattingViewModel();
    GeneralConfigViewModel CreateGeneralConfigViewModel();
    PresetsViewModel CreatePresetsViewModel();
    SandboxViewModel CreateSandboxViewModel();
}
