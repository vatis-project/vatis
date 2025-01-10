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

    public NetworkRating NetworkRating { get; set; } = NetworkRating.Obs;

    public bool SuppressNotificationSound { get; set; }

    public bool AlwaysOnTop { get; set; }

    public bool CompactWindowAlwaysOnTop { get; set; }

    public WindowPosition? MainWindowPosition { get; set; }

    public WindowPosition? CompactWindowPosition { get; set; }

    public WindowPosition? ProfileListDialogWindowPosition { get; set; }

    public WindowPosition? VoiceRecordAtisDialogWindowPosition { get; set; }

    public string? MicrophoneDevice { get; set; }

    public string? PlaybackDevice { get; set; }

    [JsonIgnore]
    public string PasswordDecrypted
    {
        get => Decrypt(this.Password);
        set => this.Password = Encrypt(value);
    }

    [JsonIgnore]
    public bool ConfigRequired => string.IsNullOrEmpty(this.UserId) ||
                                  string.IsNullOrEmpty(this.PasswordDecrypted) ||
                                  string.IsNullOrEmpty(this.Name);

    public void LoadConfig()
    {
        using var fs = new FileStream(
            PathProvider.AppConfigFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        var config = JsonSerializer.Deserialize(sr.ReadToEnd(), SourceGenerationContext.NewDefault.AppConfig);
        if (config != null)
        {
            this.Name = config.Name;
            this.UserId = config.UserId;
            this.Password = config.Password;
            this.NetworkRating = config.NetworkRating;
            this.SuppressNotificationSound = config.SuppressNotificationSound;
            this.AlwaysOnTop = config.AlwaysOnTop;
            this.CompactWindowAlwaysOnTop = config.CompactWindowAlwaysOnTop;
            this.MainWindowPosition = config.MainWindowPosition;
            this.CompactWindowPosition = config.CompactWindowPosition;
            this.ProfileListDialogWindowPosition = config.ProfileListDialogWindowPosition;
            this.VoiceRecordAtisDialogWindowPosition = config.VoiceRecordAtisDialogWindowPosition;
            this.MicrophoneDevice = config.MicrophoneDevice;
            this.PlaybackDevice = config.PlaybackDevice;
        }

        this.SaveConfig();
    }

    public void SaveConfig()
    {
        File.WriteAllText(
            PathProvider.AppConfigFilePath,
            JsonSerializer.Serialize(this, SourceGenerationContext.NewDefault.AppConfig));
    }

    private static string Encrypt(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var cryptoProvider = TripleDES.Create();

        var byteHash = MD5.HashData(Encoding.UTF8.GetBytes(EncryptionKey));
        cryptoProvider.Key = byteHash;
        cryptoProvider.Mode = CipherMode.ECB;
        var byteBuff = Encoding.UTF8.GetBytes(value);

        return Convert.ToBase64String(
            cryptoProvider.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
    }

    private static string Decrypt(string value)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            var cryptoProvider = TripleDES.Create();

            var byteHash = MD5.HashData(Encoding.UTF8.GetBytes(EncryptionKey));
            cryptoProvider.Key = byteHash;
            cryptoProvider.Mode = CipherMode.ECB;
            var byteBuff = Convert.FromBase64String(value);

            return Encoding.UTF8.GetString(
                cryptoProvider.CreateDecryptor()
                    .TransformFinalBlock(byteBuff, 0, byteBuff.Length));
        }
        catch
        {
            return "";
        }
    }
}