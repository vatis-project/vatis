using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vatsim.Network;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Config;

public class AppConfig : IAppConfig
{
    private static string EncryptionKey =>
        new Guid(0xb650f0bd, 0x9823, 0x46b7, 0x8e, 0xa6, 0x12, 0x8a, 0x3b, 0x2f, 0x98, 0xaf).ToString();

    public string Name { get; set; } = "";

    public string UserId { get; set; } = "";

    public string Password { get; set; } = "";

    public NetworkRating NetworkRating { get; set; } = NetworkRating.OBS;

    public bool SuppressNotificationSound { get; set; }

    public bool AlwaysOnTop { get; set; }

    public WindowPosition? MainWindowPosition { get; set; }

    public WindowPosition? CompactWindowPosition { get; set; }

    public WindowPosition? VoiceRecordAtisDialogWindowPosition { get; set; }

    public string? MicrophoneDevice { get; set; }

    public string? PlaybackDevice { get; set; }

    [JsonIgnore]
    public string PasswordDecrypted
    {
        get => Decrypt(Password);
        set => Password = Encrypt(value);
    }

    [JsonIgnore]
    public bool ConfigRequired => string.IsNullOrEmpty(UserId) ||
                                  string.IsNullOrEmpty(PasswordDecrypted) ||
                                  string.IsNullOrEmpty(Name);

    public void LoadConfig()
    {
        using var fs = new FileStream(PathProvider.AppConfigFilePath, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        var config = JsonSerializer.Deserialize(sr.ReadToEnd(), SourceGenerationContext.NewDefault.AppConfig);
        if (config != null)
        {
            Name = config.Name;
            UserId = config.UserId;
            Password = config.Password;
            NetworkRating = config.NetworkRating;
            SuppressNotificationSound = config.SuppressNotificationSound;
            AlwaysOnTop = config.AlwaysOnTop;
            MainWindowPosition = config.MainWindowPosition;
            CompactWindowPosition = config.CompactWindowPosition;
            VoiceRecordAtisDialogWindowPosition = config.VoiceRecordAtisDialogWindowPosition;
            MicrophoneDevice = config.MicrophoneDevice;
            PlaybackDevice = config.PlaybackDevice;
        }

        SaveConfig();
    }

    public void SaveConfig()
    {
        File.WriteAllText(PathProvider.AppConfigFilePath,
            JsonSerializer.Serialize(this, SourceGenerationContext.NewDefault.AppConfig));
    }

    private static string Encrypt(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var cyrptoProvider = TripleDES.Create();

        var byteHash = MD5.HashData(Encoding.UTF8.GetBytes(EncryptionKey));
        cyrptoProvider.Key = byteHash;
        cyrptoProvider.Mode = CipherMode.ECB;
        var byteBuff = Encoding.UTF8.GetBytes(value);

        return Convert.ToBase64String(
            cyrptoProvider.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
    }

    private static string Decrypt(string value)
    {
        try
        {
            if (string.IsNullOrEmpty(value)) return "";

            var cryptoProvider = TripleDES.Create();

            var byteHash = MD5.HashData(Encoding.UTF8.GetBytes(EncryptionKey));
            cryptoProvider.Key = byteHash;
            cryptoProvider.Mode = CipherMode.ECB;
            var byteBuff = Convert.FromBase64String(value);

            return Encoding.UTF8.GetString(cryptoProvider.CreateDecryptor()
                .TransformFinalBlock(byteBuff, 0, byteBuff.Length));
        }
        catch
        {
            return "";
        }
    }
}