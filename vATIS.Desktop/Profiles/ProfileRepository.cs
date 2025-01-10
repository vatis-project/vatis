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

public class ProfileRepository : IProfileRepository
{
    private readonly IDownloader _downloader;

    public ProfileRepository(IDownloader downloader)
    {
        this._downloader = downloader;
        this.EnsureProfilesFolderExists();
    }

    public async Task CheckForProfileUpdates()
    {
        var profiles = await this.LoadAll();
        foreach (var localProfile in profiles)
        {
            try
            {
                if (string.IsNullOrEmpty(localProfile.UpdateUrl))
                {
                    continue;
                }

                var response = await this._downloader.GetAsync(localProfile.UpdateUrl);
                if (response.IsSuccessStatusCode)
                {
                    var remoteProfileJson = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(remoteProfileJson))
                    {
                        var remoteProfile = JsonSerializer.Deserialize(
                            remoteProfileJson,
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
                                this.Delete(localProfile);
                                this.Save(updatedProfile);
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

    public void Save(Profile profile)
    {
        var path = PathProvider.GetProfilePath(profile.Id);
        Log.Information($"Saving Profile {profile.Name} to {path}");
        File.WriteAllText(path, JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile));
    }

    public async Task Rename(string profileId, string newName)
    {
        var profile = await Load(PathProvider.GetProfilePath(profileId));
        profile.Name = newName;
        this.Save(profile);
    }

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

    public async Task<Profile> Copy(Profile profile)
    {
        var profiles = await this.LoadAll();
        var newName = CreateCopyName(profile.Name, profiles.Select(p => p.Name).ToArray());
        var newProfile = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile),
            SourceGenerationContext.NewDefault.Profile) ?? throw new JsonException("Result is null");
        newProfile.Id = Guid.NewGuid().ToString();
        newProfile.Name = newName;
        Log.Information($"Copying profile {profile.Name} to {newProfile.Name}");
        this.Save(newProfile);
        return newProfile;
    }

    public void Delete(Profile profile)
    {
        var path = PathProvider.GetProfilePath(profile.Id);
        Log.Information($"Deleting profile {profile.Name} from {path}");
        File.Delete(path);
    }

    public async Task<Profile> Import(string path)
    {
        var profile = await Load(path);
        Log.Information($"Importing profile {profile.Name}");
        profile.Id = Guid.NewGuid().ToString();
        this.Save(profile);
        return profile;
    }

    public void Export(Profile profile, string path)
    {
        var scrubbed = JsonSerializer.Deserialize(
            JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile),
            SourceGenerationContext.NewDefault.Profile) ?? throw new JsonException("Result is null");
        File.WriteAllText(path, JsonSerializer.Serialize(scrubbed, SourceGenerationContext.NewDefault.Profile));
    }

    private void EnsureProfilesFolderExists()
    {
        if (!Directory.Exists(PathProvider.ProfilesFolderPath))
        {
            Log.Information($"Creating Profiles folder {PathProvider.ProfilesFolderPath}");
            Directory.CreateDirectory(PathProvider.ProfilesFolderPath);
        }
    }

    private static async Task<Profile> Load(string path)
    {
        return JsonSerializer.Deserialize(
            await File.ReadAllTextAsync(path),
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
}