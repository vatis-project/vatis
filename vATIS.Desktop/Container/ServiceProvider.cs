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
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Container;

/// <summary>
/// Provides a centralized service container for resolving dependencies and initializing components.
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
[Singleton<IAtisHubConnection>(Factory = nameof(CreateAtisHubConnection))]
[Transient(typeof(IWindowFactory), Factory = nameof(WindowFactory))]
[Transient(typeof(IViewModelFactory), Factory = nameof(ViewModelFactory))]
[Transient(typeof(INetworkConnectionFactory), Factory = nameof(NetworkConnectionFactory))]
[Transient(typeof(IVoiceServerConnectionFactory), Factory = nameof(VoiceServerConnectionFactory))]

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
    /// Determines whether the current environment is set to development.
    /// </summary>
    /// <returns>True if the environment is development; otherwise, false.</returns>
    public static bool IsDevelopmentEnvironment()
    {
        var environmentVariable = Environment.GetEnvironmentVariable("ENV");
        return !string.IsNullOrEmpty(environmentVariable) &&
               environmentVariable.Equals("DEV", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowFactory"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="WindowFactory"/> class.</returns>
    public IWindowFactory WindowFactory() => new WindowFactory(this);

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelFactory"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="ViewModelFactory"/> class.</returns>
    public IViewModelFactory ViewModelFactory() => new ViewModelFactory(this);

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConnectionFactory"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="NetworkConnectionFactory"/> class.</returns>
    public INetworkConnectionFactory NetworkConnectionFactory() => new NetworkConnectionFactory(this);

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceServerConnectionFactory"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="VoiceServerConnectionFactory"/> class.</returns>
    public IVoiceServerConnectionFactory VoiceServerConnectionFactory() => new VoiceServerConnectionFactory(this);

    private IMetarRepository CreateMetarRepository()
    {
        if (IsDevelopmentEnvironment())
        {
            return new MockMetarRepository(GetService<IDownloader>());
        }

        return new MetarRepository(GetService<IDownloader>(), GetService<IAppConfigurationProvider>());
    }

    private IAtisHubConnection CreateAtisHubConnection()
    {
        if (IsDevelopmentEnvironment())
        {
            return new MockAtisHubConnection(GetService<IDownloader>(), GetService<IAppConfigurationProvider>());
        }

        return new AtisHubConnection(GetService<IAppConfigurationProvider>(), GetService<IClientAuth>());
    }
}
