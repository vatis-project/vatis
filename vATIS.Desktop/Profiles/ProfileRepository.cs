// <copyright file="ProfileRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Voice.WavPack;

namespace Vatsim.Vatis.Profiles;

/// <inheritdoc />
public class ProfileRepository : IProfileRepository
{
    private readonly IDownloader _downloader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileRepository"/> class.
    /// </summary>
    /// <param name="downloader">The downloader instance used to facilitate downloading operations.</param>
    public ProfileRepository(IDownloader downloader)
    {
        _downloader = downloader;
        EnsureProfilesFolderExists();
    }

    /// <inheritdoc />
    public async Task CheckForProfileUpdates()
    {
        var profiles = await LoadAll();
        await Task.WhenAll(profiles.Select(p => CheckForProfileUpdate(p)));
    }

    /// <inheritdoc />
    public async Task<Profile?> LoadById(string profileId)
    {
        var path = PathProvider.GetProfilePath(profileId);
        if (File.Exists(path))
        {
            try
            {
                var profile = await LoadAndMigrate(path);
                if (profile.Id == profileId)
                    return profile;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to deserialize profile from path: " + path);
            }
        }

        // The file name did not match the profile id (or could not be read); fall back to scanning every profile.
        return (await LoadAll()).Find(p => p.Id == profileId);
    }

