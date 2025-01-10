// <copyright file="ViewModelFactory.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;
using Vatsim.Vatis.Voice.Network;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Container.Factory;

/// <summary>
/// Factory responsible for creating instances of various ViewModel classes used in the application.
/// </summary>
internal class ViewModelFactory : IViewModelFactory
{
    private readonly ServiceProvider provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelFactory"/> class.
    /// </summary>
    /// <param name="provider">The service provider context.</param>
    public ViewModelFactory(ServiceProvider provider)
    {
        this.provider = provider;
    }

    /// <summary>
    /// Creates an instance of the <see cref="AtisStationViewModel"/> class using the specified <see cref="AtisStation"/> and required services.
    /// </summary>
    /// <param name="station">The <see cref="AtisStation"/> instance to associate with the created view model.</param>
    /// <returns>An instance of <see cref="AtisStationViewModel"/> configured with the provided station and necessary dependencies.</returns>
    public AtisStationViewModel CreateAtisStationViewModel(AtisStation station)
    {
        return new AtisStationViewModel(
            station,
            this.provider.GetService<INetworkConnectionFactory>(),
            this.provider.GetService<IAppConfig>(),
            this.provider.GetService<IVoiceServerConnection>(),
            this.provider.GetService<IAtisBuilder>(),
            this.provider.GetService<IWindowFactory>(),
            this.provider.GetService<INavDataRepository>(),
            this.provider.GetService<IAtisHubConnection>(),
            this.provider.GetService<ISessionManager>(),
            this.provider.GetService<IProfileRepository>(),
            this.provider.GetService<IWebsocketService>());
    }

    /// <summary>
    /// Creates and returns an instance of <see cref="ContractionsViewModel"/>.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="ContractionsViewModel"/> initialized with necessary dependencies.
    /// </returns>
    public ContractionsViewModel CreateContractionsViewModel()
    {
        return new ContractionsViewModel(
            this.provider.GetService<IWindowFactory>(),
            this.provider.GetService<IAppConfig>());
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="FormattingViewModel"/> class.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="FormattingViewModel"/> configured with the required services.
    /// </returns>
    public FormattingViewModel CreateFormattingViewModel()
    {
        return new FormattingViewModel(
            this.provider.GetService<IWindowFactory>(),
            this.provider.GetService<IProfileRepository>(),
            this.provider.GetService<ISessionManager>());
    }

    /// <summary>
    /// Creates and returns a new instance of the <see cref="GeneralConfigViewModel"/> class.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="GeneralConfigViewModel"/> configured with required dependencies.
    /// </returns>
    public GeneralConfigViewModel CreateGeneralConfigViewModel()
    {
        return new GeneralConfigViewModel(
            this.provider.GetService<ISessionManager>(),
            this.provider.GetService<IProfileRepository>());
    }

    /// <summary>
    /// Creates and returns an instance of the <see cref="PresetsViewModel"/> class.
    /// </summary>
    /// <returns>
    /// A new instance of the <see cref="PresetsViewModel"/> class, initialized with dependencies.
    /// </returns>
    public PresetsViewModel CreatePresetsViewModel()
    {
        return new PresetsViewModel(
            this.provider.GetService<IWindowFactory>(),
            this.provider.GetService<IDownloader>(),
            this.provider.GetService<IMetarRepository>(),
            this.provider.GetService<IProfileRepository>(),
            this.provider.GetService<ISessionManager>());
    }

    /// <summary>
    /// Creates a new instance of <see cref="SandboxViewModel"/>.
    /// </summary>
    /// <returns>
    /// An instance of the <see cref="SandboxViewModel"/> class initialized with dependencies.
    /// </returns>
    public SandboxViewModel CreateSandboxViewModel()
    {
        return new SandboxViewModel(
            this.provider.GetService<IWindowFactory>(),
            this.provider.GetService<IAtisBuilder>(),
            this.provider.GetService<IMetarRepository>(),
            this.provider.GetService<IProfileRepository>(),
            this.provider.GetService<ISessionManager>());
    }
}
