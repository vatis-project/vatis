using System;
using Jab;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Ui;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.Startup;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Ui.Windows;
using Vatsim.Vatis.Updates;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Voice.Network;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis;

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
[Singleton<IMetarRepository>(Factory = nameof(CreateMetarRepository))]
[Singleton<IVoiceServerConnection>(Factory = nameof(CreateVoiceServerConnection))]
[Singleton<IAtisHubConnection>(Factory = nameof(CreateAtisHubConnection))]
[Transient(typeof(IWindowFactory), Factory = nameof(WindowFactory))]
[Transient(typeof(IViewModelFactory), Factory = nameof(ViewModelFactory))]
[Transient(typeof(INetworkConnectionFactory), Factory = nameof(NetworkConnectionFactory))]
//Views
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
//ViewModels
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
    public static bool IsDevelopmentEnvironment()
    {
        var environmentVariable = Environment.GetEnvironmentVariable("ENV");
        return !string.IsNullOrEmpty(environmentVariable) &&
               environmentVariable.Equals("DEV", StringComparison.OrdinalIgnoreCase);
    }
    
    private IMetarRepository CreateMetarRepository()
    {
        if(IsDevelopmentEnvironment())
        {
            return new MockMetarRepository(GetService<IDownloader>());
        }

        return new MetarRepository(GetService<IDownloader>(), GetService<IAppConfigurationProvider>());
    }

    private IAtisHubConnection CreateAtisHubConnection()
    {
        if(IsDevelopmentEnvironment())
        {
            return new MockAtisHubConnection(GetService<IDownloader>());
        }

        return new AtisHubConnection(GetService<IAppConfigurationProvider>());
    }

    private IVoiceServerConnection CreateVoiceServerConnection()
    {
        if(IsDevelopmentEnvironment())
        {
            return new MockVoiceServerConnection();
        }

        return new VoiceServerConnection(GetService<IDownloader>());
    }

    public IWindowFactory WindowFactory() => new WindowFactory(this);
    public IViewModelFactory ViewModelFactory() => new ViewModelFactory(this);
    public INetworkConnectionFactory NetworkConnectionFactory() => new NetworkConnectionFactory(this);
}

internal class NetworkConnectionFactory : INetworkConnectionFactory
{
    private readonly ServiceProvider mProvider;

    public NetworkConnectionFactory(ServiceProvider provider)
    {
        mProvider = provider;
    }

    public INetworkConnection CreateConnection(AtisStation station)
    {
        if(ServiceProvider.IsDevelopmentEnvironment())
        {
            return new MockNetworkConnection(station, mProvider.GetService<IMetarRepository>());
        }

        return new NetworkConnection(station, mProvider.GetService<IAppConfig>(),
            mProvider.GetService<IAuthTokenManager>(), mProvider.GetService<IMetarRepository>(),
            mProvider.GetService<IDownloader>(), mProvider.GetService<INavDataRepository>());
    }
}

internal class ViewModelFactory : IViewModelFactory
{
    private readonly ServiceProvider mProvider;

    public ViewModelFactory(ServiceProvider provider)
    {
        mProvider = provider;
    }

    public AtisStationViewModel CreateAtisStationViewModel(AtisStation station)
    {
        return new AtisStationViewModel(station, mProvider.GetService<INetworkConnectionFactory>(),
            mProvider.GetService<IAppConfig>(), mProvider.GetService<IVoiceServerConnection>(),
            mProvider.GetService<IAtisBuilder>(), mProvider.GetService<IWindowFactory>(),
            mProvider.GetService<INavDataRepository>(), mProvider.GetService<IAtisHubConnection>(),
            mProvider.GetService<ISessionManager>(), mProvider.GetService<IProfileRepository>(),
            mProvider.GetService<IWebsocketService>());
    }

    public ContractionsViewModel CreateContractionsViewModel()
    {
        return new ContractionsViewModel(mProvider.GetService<IWindowFactory>(), mProvider.GetService<IAppConfig>());
    }

    public FormattingViewModel CreateFormattingViewModel()
    {
        return new FormattingViewModel(mProvider.GetService<IWindowFactory>(),
            mProvider.GetService<IProfileRepository>(),
            mProvider.GetService<ISessionManager>());
    }