    /// <inheritdoc />
    public void Save(Profile profile)
    {
        var path = PathProvider.GetProfilePath(profile.Id);
        Log.Information($"Saving Profile {profile.Name} to {path}");
        File.WriteAllText(path, JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile));
    }

    /// <inheritdoc />
    public async Task Rename(string profileId, string newName)
    {
        var profile = await Load(PathProvider.GetProfilePath(profileId));
        profile.Name = newName;
        profile.IsNameOverridden = true;
        Save(profile);
    }

    /// <inheritdoc />
    public async Task<List<Profile>> LoadAll()
    {
        var paths = Directory.GetFiles(PathProvider.ProfilesFolderPath, "*.json");

        // Reading and deserializing the (large) profile files is independent per file, so do it concurrently.
        var loaded = await Task.WhenAll(paths.Select(async path =>
        {
            try
            {
                return await LoadAndMigrate(path);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to deserialize profile from path: " + path);
                return null;
            }
        }));

        return loaded.Where(p => p != null).Select(p => p!).ToList();
    }

    /// <inheritdoc />
    public async Task<Profile> Copy(Profile profile)
    {
        var profiles = await LoadAll();
        var newName = CreateCopyName(profile.Name, profiles.Select(p => p.Name).ToArray());
        var newProfile = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile),
            SourceGenerationContext.NewDefault.Profile) ?? throw new JsonException("Result is null");
        newProfile.Id = Guid.NewGuid().ToString();
        newProfile.Name = newName;
        Log.Information($"Copying profile {profile.Name} to {newProfile.Name}");
        Save(newProfile);
        return newProfile;
    }

    /// <inheritdoc />
    public void Delete(Profile profile)
    {
        var path = PathProvider.GetProfilePath(profile.Id);
        Log.Information($"Deleting profile {profile.Name} from {path}");
        File.Delete(path);
    }

    /// <inheritdoc />
    public async Task<Profile> Import(string path)
    {
        var profile = IsBundle(path) ? ReadBundle(path) : await Load(path);
        Log.Information($"Importing profile {profile.Name}");
        profile.Id = Guid.NewGuid().ToString();
        Save(profile);
        return profile;
    }

    /// <inheritdoc />
    public async Task<Profile> ImportWithId(string path)
    {
        var profile = await Load(path);
        if (string.IsNullOrWhiteSpace(profile.Id) || !Guid.TryParse(profile.Id, out _))
        {
            profile.Id = Guid.NewGuid().ToString();
        }

        Log.Information($"Importing profile {profile.Name} ({profile.Id})");
        Save(profile);
        return profile;
    }

    /// <inheritdoc />
    public void Export(Profile profile, string path)
    {
        if (IsBundle(path))
        {
            var clone = JsonSerializer.Deserialize(
                JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile),
                SourceGenerationContext.NewDefault.Profile) ?? throw new JsonException("Result is null");
            WavPackBundler.CreateBundle(path, WavPackBundler.ProfileEntry, clone.Stations ?? [],
                () => JsonSerializer.Serialize(clone, SourceGenerationContext.NewDefault.Profile));
            return;
        }

        var scrubbed = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile),
            SourceGenerationContext.NewDefault.Profile) ?? throw new JsonException("Result is null");
        File.WriteAllText(path, JsonSerializer.Serialize(scrubbed, SourceGenerationContext.NewDefault.Profile));
    }

    /// <summary>
    /// Keeps a user's WAV voice pack settings when a profile is replaced by its remote version, which has none.
    /// Stations are matched by id, then by identifier and ATIS type.
    /// </summary>
    private static void PreserveVoicePacks(Profile local, Profile updated)
    {
        foreach (var localStation in local.Stations?.Where(x => x.AtisVoice is { UseWavPack: true }) ?? [])
        {
            var match = updated.Stations?.FirstOrDefault(x => x.Id == localStation.Id) ??
                        updated.Stations?.FirstOrDefault(x =>
                            x.Identifier == localStation.Identifier && x.AtisType == localStation.AtisType);
            if (match == null)
            {
                continue;
            }

            match.AtisVoice.UseWavPack = true;
            match.AtisVoice.UseTextToSpeech = false;
            match.AtisVoice.WavPackId = localStation.AtisVoice.WavPackId;
            match.AtisVoice.WavPackPath = localStation.AtisVoice.WavPackPath;
        }
    }

    private static bool IsBundle(string path) => path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

    private static Profile ReadBundle(string path)
    {
        var (json, packIds) = WavPackBundler.ReadBundle(path, WavPackBundler.ProfileEntry);
        var profile = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.Profile) ??
                      throw new JsonException("Result is null");
        WavPackBundler.ResolvePacks(profile.Stations ?? [], packIds);
        return profile;
    }

    private static async Task<Profile> Load(string path)
    {
        return JsonSerializer.Deserialize(await File.ReadAllTextAsync(path),
            SourceGenerationContext.NewDefault.Profile) ?? throw new JsonException("Result is null");
    }

    private static string CreateCopyName(string name, string[] existingNames)
    {
        var newName = $"{name} - Copy";
        var copyNumber = 2;
        while (Array.Exists(existingNames, x => x == newName))
        {
            newName = $"{name} - Copy ({copyNumber})";
            copyNumber++;
        }

        return newName;
    }

    private async Task<Profile> LoadAndMigrate(string path)
    {
        var profile = await Load(path);
        EnsureLatestVersion(profile, out var wasUpdated);
        if (wasUpdated)
        {
            Save(profile);
        }

        return profile;
    }

    private async Task CheckForProfileUpdate(Profile localProfile)
    {
        string cacheBusterUpdateUrl; // The update URL with a cache buster timestamp appended to it.
        string queryChar; // The character to use to append the cache buster to the URL.

        try
        {
            if (string.IsNullOrEmpty(localProfile.UpdateUrl)) return;

            // Append a cache buster to the update URL to ensure we don't get a cached response.
            var baseUri = new Uri(localProfile.UpdateUrl);

            // If the query string is empty, we need to add a "?" to the URL. Otherwise, we need to add an "&".
            queryChar = baseUri.Query.Length == 0 ? "?" : "&";
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Unable to convert {localProfile.UpdateUrl} to an Uri in profile {localProfile.Id}.");
            return;
        }

        // Append the current Unix timestamp to the query string.
        cacheBusterUpdateUrl = $"{localProfile.UpdateUrl}{queryChar}ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        try
        {
            var response = await _downloader.GetAsync(cacheBusterUpdateUrl);
            if (response.IsSuccessStatusCode)
            {
                var remoteProfileJson = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(remoteProfileJson))
                {
                    var remoteProfile = JsonSerializer.Deserialize(remoteProfileJson,
                        SourceGenerationContext.NewDefault.Profile);
                    if (remoteProfile != null)
                    {
                        if (localProfile.UpdateSerial == null ||
                            remoteProfile.UpdateSerial > localProfile.UpdateSerial)
                        {
                            Log.Information($"Updating profile {localProfile.Name}: {localProfile.Id}");
                            var updatedProfile =
                                remoteProfile ?? throw new JsonException("Updated profile is null");
                            updatedProfile.Id = localProfile.Id;
                            if (localProfile.IsNameOverridden)
                            {
                                updatedProfile.Name = localProfile.Name;
                                updatedProfile.IsNameOverridden = true;
                            }

                            PreserveVoicePacks(localProfile, updatedProfile);
                            Delete(localProfile);
                            Save(updatedProfile);
                        }
                    }
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Warning($"Profile update URL not found for {localProfile.Id} at {cacheBusterUpdateUrl}.");
            }
            else
            {
                Log.Warning(
                    $"Profile update request failed with status code {response.StatusCode} for {localProfile.Id} at {cacheBusterUpdateUrl}.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Profile update check failed for {localProfile.Id} at {cacheBusterUpdateUrl}.");
        }
    }

    private void EnsureProfilesFolderExists()
    {
        if (!Directory.Exists(PathProvider.ProfilesFolderPath))
        {
            Log.Information($"Creating Profiles folder {PathProvider.ProfilesFolderPath}");
            Directory.CreateDirectory(PathProvider.ProfilesFolderPath);
        }
    }

    private void EnsureLatestVersion(Profile profile, out bool wasUpdated)
    {
        wasUpdated = false;
        if (profile.Version < 4)
        {
            UpdateTo4(profile);
            wasUpdated = true;
        }
    }

    private void UpdateTo4(Profile profile)
    {
        Log.Information($"Updating profile {profile.Name} to version 4");

        if (profile.Stations != null)
        {
            foreach (var station in profile.Stations)
            {
                station.AtisFormat.PresentWeather.EnsureDefaultWeatherTypes();

                foreach (var preset in station.Presets)
                {
                    preset.ExternalGenerator?.MigrateUrl();
                }
            }
        }

        profile.Version = 4;
    }
}
