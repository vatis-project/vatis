using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Vatsim.Network;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Ui.Common;

namespace Vatsim.Vatis.Ui.ViewModels;
public class SettingsDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig _appConfig;

    public ReactiveCommand<ICloseable, Unit> SaveSettingsCommand { get; }

    private string? _name;
    public string? Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string? _userId;
    public string? UserId
    {
        get => _userId;
        set => this.RaiseAndSetIfChanged(ref _userId, value);
    }

    private string? _password;
    public string? Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    private string? _selectedNetworkRating;
    public string? SelectedNetworkRating
    {
        get => _selectedNetworkRating;
        set => this.RaiseAndSetIfChanged(ref _selectedNetworkRating, value);
    }

    private ObservableCollection<ComboBoxItemMeta>? _networkRatings;
    public ObservableCollection<ComboBoxItemMeta>? NetworkRatings
    {
        get => _networkRatings;
        set => this.RaiseAndSetIfChanged(ref _networkRatings, value);
    }

    private bool _suppressNotificationSound;
    public bool SuppressNotificationSound
    {
        get => _suppressNotificationSound;
        set => this.RaiseAndSetIfChanged(ref _suppressNotificationSound, value);
    }

    public SettingsDialogViewModel(IAppConfig appConfig)
    {
        _appConfig = appConfig;

        Name = _appConfig.Name;
        UserId = _appConfig.UserId;
        Password = _appConfig.PasswordDecrypted;
        SuppressNotificationSound = _appConfig.SuppressNotificationSound;
        SelectedNetworkRating = _appConfig.NetworkRating.ToString();

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
        _appConfig.Name = Name ?? "";
        _appConfig.UserId = UserId?.Trim() ?? "";
        _appConfig.PasswordDecrypted = Password?.Trim() ?? "";
        _appConfig.SuppressNotificationSound = SuppressNotificationSound;

        if (Enum.TryParse(SelectedNetworkRating, out NetworkRating selectedNetworkRating))
        {
            _appConfig.NetworkRating = selectedNetworkRating;
        }

        _appConfig.SaveConfig();

        MessageBus.Current.SendMessage(new GeneralSettingsUpdated());

        window.Close();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        SaveSettingsCommand.Dispose();
    }
}
