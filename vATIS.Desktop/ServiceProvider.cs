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
    private readonly ServiceProvider _provider;

    public NetworkConnectionFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    public INetworkConnection CreateConnection(AtisStation station)
    {
        if(ServiceProvider.IsDevelopmentEnvironment())
        {
            return new MockNetworkConnection(station, _provider.GetService<IMetarRepository>());
        }

        return new NetworkConnection(station, _provider.GetService<IAppConfig>(),
            _provider.GetService<IAuthTokenManager>(), _provider.GetService<IMetarRepository>(),
            _provider.GetService<IDownloader>(), _provider.GetService<INavDataRepository>());
    }
}

internal class ViewModelFactory : IViewModelFactory
{
    private readonly ServiceProvider _provider;

    public ViewModelFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    public AtisStationViewModel CreateAtisStationViewModel(AtisStation station)
    {
        return new AtisStationViewModel(station, _provider.GetService<INetworkConnectionFactory>(),
            _provider.GetService<IAppConfig>(), _provider.GetService<IVoiceServerConnection>(),
            _provider.GetService<IAtisBuilder>(), _provider.GetService<IWindowFactory>(),
            _provider.GetService<INavDataRepository>(), _provider.GetService<IAtisHubConnection>(),
            _provider.GetService<ISessionManager>(), _provider.GetService<IProfileRepository>(),
            _provider.GetService<IWebsocketService>());
    }

    public ContractionsViewModel CreateContractionsViewModel()
    {
        return new ContractionsViewModel(_provider.GetService<IWindowFactory>(), _provider.GetService<IAppConfig>());
    }

    public FormattingViewModel CreateFormattingViewModel()
    {
        return new FormattingViewModel(_provider.GetService<IWindowFactory>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>());
    }

    public GeneralConfigViewModel CreateGeneralConfigViewModel()
    {
        return new GeneralConfigViewModel(_provider.GetService<ISessionManager>(),
            _provider.GetService<IProfileRepository>());
    }

    public PresetsViewModel CreatePresetsViewModel()
    {
        return new PresetsViewModel(_provider.GetService<IWindowFactory>(),
            _provider.GetService<IDownloader>(),
            _provider.GetService<IMetarRepository>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>());
    }

    public SandboxViewModel CreateSandboxViewModel()
    {
        return new SandboxViewModel(_provider.GetService<IWindowFactory>(),
            _provider.GetService<IAtisBuilder>(),
            _provider.GetService<IMetarRepository>(),
            _provider.GetService<IProfileRepository>(),
            _provider.GetService<ISessionManager>());
    }
}

internal class WindowFactory : IWindowFactory
{
    private readonly ServiceProvider _provider;

    public WindowFactory(ServiceProvider provider)
    {
        _provider = provider;
    }

    public MainWindow CreateMainWindow()
    {
        var viewModel = _provider.GetService<MainWindowViewModel>();
        return new MainWindow(viewModel);
    }

    public ProfileListDialog CreateProfileListDialog()
    {
        var viewModel = _provider.GetService<ProfileListViewModel>();
        return new ProfileListDialog(viewModel);
    }

    public SettingsDialog CreateSettingsDialog()
    {
        var viewModel = _provider.GetService<SettingsDialogViewModel>();
        return new SettingsDialog(viewModel);
    }

    public CompactWindow CreateCompactWindow()
    {
        var viewModel = _provider.GetService<CompactWindowViewModel>();
        return new CompactWindow(viewModel);
    }

    public AtisConfigurationWindow CreateProfileConfigurationWindow()
    {
        var viewModel = _provider.GetService<AtisConfigurationWindowViewModel>();
        return new AtisConfigurationWindow(viewModel);
    }

    public UserInputDialog CreateUserInputDialog()
    {
        var viewModel = _provider.GetService<UserInputDialogViewModel>();
        return new UserInputDialog(viewModel);
    }

    public NewAtisStationDialog CreateNewAtisStationDialog()
    {
        var viewModel = _provider.GetService<NewAtisStationDialogViewModel>();
        return new NewAtisStationDialog(viewModel);
    }

    public VoiceRecordAtisDialog CreateVoiceRecordAtisDialog()
    {
        var viewModel = _provider.GetService<VoiceRecordAtisDialogViewModel>();
        return new VoiceRecordAtisDialog(viewModel);
    }

    public TransitionLevelDialog CreateTransitionLevelDialog()
    {
        var viewModel = _provider.GetService<TransitionLevelDialogViewModel>();
        return new TransitionLevelDialog(viewModel);
    }

    public NewContractionDialog CreateNewContractionDialog()
    {
        var viewModel = _provider.GetService<NewContractionDialogViewModel>();
        return new NewContractionDialog(viewModel);
    }

    public StaticAirportConditionsDialog CreateStaticAirportConditionsDialog()
    {
        var viewModel = _provider.GetService<StaticAirportConditionsDialogViewModel>();
        return new StaticAirportConditionsDialog(viewModel);
    }

    public StaticNotamsDialog CreateStaticNotamsDialog()
    {
        var viewModel = _provider.GetService<StaticNotamsDialogViewModel>();
        return new StaticNotamsDialog(viewModel);
    }

    public StaticDefinitionEditorDialog CreateStaticDefinitionEditorDialog()
    {
        var viewModel = _provider.GetService<StaticDefinitionEditorDialogViewModel>();
        return new StaticDefinitionEditorDialog(viewModel);
    }

    public SortPresetsDialog CreateSortPresetsDialog()
    {
        var viewModel = _provider.GetService<SortPresetsDialogViewModel>();
        return new SortPresetsDialog(viewModel);
    }
}
