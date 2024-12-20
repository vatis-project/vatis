using ReactiveUI;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.ViewModels;

public class ProfileViewModel : ReactiveViewModelBase
{
    private Profile mProfile = null!;
    public Profile Profile
    {
        get => mProfile;
        private set => this.RaiseAndSetIfChanged(ref mProfile, value);
    }

    private string mName = "";
    public string Name
    {
        get => mName;
        set => this.RaiseAndSetIfChanged(ref mName, value);
    }

    public ProfileViewModel(Profile profile)
    {
        Profile = profile;
        Name = profile.Name;
    }
}