// <copyright file="IProfileRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Profiles;

/// <summary>
/// Provides functionality to manage and interact with ATIS profiles.
/// </summary>
public interface IProfileRepository
{
    /// <summary>
    /// Checks for any updates to the profiles.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CheckForProfileUpdates();

    /// <summary>
    /// Saves the specified profile to persistent storage.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    void Save(Profile profile);

    /// <summary>
    /// Renames a profile by updating its name and saving the changes.
    /// </summary>
    /// <param name="profileId">The identifier of the profile to rename.</param>
    /// <param name="newName">The new name for the profile.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Rename(string profileId, string newName);

    /// <summary>
    /// Loads all profiles available in the configured profile directory.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of profiles.</returns>
    Task<List<Profile>> LoadAll();

    /// <summary>
    /// Provides functionality to copy an existing profile.
    /// </summary>
    /// <param name="profile">The profile to copy.</param>
    /// <returns>A <see cref="Task{Profile}"/> representing the result of the asynchronous operation.</returns>
    Task<Profile> Copy(Profile profile);

    /// <summary>
    /// Deletes the specified profile from persistent storage.
    /// </summary>
    /// <param name="profile">The profile to delete.</param>
    void Delete(Profile profile);

    /// <summary>
    /// Imports a profile from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the profile to be imported.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the imported <see cref="Profile"/>.</returns>
    Task<Profile> Import(string path);

    /// <summary>
    /// Exports the specified profile to a file at the given path.
    /// </summary>
    /// <param name="profile">The profile to export.</param>
    /// <param name="path">The file path where the profile will be exported.</param>
    void Export(Profile profile, string path);
}
