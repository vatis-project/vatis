// <copyright file="ViewModelFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Container.Factory;

/// <summary>
/// Factory for creating view models.
/// </summary>
internal class ViewModelFactory : IViewModelFactory
{
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelFactory"/> class.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public ViewModelFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="AtisStationViewModel"/> class.
    /// </summary>
    /// <param name="station">The ATIS station.</param>
    /// <returns>A new <see cref="AtisStationViewModel"/> instance.</returns>
    public AtisStationViewModel CreateAtisStationViewModel(AtisStation station)
    {
        return new AtisStationViewModel(
            station,
            _provider.GetService<INetworkConnectionFactory>(),
            _provider.GetService<IVoiceServerConnectionFactory>(),
            _provider.GetService<IAppConfig>(),
            _provider.GetService<IAtisBuilder>(),
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<INavDataRepository>(),
            _provider.GetService<IAtisHubConnection>(),
            _provider.GetService<ISessionManager>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<IWebsocketService>());
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ContractionsViewModel"/> class.
    /// </summary>
    /// <returns>A new <see cref="ContractionsViewModel"/> instance.</returns>
    public ContractionsViewModel CreateContractionsViewModel()
    {
        return new ContractionsViewModel(_provider.GetService<IWindowFactory>(), _provider.GetService<IAppConfig>());
    }

    /// <summary>
    /// Creates a new instance of the <see cref="FormattingViewModel"/> class.
    /// </summary>
    /// <returns>A new <see cref="FormattingViewModel"/> instance.</returns>
    public FormattingViewModel CreateFormattingViewModel()
    {
        return new FormattingViewModel(
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>());
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="GeneralConfigViewModel"/> class.
    /// </summary>
    /// <returns>A new <see cref="GeneralConfigViewModel"/> instance.</returns>
    public GeneralConfigViewModel CreateGeneralConfigViewModel()
    {
        return new GeneralConfigViewModel(
            _provider.GetService<ISessionManager>(),
            _provider.GetService<IProfileRepository>());
    }

    /// <summary>
    /// Creates and returns an instance of the <see cref="PresetsViewModel"/> class.
    /// </summary>
    /// <returns>An instance of the <see cref="PresetsViewModel"/> class.</returns>
    public PresetsViewModel CreatePresetsViewModel()
    {
        return new PresetsViewModel(
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<IMetarRepository>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>(),
            _provider.GetService<IAtisBuilder>());
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="SandboxViewModel"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="SandboxViewModel"/> class.</returns>
    public SandboxViewModel CreateSandboxViewModel()
    {
        return new SandboxViewModel(
            _provider.GetService<IWindowFactory>(),
            _provider.GetService<IAtisBuilder>(),
            _provider.GetService<IMetarRepository>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>());
    }
}
