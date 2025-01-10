using System;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using Vatsim.Network;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Ui.Common;

namespace Vatsim.Vatis.Ui.ViewModels;

public class SettingsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig _appConfig;

    private string? _name;

    private ObservableCollection<ComboBoxItemMeta>? _networkRatings;

    private string? _password;

    private string? _selectedNetworkRating;

    private bool _suppressNotificationSound;

    private string? _userId;

    public SettingsDialogViewModel(IAppConfig appConfig)
    {
        this._appConfig = appConfig;

        this.Name = this._appConfig.Name;
        this.UserId = this._appConfig.UserId;
        this.Password = this._appConfig.PasswordDecrypted;
        this.SuppressNotificationSound = this._appConfig.SuppressNotificationSound;
        this.SelectedNetworkRating = this._appConfig.NetworkRating.ToString();

        this.NetworkRatings =
        [
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
            new ComboBoxItemMeta("Administrator", "ADM")
        ];

        this.SaveSettingsCommand = ReactiveCommand.Create<ICloseable>(this.SaveSettings);
    }

    public ReactiveCommand<ICloseable, Unit> SaveSettingsCommand { get; }

    public string? Name
    {
        get => this._name;
        set => this.RaiseAndSetIfChanged(ref this._name, value);
    }

    public string? UserId
    {
        get => this._userId;
        set => this.RaiseAndSetIfChanged(ref this._userId, value);
    }

    public string? Password
    {
        get => this._password;
        set => this.RaiseAndSetIfChanged(ref this._password, value);
    }

    public string? SelectedNetworkRating
    {
        get => this._selectedNetworkRating;
        set => this.RaiseAndSetIfChanged(ref this._selectedNetworkRating, value);
    }

    public ObservableCollection<ComboBoxItemMeta>? NetworkRatings
    {
        get => this._networkRatings;
        set => this.RaiseAndSetIfChanged(ref this._networkRatings, value);
    }

    public bool SuppressNotificationSound
    {
        get => this._suppressNotificationSound;
        set => this.RaiseAndSetIfChanged(ref this._suppressNotificationSound, value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.SaveSettingsCommand.Dispose();
    }

    private void SaveSettings(ICloseable window)
    {
        this._appConfig.Name = this.Name ?? "";
        this._appConfig.UserId = this.UserId?.Trim() ?? "";
        this._appConfig.PasswordDecrypted = this.Password?.Trim() ?? "";
        this._appConfig.SuppressNotificationSound = this.SuppressNotificationSound;

        if (Enum.TryParse(this.SelectedNetworkRating, out NetworkRating selectedNetworkRating))
        {
            this._appConfig.NetworkRating = selectedNetworkRating;
        }

        this._appConfig.SaveConfig();

        MessageBus.Current.SendMessage(new GeneralSettingsUpdated());

        window.Close();
    }
}