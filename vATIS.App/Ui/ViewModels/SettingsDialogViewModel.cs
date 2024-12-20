using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Vatsim.Network;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Ui.Common;

namespace Vatsim.Vatis.Ui.ViewModels;
public class SettingsDialogViewModel : ReactiveViewModelBase
{
    private readonly IAppConfig mAppConfig;

    public ReactiveCommand<ICloseable, Unit> SaveSettingsCommand { get; private set; }

    private string? mName;
    public string? Name
    {
        get => mName;
        set => this.RaiseAndSetIfChanged(ref mName, value);
    }

    private string? mUserId;
    public string? UserId
    {
        get => mUserId;
        set => this.RaiseAndSetIfChanged(ref mUserId, value);
    }

    private string? mPassword;
    public string? Password
    {
        get => mPassword;
        set => this.RaiseAndSetIfChanged(ref mPassword, value);
    }

    private string? mSelectedNetworkRating;
    public string? SelectedNetworkRating
    {
        get => mSelectedNetworkRating;
        set => this.RaiseAndSetIfChanged(ref mSelectedNetworkRating, value);
    }

    private ObservableCollection<ComboBoxItemMeta>? mNetworkRatings;
    public ObservableCollection<ComboBoxItemMeta>? NetworkRatings
    {
        get => mNetworkRatings;
        set => this.RaiseAndSetIfChanged(ref mNetworkRatings, value);
    }

    private bool mSuppressNotificationSound;
    public bool SuppressNotificationSound
    {
        get => mSuppressNotificationSound;
        set => this.RaiseAndSetIfChanged(ref mSuppressNotificationSound, value);
    }

    public SettingsDialogViewModel(IAppConfig appConfig)
    {
        mAppConfig = appConfig;

        Name = mAppConfig.Name;
        UserId = mAppConfig.UserId;
        Password = mAppConfig.PasswordDecrypted;
        SuppressNotificationSound = mAppConfig.SuppressNotificationSound;
        SelectedNetworkRating = mAppConfig.NetworkRating.ToString();

        NetworkRatings = [
            new ComboBoxItemMeta("Observer", "OBS"),
            new ComboBoxItemMeta("Student 1", "S1"),
            new ComboBoxItemMeta("Student 2", "S2"),
            new ComboBoxItemMeta("Student 3", "S3"),
            new ComboBoxItemMeta("Controller 1", "C1"),
            new ComboBoxItemMeta("Controller 2", "C2"),
            new ComboBoxItemMeta("Controller 3", "C3"),
            new ComboBoxItemMeta("Instructor 1", "I1"),
            new ComboBoxItemMeta("Instructor 2", "I2"),
            new ComboBoxItemMeta("Instructor 3", "I3"),
            new ComboBoxItemMeta("Supervisor", "SUP"),
            new ComboBoxItemMeta("Administrator", "ADM"),
        ];

        SaveSettingsCommand = ReactiveCommand.Create<ICloseable>(SaveSettings);
    }

    private void SaveSettings(ICloseable window)
    {
        mAppConfig.Name = Name ?? "";
        mAppConfig.UserId = UserId?.Trim() ?? "";
        mAppConfig.PasswordDecrypted = Password?.Trim() ?? "";
        mAppConfig.SuppressNotificationSound = SuppressNotificationSound;

        if (Enum.TryParse(SelectedNetworkRating, out NetworkRating selectedNetworkRating))
        {
            mAppConfig.NetworkRating = selectedNetworkRating;
        }

        mAppConfig.SaveConfig();
        
        MessageBus.Current.SendMessage(new GeneralSettingsUpdated());

        window.Close();
    }
}
