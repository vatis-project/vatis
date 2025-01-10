using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Utils;

namespace Vatsim.Vatis.Ui.ViewModels;

public class ProfileListViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly IWindowLocationService _windowLocationService;
    private IDialogOwner? _dialogOwner;
    private string _previousUserValue = "";

    public ProfileListViewModel(
        ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IWindowLocationService windowLocationService,
        IProfileRepository profileRepository)
    {
        this._sessionManager = sessionManager;
        this._windowFactory = windowFactory;
        this._windowLocationService = windowLocationService;
        this._profileRepository = profileRepository;

        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        this.ClientVersion = $"Version {version}";

        var canExecute = this.WhenAnyValue(x => x.SelectedProfile).Select(x => x != null);

        this.InitializeCommand = ReactiveCommand.CreateFromTask(this.Initialize);
        this.ShowNewProfileDialogCommand = ReactiveCommand.CreateFromTask(this.HandleNewProfile);
        this.RenameProfileCommand =
            ReactiveCommand.CreateFromTask<ProfileViewModel>(this.HandleRenameProfile, canExecute);
        this.ImportProfileCommand = ReactiveCommand.CreateFromTask(this.HandleImportProfile);
        this.ExportProfileCommand = ReactiveCommand.CreateFromTask(this.HandleExportProfile, canExecute);
        this.DeleteProfileCommand =
            ReactiveCommand.CreateFromTask<ProfileViewModel>(this.HandleDeleteProfile, canExecute);
        this.StartClientSessionCommand = ReactiveCommand.Create<ProfileViewModel>(this.HandleStartSession, canExecute);
        this.ExitCommand = ReactiveCommand.Create(this.HandleExit);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                this.ShowOverlay = lifetime.Windows.Count > 1;
            };
        }

        this._profileList.Connect()
            .Sort(SortExpressionComparer<ProfileViewModel>.Ascending(i => i.Name))
            .Bind(out var sortedProfiles)
            .Subscribe(_ => this.Profiles = sortedProfiles);
        this.Profiles = sortedProfiles;
    }

    public ReactiveCommand<Unit, Unit> InitializeCommand { get; }

    public ReactiveCommand<Unit, Unit> ShowNewProfileDialogCommand { get; }

    public ReactiveCommand<ProfileViewModel, Unit> RenameProfileCommand { get; }

    public ReactiveCommand<Unit, Unit> ImportProfileCommand { get; }

    public ReactiveCommand<ProfileViewModel, Unit> DeleteProfileCommand { get; }

    public ReactiveCommand<Unit, Unit> ExportProfileCommand { get; }

    public ReactiveCommand<ProfileViewModel, Unit> StartClientSessionCommand { get; }

    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.InitializeCommand.Dispose();
        this.ShowNewProfileDialogCommand.Dispose();
        this.RenameProfileCommand.Dispose();
        this.ImportProfileCommand.Dispose();
        this.DeleteProfileCommand.Dispose();
        this.ExportProfileCommand.Dispose();
        this.StartClientSessionCommand.Dispose();
        this.ExitCommand.Dispose();
    }

    private async Task Initialize()
    {
        foreach (var profile in await this._profileRepository.LoadAll())
        {
            this._profileList.Add(new ProfileViewModel(profile));
        }
    }

    private void HandleStartSession(ProfileViewModel model)
    {
        this._sessionManager.StartSession(model.Profile.Id);
    }

    private async Task HandleRenameProfile(ProfileViewModel profile)
    {
        if (this._dialogOwner == null)
        {
            return;
        }

        var dialog = this._windowFactory.CreateUserInputDialog();
        if (dialog.DataContext is UserInputDialogViewModel context)
        {
            context.Title = "Rename Profile";
            context.Prompt = "Profile Name:";
            context.UserValue = profile.Name;
            context.DialogResultChanged += async (_, dialogResult) =>
            {
                if (dialogResult == DialogResult.Ok)
                {
                    context.ClearError();

                    if (string.IsNullOrWhiteSpace(context.UserValue))
                    {
                        context.SetError("Profile name is required.");
                        return;
                    }

                    profile.Name = context.UserValue;
                    await this._profileRepository.Rename(profile.Profile.Id, profile.Name);
                }
            };
            await dialog.ShowDialog((Window)this._dialogOwner);
        }
    }

    private async Task HandleDeleteProfile(ProfileViewModel profile)
    {
        if (this._dialogOwner == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)this._dialogOwner,
                $"Are you sure you want to delete profile \"{profile.Name}\"?",
                "Delete Profile",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Question) == MessageBoxResult.Yes)
        {
            if (profile == this.SelectedProfile)
            {
                this.SelectedProfile = null;
            }

            this._profileRepository.Delete(profile.Profile);
            this._profileList.Remove(profile);
        }
    }

    private async Task HandleNewProfile()
    {
        this._previousUserValue = "";

        if (this._dialogOwner == null)
        {
            return;
        }

        var dialog = this._windowFactory.CreateUserInputDialog();
        if (dialog.DataContext is UserInputDialogViewModel context)
        {
            context.Title = "New Profile";
            context.Prompt = "Profile Name:";
            context.UserValue = this._previousUserValue;
            context.DialogResultChanged += (_, dialogResult) =>
            {
                if (dialogResult == DialogResult.Ok)
                {
                    context.ClearError();

                    if (string.IsNullOrWhiteSpace(context.UserValue))
                    {
                        context.SetError("Profile name is required.");
                        return;
                    }

                    var profile = new Profile { Name = context.UserValue.Trim() };
                    this._profileList.Add(new ProfileViewModel(profile));
                    this._profileRepository.Save(profile);
                }
            };
            await dialog.ShowDialog((Window)this._dialogOwner);
        }
    }

    private async Task HandleImportProfile()
    {
        if (this._dialogOwner == null)
        {
            return;
        }

        try
        {
            var filters = new List<FilePickerFileType> { new("vATIS Profile (*.json)") { Patterns = ["*.json"] } };
            var files = await FilePickerExtensions.OpenFilePickerAsync(filters, "Import vATIS Profile");

            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                var newProfile = await this._profileRepository.Import(file);
                this._profileList.Add(new ProfileViewModel(newProfile));
            }

            try
            {
                Log.Information("Checking for profile updates...");
                await this._profileRepository.CheckForProfileUpdates();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for profile updates");
                await MessageBox.ShowDialog(
                    (Window)this._dialogOwner,
                    ex.Message,
                    "Import Error: Checking for profile updates",
                    MessageBoxButton.Ok,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to import profile");
            await MessageBox.ShowDialog(
                (Window)this._dialogOwner,
                ex.Message,
                "Import Error",
                MessageBoxButton.Ok,
                MessageBoxIcon.Error);
        }
    }

    private async Task HandleExportProfile()
    {
        if (this.SelectedProfile?.Profile == null)
        {
            return;
        }

        if (this._dialogOwner == null)
        {
            return;
        }

        var filters = new List<FilePickerFileType> { new("vATIS Profile (*.json)") { Patterns = ["*.json"] } };
        var file = await FilePickerExtensions.SaveFileAsync(
            "Export Profile",
            filters,
            $"vATIS Profile - {this.SelectedProfile.Name}.json");

        if (file == null)
        {
            return;
        }

        this._profileRepository.Export(this.SelectedProfile.Profile, file.Path.LocalPath);
        await MessageBox.ShowDialog(
            (Window)this._dialogOwner,
            "Profile successfully exported.",
            "Success",
            MessageBoxButton.Ok,
            MessageBoxIcon.Information);
    }

    private void HandleExit()
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.Invoke(() => desktop.Shutdown());
        }
    }

    public void UpdatePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this._windowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this._windowLocationService.Restore(window);
    }

    public void SetDialogOwner(IDialogOwner owner)
    {
        this._dialogOwner = owner;
    }

    #region Reactive Properties

    private readonly SourceList<ProfileViewModel> _profileList = new();

    public ReadOnlyObservableCollection<ProfileViewModel> Profiles { get; set; }

    private ProfileViewModel? _selectedProfile;

    public ProfileViewModel? SelectedProfile
    {
        get => this._selectedProfile;
        set => this.RaiseAndSetIfChanged(ref this._selectedProfile, value);
    }

    private bool _showOverlay;

    public bool ShowOverlay
    {
        get => this._showOverlay;
        set => this.RaiseAndSetIfChanged(ref this._showOverlay, value);
    }

    public string ClientVersion { get; }

    #endregion
}