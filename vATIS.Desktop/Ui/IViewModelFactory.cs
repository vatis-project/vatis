// <copyright file="IViewModelFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui;

/// <summary>
/// Defines a factory for creating various view model instances used in the application.
/// </summary>
public interface IViewModelFactory
{
    /// <summary>
    /// Creates an instance of <see cref="AtisStationViewModel"/> for a given ATIS station.
    /// </summary>
    /// <param name="station">The ATIS station for which the view model is to be created.</param>
    /// <returns>An instance of <see cref="AtisStationViewModel"/> that corresponds to the given station.</returns>
    AtisStationViewModel CreateAtisStationViewModel(AtisStation station);

    /// <summary>
    /// Creates an instance of <see cref="ContractionsViewModel"/> for managing contractions in the ATIS configuration.
    /// </summary>
    /// <returns>An instance of <see cref="ContractionsViewModel"/> for modifying and interacting with contraction data.</returns>
    ContractionsViewModel CreateContractionsViewModel();

    /// <summary>
    /// Creates an instance of <see cref="FormattingViewModel"/> for formatting-related configurations.
    /// </summary>
    /// <returns>An instance of <see cref="FormattingViewModel"/>.</returns>
    FormattingViewModel CreateFormattingViewModel();

    /// <summary>
    /// Creates an instance of <see cref="GeneralConfigViewModel"/>.
    /// </summary>
    /// <returns>An instance of <see cref="GeneralConfigViewModel"/>.</returns>
    GeneralConfigViewModel CreateGeneralConfigViewModel();

    /// <summary>
    /// Creates an instance of <see cref="PresetsViewModel"/> for managing ATIS preset configurations.
    /// </summary>
    /// <returns>An instance of <see cref="PresetsViewModel"/> for interacting with ATIS presets.</returns>
    PresetsViewModel CreatePresetsViewModel();

    /// <summary>
    /// Creates an instance of <see ref="SandboxViewModel"/> for sandbox operations.
    /// </summary>
    /// <returns>A new instance of <see ref="SandboxViewModel"/>.</returns>
    SandboxViewModel CreateSandboxViewModel();
}
