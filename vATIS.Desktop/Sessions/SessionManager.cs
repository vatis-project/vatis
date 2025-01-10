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
    private readonly IProfileRepository _profileRepository;
    private readonly IWindowFactory _windowFactory;
    private MainWindow? _mainWindow;
    private ProfileListDialog? _profileListDialog;

    public SessionManager(IWindowFactory windowFactory, IProfileRepository profileRepository)
    {
        this._windowFactory = windowFactory;
        this._profileRepository = profileRepository;
    }

    public int MaxConnectionCount => 4;

    public int CurrentConnectionCount { get; set; }

    public void Run()
    {
        this.ShowProfileListDialog();
    }

    public Profile? CurrentProfile { get; private set; }

    public async Task StartSession(string profileId)
    {
        var profile = (await this._profileRepository.LoadAll()).Find(p => p.Id == profileId);
        if (profile == null)
        {
            return;
        }

        this._profileListDialog?.Close();
        this.CurrentProfile = profile;
        this.CurrentConnectionCount = 0;
        this._mainWindow = this._windowFactory.CreateMainWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = this._mainWindow;
        }

        this._mainWindow.Show();
    }

    public void EndSession()
    {
        MessageBus.Current.SendMessage(new SessionEnded());
        this.CurrentProfile = null;
        this.CurrentConnectionCount = 0;
        this._mainWindow?.Close();
        this.ShowProfileListDialog();
    }

    private void ShowProfileListDialog()
    {
        this._profileListDialog = this._windowFactory.CreateProfileListDialog();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = this._profileListDialog;
        }

        this._profileListDialog.Show();
    }
}