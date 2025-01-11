// <copyright file="ServiceProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Jab;
using Vatsim.Network;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Container.Factory;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Ui;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Ui.Startup;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;
using Vatsim.Vatis.Ui.Windows;
using Vatsim.Vatis.Updates;
using Vatsim.Vatis.Voice.Network;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Container;

/// <summary>
/// Dependency Injection Service Provider for managing service lifetimes and instantiations.
/// </summary>
[ServiceProvider]
[Singleton(typeof(IAppConfigurationProvider), typeof(AppConfigurationProvider))]
[Singleton(typeof(IAppConfig), typeof(AppConfig))]
[Singleton(typeof(IClientUpdater), typeof(ClientUpdater))]
[Singleton(typeof(IDownloader), typeof(Downloader))]
[Singleton(typeof(ISessionManager), typeof(SessionManager))]
[Singleton(typeof(IAuthTokenManager), typeof(AuthTokenManager))]
[Singleton(typeof(INavDataRepository), typeof(NavDataRepository))]
[Singleton(typeof(ITextToSpeechService), typeof(TextToSpeechService))]
[Singleton(typeof(IAtisBuilder), typeof(AtisBuilder))]
[Singleton(typeof(IWindowLocationService), typeof(WindowLocationService))]
[Singleton(typeof(IProfileRepository), typeof(ProfileRepository))]
[Singleton(typeof(IWebsocketService), typeof(WebsocketService))]
[Singleton(typeof(IClientAuth), typeof(ClientAuth))]
[Singleton<IMetarRepository>(Factory = nameof(CreateMetarRepository))]
[Singleton<IVoiceServerConnection>(Factory = nameof(CreateVoiceServerConnection))]
[Singleton<IAtisHubConnection>(Factory = nameof(CreateAtisHubConnection))]
[Transient(typeof(IWindowFactory), Factory = nameof(WindowFactory))]
[Transient(typeof(IViewModelFactory), Factory = nameof(ViewModelFactory))]
[Transient(typeof(INetworkConnectionFactory), Factory = nameof(NetworkConnectionFactory))]

// Views
[Transient(typeof(MainWindow))]
[Transient(typeof(CompactWindow))]
[Transient(typeof(AtisConfigurationWindow))]
[Transient(typeof(StartupWindow))]
[Transient(typeof(ProfileListDialog))]
[Transient(typeof(NewAtisStationDialog))]
[Transient(typeof(TransitionLevelDialog))]
[Transient(typeof(NewContractionDialog))]
[Transient(typeof(StaticAirportConditionsDialog))]
[Transient(typeof(StaticNotamsDialog))]
[Transient(typeof(StaticDefinitionEditorDialog))]
[Transient(typeof(SettingsDialog))]
[Transient(typeof(UserInputDialog))]
[Transient(typeof(MessageBoxView))]
[Transient(typeof(SortPresetsDialog))]

// ViewModels
[Transient(typeof(ProfileListViewModel))]
[Transient(typeof(StartupWindowViewModel))]
[Transient(typeof(CompactViewItemViewModel))]
[Transient(typeof(CompactWindowViewModel))]
[Transient(typeof(MainWindowViewModel))]
[Transient(typeof(AtisConfigurationWindowViewModel))]
[Transient(typeof(NewAtisStationDialogViewModel))]
[Transient(typeof(TransitionLevelDialogViewModel))]
[Transient(typeof(NewContractionDialogViewModel))]
[Transient(typeof(StaticAirportConditionsDialogViewModel))]
[Transient(typeof(StaticNotamsDialogViewModel))]
[Transient(typeof(StaticDefinitionEditorDialogViewModel))]
[Transient(typeof(SettingsDialogViewModel))]
[Transient(typeof(UserInputDialogViewModel))]
[Transient(typeof(MessageBoxViewModel))]
[Transient(typeof(VoiceRecordAtisDialogViewModel))]
[Transient(typeof(SortPresetsDialogViewModel))]
[Transient(typeof(ContractionsViewModel))]
[Transient(typeof(FormattingViewModel))]
[Transient(typeof(GeneralConfigViewModel))]
[Transient(typeof(PresetsViewModel))]
[Transient(typeof(SandboxViewModel))]
internal sealed partial class ServiceProvider
{
    /// <summary>
    /// Provides a factory implementation for creating window instances within the application.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IWindowFactory"/> configured using the current service provider.
    /// </returns>
    public IWindowFactory WindowFactory()
    {
        return new WindowFactory(this);
    }

    /// <summary>
    /// Creates and provides instances of view models for use in the application.
    /// </summary>
    /// <returns>
    /// An instance implementing <see cref="IViewModelFactory"/> that is initialized with the current service provider context.
    /// </returns>
    public IViewModelFactory ViewModelFactory()
    {
        return new ViewModelFactory(this);
    }

    /// <summary>
    /// Creates and provides an instance of the network connection factory used for managing network connections.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="INetworkConnectionFactory"/> to facilitate network connection operations.
    /// </returns>
    public INetworkConnectionFactory NetworkConnectionFactory()
    {
        return new NetworkConnectionFactory(this);
    }

    /// <summary>
    /// Determines if the application is running in a development environment.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the current environment is set to "DEV".
    /// </returns>
    internal static bool IsDevelopmentEnvironment()
    {
        var environmentVariable = Environment.GetEnvironmentVariable("ENV");
        return !string.IsNullOrEmpty(environmentVariable) &&
               environmentVariable.Equals("DEV", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates and initializes an instance of a METAR repository.
    /// </summary>
    /// <returns>
    /// An object that implements the <see cref="IMetarRepository"/> interface,
    /// providing methods related to METAR data management.
    /// </returns>
    private IMetarRepository CreateMetarRepository()
    {
        if (IsDevelopmentEnvironment())
        {
            return new MockMetarRepository(this.GetService<IDownloader>());
        }

        return new MetarRepository(this.GetService<IDownloader>(), this.GetService<IAppConfigurationProvider>());
    }

    /// <summary>
    /// Creates and initializes an instance of <see cref="IAtisHubConnection"/> for managing a connection to the ATIS Hub.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IAtisHubConnection"/> used for communicating with the ATIS Hub.
    /// </returns>
    private IAtisHubConnection CreateAtisHubConnection()
    {
        if (IsDevelopmentEnvironment())
        {
            return new MockAtisHubConnection();
        }

        return new AtisHubConnection(this.GetService<IAppConfigurationProvider>(), this.GetService<IClientAuth>());
    }

    /// <summary>
    /// Creates and configures a connection to the VATSIM voice server.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IVoiceServerConnection"/> used to manage the connection to the voice server.
    /// </returns>
    private IVoiceServerConnection CreateVoiceServerConnection()
    {
        if (IsDevelopmentEnvironment())
        {
            return new MockVoiceServerConnection();
        }

        return new VoiceServerConnection(this.GetService<IDownloader>());
    }
}
