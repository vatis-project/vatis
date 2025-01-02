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
    #region Reactive Properties
    private readonly SourceList<ProfileViewModel> mProfileList = new();
    public ReadOnlyObservableCollection<ProfileViewModel> Profiles { get; set; }

    private ProfileViewModel? mSelectedProfile;
    public ProfileViewModel? SelectedProfile
    {
        get => mSelectedProfile;
        set => this.RaiseAndSetIfChanged(ref mSelectedProfile, value);
    }

    private bool mShowOverlay;
    public bool ShowOverlay
    {
        get => mShowOverlay;
        set => this.RaiseAndSetIfChanged(ref mShowOverlay, value);
    }

    public string ClientVersion { get; }
    #endregion

    public ReactiveCommand<Unit, Unit> InitializeCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowNewProfileDialogCommand { get; }
    public ReactiveCommand<ProfileViewModel, Unit> RenameProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportProfileCommand { get; }
    public ReactiveCommand<ProfileViewModel, Unit> DeleteProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportProfileCommand { get; }
    public ReactiveCommand<ProfileViewModel, Unit> StartClientSessionCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    private readonly ISessionManager mSessionManager;
    private readonly IWindowFactory mWindowFactory;
    private readonly IWindowLocationService mWindowLocationService;
    private readonly IProfileRepository mProfileRepository;
    private IDialogOwner? mDialogOwner;
    private string mPreviousUserValue = "";

    public ProfileListViewModel(ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IWindowLocationService windowLocationService,
        IProfileRepository profileRepository)
    {
        mSessionManager = sessionManager;
        mWindowFactory = windowFactory;
        mWindowLocationService = windowLocationService;
        mProfileRepository = profileRepository;

        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        ClientVersion = $"Version {version}";

        var canExecute = this.WhenAnyValue(x => x.SelectedProfile).Select(x => x != null);

        InitializeCommand = ReactiveCommand.CreateFromTask(Initialize);
        ShowNewProfileDialogCommand = ReactiveCommand.CreateFromTask(HandleNewProfile);
        RenameProfileCommand = ReactiveCommand.CreateFromTask<ProfileViewModel>(HandleRenameProfile, canExecute);
        ImportProfileCommand = ReactiveCommand.CreateFromTask(HandleImportProfile);
        ExportProfileCommand = ReactiveCommand.CreateFromTask(HandleExportProfile, canExecute);
        DeleteProfileCommand = ReactiveCommand.CreateFromTask<ProfileViewModel>(HandleDeleteProfile, canExecute);
        StartClientSessionCommand = ReactiveCommand.Create<ProfileViewModel>(HandleStartSession, canExecute);
        ExitCommand = ReactiveCommand.Create(HandleExit);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                ShowOverlay = lifetime.Windows.Count > 1;
            };
        }

        mProfileList.Connect()
            .Sort(SortExpressionComparer<ProfileViewModel>.Ascending(i => i.Name))
            .Bind(out var sortedProfiles)
            .Subscribe(_ => Profiles = sortedProfiles);
        Profiles = sortedProfiles;
    }

    private async Task Initialize()
    {
        foreach (var profile in await mProfileRepository.LoadAll())
        {
            mProfileList.Add(new ProfileViewModel(profile));
        }
    }

    private void HandleStartSession(ProfileViewModel model)
    {
        mSessionManager.StartSession(model.Profile.Id);
    }

    private async Task HandleRenameProfile(ProfileViewModel profile)
    {
        if (mDialogOwner == null)
            return;

        var dialog = mWindowFactory.CreateUserInputDialog();
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
                    await mProfileRepository.Rename(profile.Profile.Id, profile.Name);
                }
            };
            await dialog.ShowDialog((Window)mDialogOwner);
        }
    }

    private async Task HandleDeleteProfile(ProfileViewModel profile)
    {
        if (mDialogOwner == null)
            return;

        if (await MessageBox.ShowDialog((Window)mDialogOwner,
                $"Are you sure you want to delete profile \"{profile.Name}\"?", "Delete Profile",
                MessageBoxButton.YesNo, MessageBoxIcon.Question) == MessageBoxResult.Yes)
        {
            if (profile == SelectedProfile)
            {
                SelectedProfile = null;
            }

            mProfileRepository.Delete(profile.Profile);
            mProfileList.Remove(profile);
        }
    }

    private async Task HandleNewProfile()
    {
        mPreviousUserValue = "";

        if (mDialogOwner == null)
            return;

        var dialog = mWindowFactory.CreateUserInputDialog();
        if (dialog.DataContext is UserInputDialogViewModel context)
        {
            context.Title = "New Profile";
            context.Prompt = "Profile Name:";
            context.UserValue = mPreviousUserValue;
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
                    mProfileList.Add(new ProfileViewModel(profile));
                    mProfileRepository.Save(profile);
                }
            };
            await dialog.ShowDialog((Window)mDialogOwner);
        }
    }

    private async Task HandleImportProfile()
    {
        if (mDialogOwner == null)
            return;

        try
        {
            var filters = new List<FilePickerFileType> { new("vATIS Profile (*.json)") { Patterns = ["*.json"] } };
            var files = await FilePickerExtensions.OpenFilePickerAsync(filters, "Import vATIS Profile");

            if (files == null)
                return;

            foreach (var file in files)
            {
                var newProfile = await mProfileRepository.Import(file);
                mProfileList.Add(new ProfileViewModel(newProfile));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to import profile");
            await MessageBox.ShowDialog((Window)mDialogOwner, ex.Message, "Import Error", MessageBoxButton.Ok,
                MessageBoxIcon.Error);
        }
    }

    private async Task HandleExportProfile()
    {
        if (SelectedProfile?.Profile == null)
            return;

        if (mDialogOwner == null)
            return;

        var filters = new List<FilePickerFileType> { new("vATIS Profile (*.json)") { Patterns = ["*.json"] } };
        var file = await FilePickerExtensions.SaveFileAsync("Export Profile", filters,
            $"vATIS Profile - {SelectedProfile.Name}.json");

        if (file == null)
            return;

        mProfileRepository.Export(SelectedProfile.Profile, file.Path.LocalPath);
        await MessageBox.ShowDialog((Window)mDialogOwner, "Profile successfully exported.", "Success",
            MessageBoxButton.Ok, MessageBoxIcon.Information);
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
            return;

        mWindowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        mWindowLocationService.Restore(window);
    }

    public void SetDialogOwner(IDialogOwner owner)
    {
        mDialogOwner = owner;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InitializeCommand.Dispose();
        ShowNewProfileDialogCommand.Dispose();
        RenameProfileCommand.Dispose();
        ImportProfileCommand.Dispose();
        DeleteProfileCommand.Dispose();
        ExportProfileCommand.Dispose();
        StartClientSessionCommand.Dispose();
        ExitCommand.Dispose();
    }
}