// <copyright file="ProfileViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using ReactiveUI;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.ViewModels;

public class ProfileViewModel : ReactiveViewModelBase
{
    private Profile _profile = null!;
    public Profile Profile
    {
        get => _profile;
        private set => this.RaiseAndSetIfChanged(ref _profile, value);
    }

    private string _name = "";
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public ProfileViewModel(Profile profile)
    {
        Profile = profile;
        Name = profile.Name;
    }
}
