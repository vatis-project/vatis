// <copyright file="IViewModelFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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
