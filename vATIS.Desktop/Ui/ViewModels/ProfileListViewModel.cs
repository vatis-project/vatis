// <copyright file="ProfileListViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events.WebSocket;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Ui.Services.Websocket;
using Vatsim.Vatis.Utils;
using Velopack.Locators;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for managing a list of profiles, including commands for creating, renaming,
/// importing, exporting, and deleting profiles, as well as starting client sessions.
/// </summary>
public class ProfileListViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly IWindowLocationService _windowLocationService;
    private readonly IProfileRepository _profileRepository;
    private readonly IAppConfig _appConfig;
    private readonly SourceList<ProfileViewModel> _profileList = new();
    private readonly IWebsocketService _websocketService;
    private IDialogOwner? _dialogOwner;
    private ProfileViewModel? _selectedProfile;
    private string _previousUserValue = "";
    private bool _showOverlay;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileListViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager responsible for handling client sessions.</param>
    /// <param name="windowFactory">Factory for creating application windows.</param>
    /// <param name="windowLocationService">Service for managing window locations.</param>
    /// <param name="profileRepository">Repository for managing user profiles.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="websocketService">The websocket service.</param>
    public ProfileListViewModel(ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IWindowLocationService windowLocationService,
        IProfileRepository profileRepository,
        IAppConfig appConfig,
        IWebsocketService websocketService)
    {
        _sessionManager = sessionManager;
        _windowFactory = windowFactory;
        _windowLocationService = windowLocationService;
        _profileRepository = profileRepository;
        _appConfig = appConfig;
        _websocketService = websocketService;

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
        OpenReleaseNotesCommand = ReactiveCommand.CreateFromTask(HandleOpenReleaseNotes);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += OnWindowCollectionChanged;
        }

        _profileList.Connect()
            .AutoRefresh(p => p.Name)
            .Sort(SortExpressionComparer<ProfileViewModel>.Ascending(i => i.Name))
            .Bind(out var sortedProfiles)
            .Subscribe(_ => Profiles = sortedProfiles);
        Profiles = sortedProfiles;

        _websocketService.ChangeProfileReceived += OnChangeProfileReceived;
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
    /// Gets the command that opens the release notes for the installed version.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenReleaseNotesCommand { get; }

    /// <summary>
    /// Gets or sets the collection of profiles.
    /// </summary>
    public ReadOnlyObservableCollection<ProfileViewModel> Profiles { get; set; }

    /// <summary>
    /// Gets or sets the currently selected profile.
    /// </summary>
    public ProfileViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the overlay is displayed.
    /// </summary>
    public bool ShowOverlay
    {
        get => _showOverlay;
        set => this.RaiseAndSetIfChanged(ref _showOverlay, value);
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
            return;

        _windowLocationService.Update(window);
    }

    /// <summary>
    /// Restores the position of the specified window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position needs to be restored.</param>
    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Restore(window);
    }

    /// <summary>
    /// Sets the dialog owner that provides contextual ownership for dialogs.
    /// </summary>
    /// <param name="owner">The dialog owner implementing the <see cref="IDialogOwner"/> interface.</param>
    public void SetDialogOwner(IDialogOwner owner)
    {
        _dialogOwner = owner;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        InitializeCommand.Dispose();
        ShowNewProfileDialogCommand.Dispose();
        RenameProfileCommand.Dispose();
        ImportProfileCommand.Dispose();
        DeleteProfileCommand.Dispose();
        ExportProfileCommand.Dispose();
        StartClientSessionCommand.Dispose();
        ExitCommand.Dispose();
        OpenReleaseNotesCommand.Dispose();

        _websocketService.ChangeProfileReceived -= OnChangeProfileReceived;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged -= OnWindowCollectionChanged;
        }

        _profileList.Dispose();

        GC.SuppressFinalize(this);
    }

    private async Task Initialize()
    {
        foreach (var profile in await _profileRepository.LoadAll())
        {
            _profileList.Add(new ProfileViewModel(profile));
        }
    }

    private void HandleStartSession(ProfileViewModel model)
    {
        if (model.Profile != null)
        {
            _sessionManager.StartSession(model.Profile.Id);
        }
    }

    private async Task HandleRenameProfile(ProfileViewModel profile)
    {
        if (_dialogOwner == null)
            return;

        var dialog = _windowFactory.CreateUserInputDialog();
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
                        await _profileRepository.Rename(profile.Profile.Id, profile.Name);
                    }
                }
            };
            await dialog.ShowDialog((Window)_dialogOwner);
        }
    }

    private async Task HandleDeleteProfile(ProfileViewModel profile)
    {
        if (_dialogOwner == null)
            return;

        if (await MessageBox.ShowDialog((Window)_dialogOwner,
                $"Are you sure you want to delete profile \"{profile.Name}\"?", "Delete Profile",
                MessageBoxButton.YesNo, MessageBoxIcon.Question) == MessageBoxResult.Yes)
        {
            if (profile == SelectedProfile)
            {
                SelectedProfile = null;
            }

            if (profile.Profile != null)
            {
                _profileRepository.Delete(profile.Profile);
            }

            _profileList.Remove(profile);
        }
    }

    private async Task HandleNewProfile()
    {
        _previousUserValue = "";

        if (_dialogOwner == null)
            return;

        var dialog = _windowFactory.CreateUserInputDialog();
        if (dialog.DataContext is UserInputDialogViewModel context)
        {
            context.Title = "New Profile";
            context.Prompt = "Profile Name:";
            context.UserValue = _previousUserValue;
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
                    _profileList.Add(new ProfileViewModel(profile));
                    _profileRepository.Save(profile);
                }
            };
            await dialog.ShowDialog((Window)_dialogOwner);
        }
    }

    private async Task HandleImportProfile()
    {
        if (_dialogOwner == null)
            return;

        try
        {
            var filters = new List<FilePickerFileType> { new("vATIS Profile (*.json)") { Patterns = ["*.json"] } };
            var files = await FilePickerExtensions.OpenFilePickerAsync(filters, "Import vATIS Profile");

            if (files == null)
                return;

            foreach (var file in files)
            {
                var newProfile = await _profileRepository.Import(file);
                _profileList.Add(new ProfileViewModel(newProfile));
            }

            try
            {
                Log.Information("Checking for profile updates...");
                await _profileRepository.CheckForProfileUpdates();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for profile updates");
                await MessageBox.ShowDialog(
                    (Window)_dialogOwner,
                    ex.Message,
                    "Import Error: Checking for profile updates",
                    MessageBoxButton.Ok,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to import profile");
            await MessageBox.ShowDialog((Window)_dialogOwner, ex.Message, "Import Error", MessageBoxButton.Ok,
                MessageBoxIcon.Error);
        }
    }

    private async Task HandleExportProfile()
    {
        if (SelectedProfile?.Profile == null)
            return;

        if (_dialogOwner == null)
            return;

        var filters = new List<FilePickerFileType> { new("vATIS Profile (*.json)") { Patterns = ["*.json"] } };
        var file = await FilePickerExtensions.SaveFileAsync("Export Profile", filters,
            $"vATIS Profile - {SelectedProfile.Name}.json");

        if (file == null)
            return;

        _profileRepository.Export(SelectedProfile.Profile, file.Path.LocalPath);
        await MessageBox.ShowDialog((Window)_dialogOwner, "Profile successfully exported.", "Success",
            MessageBoxButton.Ok, MessageBoxIcon.Information);
    }

    private void HandleExit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.Invoke(() => desktop.Shutdown());
        }
    }

    private async Task HandleOpenReleaseNotes()
    {
        if (_dialogOwner == null)
            return;

        try
        {
            var locator = VelopackLocator.GetDefault(NullLogger.Instance);
            var currentRelease = locator.GetLocalPackages()
                .FirstOrDefault(x => x.Version == locator.CurrentlyInstalledVersion);
            if (currentRelease?.NotesMarkdown != null)
            {
                var releaseNotes = _windowFactory.CreateReleaseNotesDialog();
                if (releaseNotes.ViewModel != null)
                {
                    releaseNotes.ViewModel.SuppressReleaseNotes = _appConfig.SuppressReleaseNotes;
                    releaseNotes.ViewModel.ReleaseNotes = currentRelease.NotesMarkdown;
                    if (await releaseNotes.ShowDialog<DialogResult>((Window)_dialogOwner) == DialogResult.Ok)
                    {
                        _appConfig.SuppressReleaseNotes = releaseNotes.ViewModel.SuppressReleaseNotes;
                        _appConfig.SaveConfig();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing release notes.");
        }
    }

    private void OnChangeProfileReceived(object? sender, GetChangeProfileReceived e)
    {
        if (e.ProfileId != null)
        {
            Dispatcher.UIThread.Invoke(() => _sessionManager.StartSession(e.ProfileId));
        }
    }

    private void OnWindowCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ShowOverlay = lifetime.Windows.Count > 1;
        }
    }
}