    public GeneralConfigViewModel CreateGeneralConfigViewModel()
    {
        return new GeneralConfigViewModel(mProvider.GetService<IAppConfig>(),
            mProvider.GetService<ISessionManager>(),
            mProvider.GetService<IProfileRepository>());
    }

    public PresetsViewModel CreatePresetsViewModel()
    {
        return new PresetsViewModel(mProvider.GetService<IWindowFactory>(),
            mProvider.GetService<IDownloader>(),
            mProvider.GetService<IMetarRepository>(),
            mProvider.GetService<IProfileRepository>(),
            mProvider.GetService<ISessionManager>());
    }

    public SandboxViewModel CreateSandboxViewModel()
    {
        return new SandboxViewModel(mProvider.GetService<IWindowFactory>(),
            mProvider.GetService<IAtisBuilder>(),
            mProvider.GetService<IMetarRepository>(),
            mProvider.GetService<IProfileRepository>(),
            mProvider.GetService<ISessionManager>());
    }
}

internal class WindowFactory : IWindowFactory
{
    private readonly ServiceProvider mProvider;

    public WindowFactory(ServiceProvider provider)
    {
        mProvider = provider;
    }

    public MainWindow CreateMainWindow()
    {
        var viewModel = mProvider.GetService<MainWindowViewModel>();
        return new MainWindow(viewModel);
    }

    public ProfileListDialog CreateProfileListDialog()
    {
        var viewModel = mProvider.GetService<ProfileListViewModel>();
        return new ProfileListDialog(viewModel);
    }

    public SettingsDialog CreateSettingsDialog()
    {
        var viewModel = mProvider.GetService<SettingsDialogViewModel>();
        return new SettingsDialog(viewModel);
    }

    public CompactWindow CreateCompactWindow()
    {
        var viewModel = mProvider.GetService<CompactWindowViewModel>();
        return new CompactWindow(viewModel);
    }

    public AtisConfigurationWindow CreateProfileConfigurationWindow()
    {
        var viewModel = mProvider.GetService<AtisConfigurationWindowViewModel>();
        return new AtisConfigurationWindow(viewModel);
    }

    public UserInputDialog CreateUserInputDialog()
    {
        var viewModel = mProvider.GetService<UserInputDialogViewModel>();
        return new UserInputDialog(viewModel);
    }

    public NewAtisStationDialog CreateNewAtisStationDialog()
    {
        var viewModel = mProvider.GetService<NewAtisStationDialogViewModel>();
        return new NewAtisStationDialog(viewModel);
    }

    public VoiceRecordAtisDialog CreateVoiceRecordAtisDialog()
    {
        var viewModel = mProvider.GetService<VoiceRecordAtisDialogViewModel>();
        return new VoiceRecordAtisDialog(viewModel);
    }

    public TransitionLevelDialog CreateTransitionLevelDialog()
    {
        var viewModel = mProvider.GetService<TransitionLevelDialogViewModel>();
        return new TransitionLevelDialog(viewModel);
    }

    public NewContractionDialog CreateNewContractionDialog()
    {
        var viewModel = mProvider.GetService<NewContractionDialogViewModel>();
        return new NewContractionDialog(viewModel);
    }

    public StaticAirportConditionsDialog CreateStaticAirportConditionsDialog()
    {
        var viewModel = mProvider.GetService<StaticAirportConditionsDialogViewModel>();
        return new StaticAirportConditionsDialog(viewModel);
    }

    public StaticNotamsDialog CreateStaticNotamsDialog()
    {
        var viewModel = mProvider.GetService<StaticNotamsDialogViewModel>();
        return new StaticNotamsDialog(viewModel);
    }

    public StaticDefinitionEditorDialog CreateStaticDefinitionEditorDialog()
    {
        var viewModel = mProvider.GetService<StaticDefinitionEditorDialogViewModel>();
        return new StaticDefinitionEditorDialog(viewModel);
    }

    public SortPresetsDialog CreateSortPresetsDialog()
    {
        var viewModel = mProvider.GetService<SortPresetsDialogViewModel>();
        return new SortPresetsDialog(viewModel);
    }
}