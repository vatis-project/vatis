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
    private MainWindow? mMainWindow;
    private ProfileListDialog? mProfileListDialog;
    private readonly IWindowFactory mWindowFactory;
    private readonly IProfileRepository mProfileRepository;

    public SessionManager(IWindowFactory windowFactory, IProfileRepository profileRepository)
    {
        mWindowFactory = windowFactory;
        mProfileRepository = profileRepository;
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
        mProfileListDialog = mWindowFactory.CreateProfileListDialog();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mProfileListDialog;
        }

        mProfileListDialog.Show();
    }

    public async Task StartSession(string profileId)
    {
        var profile = (await mProfileRepository.LoadAll()).Find(p => p.Id == profileId);
        if (profile == null)
            return;
        
        mProfileListDialog?.Close();
        CurrentProfile = profile;
        CurrentConnectionCount = 0;
        mMainWindow = mWindowFactory.CreateMainWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mMainWindow;
        }

        mMainWindow.Show();
    }

    public void EndSession()
    {
        MessageBus.Current.SendMessage(new SessionEnded());
        CurrentProfile = null;
        CurrentConnectionCount = 0;
        mMainWindow?.Close();
        ShowProfileListDialog();
    }
}