using ReactiveUI;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.ViewModels;

public class ProfileViewModel : ReactiveViewModelBase
{
    private string _name = "";
    private Profile _profile = null!;

    public ProfileViewModel(Profile profile)
    {
        this.Profile = profile;
        this.Name = profile.Name;
    }

    public Profile Profile
    {
        get => this._profile;
        private set => this.RaiseAndSetIfChanged(ref this._profile, value);
    }

    public string Name
    {
        get => this._name;
        set => this.RaiseAndSetIfChanged(ref this._name, value);
    }
}