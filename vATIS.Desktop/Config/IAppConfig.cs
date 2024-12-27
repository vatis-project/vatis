using Vatsim.Network;

namespace Vatsim.Vatis.Config;

public interface IAppConfig
{
    string Name { get; set; }
    string UserId { get; set; }
    string Password { get; set; }
    string PasswordDecrypted { get; set; }
    NetworkRating NetworkRating { get; set; }
    bool SuppressNotificationSound { get; set; }
    bool AlwaysOnTop { get; set; }
    bool ConfigRequired { get; }
    string? MicrophoneDevice { get; set; }
    string? PlaybackDevice { get; set; }
    WindowPosition? MainWindowPosition { get; set; }
    WindowPosition? CompactWindowPosition { get; set; }
    WindowPosition? ProfileListDialogWindowPosition { get; set; }
    void LoadConfig();
    void SaveConfig();
}
