// <copyright file="ProfileListViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <summary>
/// Represents the view model for managing a list of profiles, including commands for creating, renaming,
/// importing, exporting, and deleting profiles, as well as starting client sessions.
/// </summary>
public class ProfileListViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IProfileRepository profileRepository;
    private readonly ISessionManager sessionManager;
    private readonly IWindowFactory windowFactory;
    private readonly IWindowLocationService windowLocationService;
    private readonly SourceList<ProfileViewModel> profileList = new();
    private IDialogOwner? dialogOwner;
    private string previousUserValue = string.Empty;
    private ProfileViewModel? selectedProfile;
    private bool showOverlay;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileListViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager responsible for handling client sessions.</param>
    /// <param name="windowFactory">Factory for creating application windows.</param>
    /// <param name="windowLocationService">Service for managing window locations.</param>
    /// <param name="profileRepository">Repository for managing user profiles.</param>
    public ProfileListViewModel(
        ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IWindowLocationService windowLocationService,
        IProfileRepository profileRepository)
    {
        this.sessionManager = sessionManager;
        this.windowFactory = windowFactory;
        this.windowLocationService = windowLocationService;
        this.profileRepository = profileRepository;

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

        this.profileList.Connect()
            .Sort(SortExpressionComparer<ProfileViewModel>.Ascending(i => i.Name))
            .Bind(out var sortedProfiles)
            .Subscribe(_ => this.Profiles = sortedProfiles);
        this.Profiles = sortedProfiles;
    }

    /// <summary>
    /// Gets the command that initializes the view model.
    /// </summary>
    public ReactiveCommand<Unit, Unit> InitializeCommand { get; }

    /// <summary>
    /// Gets the command that displays the dialog for creating a new profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowNewProfileDialogCommand { get; }

    /// <summary>
    /// Gets the command that handles renaming a profile in the profile list.
    /// </summary>
    public ReactiveCommand<ProfileViewModel, Unit> RenameProfileCommand { get; }

    /// <summary>
    /// Gets the command that imports a profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportProfileCommand { get; }

    /// <summary>
    /// Gets the command that deletes a profile.
    /// </summary>
    public ReactiveCommand<ProfileViewModel, Unit> DeleteProfileCommand { get; }

    /// <summary>
    /// Gets the command that exports the selected profile.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportProfileCommand { get; }

    /// <summary>
    /// Gets the command that initiates a client session using the selected profile.
    /// </summary>
    public ReactiveCommand<ProfileViewModel, Unit> StartClientSessionCommand { get; }

    /// <summary>
    /// Gets the command that handles application exit functionality.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    /// <summary>
    /// Gets or sets the collection of profiles.
    /// </summary>
    public ReadOnlyObservableCollection<ProfileViewModel> Profiles { get; set; }

    /// <summary>
    /// Gets or sets the currently selected profile.
    /// </summary>
    public ProfileViewModel? SelectedProfile
    {
        get => this.selectedProfile;
        set => this.RaiseAndSetIfChanged(ref this.selectedProfile, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the overlay is displayed.
    /// </summary>
    public bool ShowOverlay
    {
        get => this.showOverlay;
        set => this.RaiseAndSetIfChanged(ref this.showOverlay, value);
    }

    /// <summary>
    /// Gets the client's version information.
    /// </summary>
    public string ClientVersion { get; }

    /// <summary>
    /// Updates the position of the specified window in the window location service.
    /// </summary>
    /// <param name="window">The window whose position is being updated.</param>
    public void UpdatePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this.windowLocationService.Update(window);
    }

    /// <summary>
    /// Restores the position of the specified window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position needs to be restored.</param>
    public void RestorePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this.windowLocationService.Restore(window);
    }

    /// <summary>
    /// Sets the dialog owner that provides contextual ownership for dialogs.
    /// </summary>
    /// <param name="owner">The dialog owner implementing the <see cref="IDialogOwner"/> interface.</param>
    public void SetDialogOwner(IDialogOwner owner)
    {
        this.dialogOwner = owner;
    }

    /// <inheritdoc/>
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
        foreach (var profile in await this.profileRepository.LoadAll())
        {
            this.profileList.Add(new ProfileViewModel(profile));
        }
    }

    private void HandleStartSession(ProfileViewModel model)
    {
        if (model.Profile != null)
        {
            this.sessionManager.StartSession(model.Profile.Id);
        }
    }

    private async Task HandleRenameProfile(ProfileViewModel profile)
    {
        if (this.dialogOwner == null)
        {
            return;
        }

        var dialog = this.windowFactory.CreateUserInputDialog();
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
                    if (profile.Profile != null)
                    {
                        await this.profileRepository.Rename(profile.Profile.Id, profile.Name);
                    }
                }
            };
            await dialog.ShowDialog((Window)this.dialogOwner);
        }
    }

    private async Task HandleDeleteProfile(ProfileViewModel profile)
    {
        if (this.dialogOwner == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)this.dialogOwner,
                $"Are you sure you want to delete profile \"{profile.Name}\"?",
                "Delete Profile",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Question) == MessageBoxResult.Yes)
        {
            if (profile == this.SelectedProfile)
            {
                this.SelectedProfile = null;
            }

            if (profile.Profile != null)
            {
                this.profileRepository.Delete(profile.Profile);
            }

            this.profileList.Remove(profile);
        }
    }

    private async Task HandleNewProfile()
    {
        this.previousUserValue = string.Empty;

        if (this.dialogOwner == null)
        {
            return;
        }

        var dialog = this.windowFactory.CreateUserInputDialog();
        if (dialog.DataContext is UserInputDialogViewModel context)
        {
            context.Title = "New Profile";
            context.Prompt = "Profile Name:";
            context.UserValue = this.previousUserValue;
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
                    this.profileList.Add(new ProfileViewModel(profile));
                    this.profileRepository.Save(profile);
                }
            };
            await dialog.ShowDialog((Window)this.dialogOwner);
        }
    }

    private async Task HandleImportProfile()
    {
        if (this.dialogOwner == null)
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
                var newProfile = await this.profileRepository.Import(file);
                this.profileList.Add(new ProfileViewModel(newProfile));
            }

            try
            {
                Log.Information("Checking for profile updates...");
                await this.profileRepository.CheckForProfileUpdates();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for profile updates");
                await MessageBox.ShowDialog(
                    (Window)this.dialogOwner,
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
                (Window)this.dialogOwner,
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

        if (this.dialogOwner == null)
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

        this.profileRepository.Export(this.SelectedProfile.Profile, file.Path.LocalPath);
        await MessageBox.ShowDialog(
            (Window)this.dialogOwner,
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
}
