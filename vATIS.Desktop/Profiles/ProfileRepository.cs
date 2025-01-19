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
        foreach (var localProfile in profiles)
        {
            try
            {
                if (string.IsNullOrEmpty(localProfile.UpdateUrl)) continue;

                var cacheBusterUpdateUrl = localProfile.UpdateUrl + $"?ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
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
                                Delete(localProfile);
                                Save(updatedProfile);
                            }
                        }
                    }
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warning($"Profile update URL not found for {localProfile.Id} at {localProfile.UpdateUrl}.");
                }
                else
                {
                    Log.Warning(
                        $"Profile update request failed with status code {response.StatusCode} for {localProfile.Id} at {localProfile.UpdateUrl}.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Profile update check failed for {localProfile.Id} at {localProfile.UpdateUrl}.");
            }
        }
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
        Save(profile);
    }

    /// <inheritdoc />
    public async Task<List<Profile>> LoadAll()
    {
        var paths = Directory.GetFiles(PathProvider.ProfilesFolderPath, "*.json");
        var profiles = new List<Profile>(paths.Length);
        foreach (var path in paths)
        {
            try
            {
                var profile = await Load(path);
                profiles.Add(profile);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to deserialize profile from path: " + path);
            }
        }

        return profiles;
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
        var profile = await Load(path);
        Log.Information($"Importing profile {profile.Name}");
        profile.Id = Guid.NewGuid().ToString();
        Save(profile);
        return profile;
    }

    /// <inheritdoc />
    public void Export(Profile profile, string path)
    {
        var scrubbed = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile),
            SourceGenerationContext.NewDefault.Profile) ?? throw new JsonException("Result is null");
        File.WriteAllText(path, JsonSerializer.Serialize(scrubbed, SourceGenerationContext.NewDefault.Profile));
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

    private void EnsureProfilesFolderExists()
    {
        if (!Directory.Exists(PathProvider.ProfilesFolderPath))
        {
            Log.Information($"Creating Profiles folder {PathProvider.ProfilesFolderPath}");
            Directory.CreateDirectory(PathProvider.ProfilesFolderPath);
        }
    }
}
