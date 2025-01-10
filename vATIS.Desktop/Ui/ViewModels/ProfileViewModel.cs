// <copyright file="ProfileViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for a user profile, providing data binding and property change notification.
/// Inherits from <see cref="ReactiveViewModelBase"/>.
/// </summary>
public class ProfileViewModel : ReactiveViewModelBase
{
    private string name = string.Empty;
    private Profile? profile;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileViewModel"/> class.
    /// </summary>
    /// <param name="profile">The associated <see cref="Profile"/>.</param>
    public ProfileViewModel(Profile profile)
    {
        this.Profile = profile;
        this.Name = profile.Name;
    }

    /// <summary>
    /// Gets the profile for the user.
    /// </summary>
    public Profile? Profile
    {
        get => this.profile;
        private set => this.RaiseAndSetIfChanged(ref this.profile, value);
    }

    /// <summary>
    /// Gets or sets the name associated with the profile.
    /// </summary>
    public string Name
    {
        get => this.name;
        set => this.RaiseAndSetIfChanged(ref this.name, value);
    }
}
