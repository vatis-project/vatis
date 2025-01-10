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
