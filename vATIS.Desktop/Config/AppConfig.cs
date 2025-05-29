// <copyright file="AppConfig.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vatsim.Network;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Config;

/// <summary>
/// Represents the configuration settings for the application.
/// </summary>
public class AppConfig : IAppConfig
{
    /// <inheritdoc />
    public string Name { get; set; } = "";

    /// <inheritdoc />
    public string UserId { get; set; } = "";

    /// <inheritdoc />
    public string Password { get; set; } = "";

    /// <inheritdoc />
    public NetworkRating NetworkRating { get; set; } = NetworkRating.Obs;

    /// <inheritdoc />
    public bool MuteOwnAtisUpdateSound { get; set; }

    /// <inheritdoc />
    public bool MuteSharedAtisUpdateSound { get; set; }

    /// <inheritdoc />
    public bool AlwaysOnTop { get; set; }

    /// <inheritdoc />
    public bool MiniWindowAlwaysOnTop { get; set; }

    /// <inheritdoc />
    public bool MiniWindowShowMetarDetails { get; set; }

    /// <inheritdoc />
    public WindowPosition? MainWindowPosition { get; set; }

    /// <inheritdoc />
    public WindowPosition? MiniWindowPosition { get; set; }

    /// <inheritdoc />
    public WindowPosition? ProfileListDialogWindowPosition { get; set; }

    /// <inheritdoc />
    public WindowPosition? VoiceRecordAtisDialogWindowPosition { get; set; }

    /// <inheritdoc />
    public string? MicrophoneDevice { get; set; }

    /// <inheritdoc />
    public string? PlaybackDevice { get; set; }

    /// <inheritdoc />
    public bool AutoFetchAtisLetter { get; set; }

    /// <inheritdoc />
    public bool SuppressReleaseNotes { get; set; }

    /// <inheritdoc />
    [JsonIgnore]
    public string PasswordDecrypted
    {
        get => Decrypt(Password);
        set => Password = Encrypt(value);
    }

    /// <inheritdoc />
    [JsonIgnore]
    public bool ConfigRequired => string.IsNullOrEmpty(UserId) ||
                                  string.IsNullOrEmpty(PasswordDecrypted) ||
                                  string.IsNullOrEmpty(Name);

    private static string EncryptionKey =>
        new Guid(0xb650f0bd, 0x9823, 0x46b7, 0x8e, 0xa6, 0x12, 0x8a, 0x3b, 0x2f, 0x98, 0xaf).ToString();

    /// <inheritdoc />
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
            MuteOwnAtisUpdateSound = config.MuteOwnAtisUpdateSound;
            MuteSharedAtisUpdateSound = config.MuteSharedAtisUpdateSound;
            AlwaysOnTop = config.AlwaysOnTop;
            MiniWindowAlwaysOnTop = config.MiniWindowAlwaysOnTop;
            MiniWindowShowMetarDetails = config.MiniWindowShowMetarDetails;
            MainWindowPosition = config.MainWindowPosition;
            MiniWindowPosition = config.MiniWindowPosition;
            ProfileListDialogWindowPosition = config.ProfileListDialogWindowPosition;
            VoiceRecordAtisDialogWindowPosition = config.VoiceRecordAtisDialogWindowPosition;
            MicrophoneDevice = config.MicrophoneDevice;
            PlaybackDevice = config.PlaybackDevice;
            AutoFetchAtisLetter = config.AutoFetchAtisLetter;
            SuppressReleaseNotes = config.SuppressReleaseNotes;
        }

        SaveConfig();
    }

    /// <inheritdoc />
    public void SaveConfig()
    {
        File.WriteAllText(PathProvider.AppConfigFilePath,
            JsonSerializer.Serialize(this, SourceGenerationContext.NewDefault.AppConfig));
    }

    private static string Encrypt(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

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
