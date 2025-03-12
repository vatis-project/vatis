// <copyright file="ISessionManager.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Sessions;

/// <summary>
/// Provides methods and properties to manage a session, including starting, running,
/// and ending sessions, as well as tracking session-related data.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Gets the current profile associated with the session.
    /// </summary>
    Profile? CurrentProfile { get; }

    /// <summary>
    /// Gets the maximum number of connections allowed for the session.
    /// </summary>
    int MaxConnectionCount { get; }

    /// <summary>
    /// Gets or sets the current count of active connections associated with the session.
    /// </summary>
    int CurrentConnectionCount { get; set; }

    /// <summary>
    /// Starts the session manager.
    /// </summary>
    void Run();

    /// <summary>
    /// Starts a new session using the specified profile identifier.
    /// </summary>
    /// <param name="profileId">The identifier of the profile to use for the session.</param>
    /// <returns>A task representing the asynchronous operation of starting the session.</returns>
    Task StartSession(string profileId);

    /// <summary>
    /// Ends the current active session, resets session data,
    /// and closes the main application window if applicable.
    /// </summary>
    void EndSession();

    /// <summary>
    /// Used by websocket to end and start a new session with the specified profile ID.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>A task.</returns>
    Task ChangeProfile(string profileId);
}
