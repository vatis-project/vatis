using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Sessions;

public class SessionManager : ISessionManager
{
    private MainWindow? _mainWindow;
    private ProfileListDialog? _profileListDialog;
    private readonly IWindowFactory _windowFactory;
    private readonly IProfileRepository _profileRepository;

    public SessionManager(IWindowFactory windowFactory, IProfileRepository profileRepository)
    {
        _windowFactory = windowFactory;
        _profileRepository = profileRepository;
    }

    public int MaxConnectionCount => 4;
    public int CurrentConnectionCount { get; set; }

    public void Run()
    {
        ShowProfileListDialog();
    }

    public Profile? CurrentProfile { get; private set; }

    private void ShowProfileListDialog()
    {
        _profileListDialog = _windowFactory.CreateProfileListDialog();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _profileListDialog;
        }

        _profileListDialog.Show();
    }

    public async Task StartSession(string profileId)
    {
        var profile = (await _profileRepository.LoadAll()).Find(p => p.Id == profileId);
        if (profile == null)
            return;

        _profileListDialog?.Close();
        CurrentProfile = profile;
        CurrentConnectionCount = 0;
        _mainWindow = _windowFactory.CreateMainWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _mainWindow;
        }

        _mainWindow.Show();
    }

    public void EndSession()
    {
        MessageBus.Current.SendMessage(new SessionEnded());
        CurrentProfile = null;
        CurrentConnectionCount = 0;
        _mainWindow?.Close();
        ShowProfileListDialog();
    }
}
