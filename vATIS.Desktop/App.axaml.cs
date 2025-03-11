// <copyright file="App.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;
using Sentry;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services.Websocket;
using Vatsim.Vatis.Ui.Startup;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Updates;
using Vatsim.Vatis.Voice.Audio;
using Velopack.Locators;

namespace Vatsim.Vatis;

/// <summary>
/// The main application class.
/// </summary>
public class App : Application
{
    private const string SingleInstanceId = "{93C4C697-85B2-42B4-936F-E07AB2C53B82}";
    private static Mutex? s_singleInstanceMutex;
    private readonly string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "org.vatsim.vatis");
    private Container.ServiceProvider? _serviceProvider;
    private StartupWindow? _startupWindow;

    /// <summary>
    /// Initializes the application.
    /// </summary>
    public override void Initialize()
    {
        if (!Debugger.IsAttached)
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = "https://0df6303309d591db70c9848473373990@o477107.ingest.us.sentry.io/4508223788548096";
                options.StackTraceMode = StackTraceMode.Enhanced;
                options.IsGlobalModeEnabled = true;
                options.AutoSessionTracking = true;
                options.TracesSampleRate = 1.0;
                options.CacheDirectoryPath = _appDataPath;
            });
        }

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            Log.Error(ex, "RxAppException");

            if (SentrySdk.IsEnabled)
            {
                SentrySdk.CaptureException(ex);
                SentrySdk.FlushAsync().SafeFireAndForget();
            }

            ShowErrorAsync(ex.Message);
        });
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (IsAlreadyRunning())
                {
                    Shutdown();
                    return;
                }

                if (!Directory.Exists(_appDataPath))
                {
                    try
                    {
                        Directory.CreateDirectory(_appDataPath);
                    }
                    catch (Exception ex)
                    {
                        HandleError(ex, "Failed to create application data directory.", true);
                        return;
                    }
                }

                PathProvider.SetAppDataPath(_appDataPath);

                var arguments = ParseArguments(desktop.Args ?? []);

                _serviceProvider = new Container.ServiceProvider();
                SetupLogging(arguments.ContainsKey("--debug"));

                if (OperatingSystem.IsMacOS() && AppContext.BaseDirectory.StartsWith("/Volumes"))
                {
                    ShowErrorAsync(
                        "vATIS cannot be launched from a DMG volume. Please move vATIS to the Applications folder.",
                        fatal: true);
                    return;
                }

                var informationalVersion = Assembly.GetEntryAssembly()
                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                Log.Information($"vATIS version {informationalVersion} starting up");

                var websocket = _serviceProvider.GetService<IWebsocketService>();
                websocket.ApplicationExitRequested += OnApplicationExitRequested;
                await websocket.StartAsync();

                var appConfig = _serviceProvider.GetService<IAppConfig>();
                try
                {
                    appConfig.LoadConfig();
                }
                catch (FileNotFoundException)
                {
                    appConfig.SaveConfig();
                }

                TopMostViewModel.Instance.Initialize(appConfig);
                CompactWindowTopMostViewModel.Instance.Initialize(appConfig);

                _startupWindow = _serviceProvider.GetService<StartupWindow>();
                desktop.MainWindow = _startupWindow;
                _startupWindow.Show();
                _startupWindow.Activate();

                try
                {
                    await _serviceProvider.GetService<IAppConfigurationProvider>().Initialize();
                }
                catch (Exception ex)
                {
                    HandleError(ex, "Error initializing app configuration provider", true);
                    return;
                }

                try
                {
                    Log.Information("Checking for new client version...");
                    if (await _serviceProvider.GetService<IClientUpdater>().Run())
                    {
                        SentrySdk.Close();
                        await Log.CloseAndFlushAsync();
                        Shutdown();
                        return;
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode != HttpStatusCode.NotFound)
                    {
                        Log.Error(ex, "Error running client updater.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error running client updater.");
                }

                await CheckForProfileUpdatesAsync();
                await UpdateNavDataAsync();
                await UpdateAvailableVoicesAsync();

                // Show release notes of new version
                if (Program.IsUpdated && !appConfig.SuppressReleaseNotes)
                {
                    try
                    {
                        var locator = VelopackLocator.GetDefault(NullLogger.Instance);
                        var currentRelease = locator.GetLocalPackages()
                            .FirstOrDefault(x => x.Version == locator.CurrentlyInstalledVersion);
                        if (currentRelease?.NotesMarkdown != null)
                        {
                            var releaseNotes = _serviceProvider.GetService<ReleaseNotesDialog>();
                            if (releaseNotes.ViewModel != null)
                            {
                                releaseNotes.ViewModel.ReleaseNotes = currentRelease.NotesMarkdown;
                                if (await releaseNotes.ShowDialog<DialogResult>(_startupWindow) == DialogResult.Ok)
                                {
                                    appConfig.SuppressReleaseNotes = releaseNotes.ViewModel.SuppressReleaseNotes;
                                    appConfig.SaveConfig();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error showing release notes.");
                    }
                }

                _startupWindow.Close();

                var sessionManager = _serviceProvider.GetService<ISessionManager>();
                if (arguments.TryGetValue("--profile", out var profileId))
                {
                    Log.Information($"Launching vATIS with --profile {profileId}");
                    await sessionManager.StartSession(profileId);
                }
                else
                {
                    sessionManager.Run();
                }

                NativeAudio.Initialize();

                base.OnFrameworkInitializationCompleted();
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Unhandled Exception", true);
        }
    }

    private static bool IsAlreadyRunning()
    {
        s_singleInstanceMutex = new Mutex(true, @"Global\" + SingleInstanceId, out var createdNew);
        if (createdNew)
        {
            s_singleInstanceMutex.ReleaseMutex();
        }

        return !createdNew;
    }

    private static void SetupLogging(bool debugMode)
    {
        var logPath = Path.Combine(PathProvider.LogsFolderPath, "Log.txt");

        var config = new LoggerConfiguration().WriteTo.File(
            logPath,
            retainedFileCountLimit: 7,
            rollingInterval: RollingInterval.Day);

        if (debugMode)
        {
            config = config.WriteTo.Trace().MinimumLevel.Debug();
        }

        Log.Logger = config.CreateLogger();
    }

    private static void Shutdown()
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            desktop.Shutdown();
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => desktop.Shutdown());
        }
    }

    private static async void ShowErrorAsync(string error, bool fatal = false)
    {
        try
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => ShowErrorAsync(error, fatal));
                return;
            }

            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                var owner = lifetime.Windows.FirstOrDefault(x => x is { IsActive: true, IsVisible: true });
                if (owner != null)
                {
                    await MessageBox.ShowDialog(
                        owner,
                        "An error has occured. Please refer to the log file for details.\n\n" + error,
                        "Error",
                        MessageBoxButton.Ok,
                        MessageBoxIcon.Error);

                    if (fatal)
                    {
                        Shutdown();
                    }
                }
                else
                {
                    await MessageBox.Show(
                        "An error has occured. Please refer to the log file for details.\n\n" + error,
                        "Error",
                        MessageBoxButton.Ok,
                        MessageBoxIcon.Error);
                    Shutdown();
                }
            }
            else
            {
                await MessageBox.Show(
                    "An error has occured. Please refer to the log file for details.\n\n" + error,
                    "Error",
                    MessageBoxButton.Ok,
                    MessageBoxIcon.Error);
                Shutdown();
            }
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error during ShowError.");
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var parsedArgs = new Dictionary<string, string>();

        for (var i = 0; i < args.Length; i++)
        {
            // Check if the argument starts with a flag (e.g., --uri, --profile)
            if (args[i].StartsWith("--"))
            {
                var flag = args[i];

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    // Only store one value per flag (the last one encountered)
                    parsedArgs[flag] = args[i + 1];
                    i++; // Skip the next argument as it's the value for this flag
                }
                else
                {
                    // If no value follows the flag, store an empty string
                    parsedArgs[flag] = string.Empty;
                }
            }
        }

        return parsedArgs;
    }

    private void OnApplicationExitRequested(object? sender, EventArgs e)
    {
        Shutdown();
    }

    private void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs ex)
    {
        if (ex.Handled)
            return;

        try
        {
            Log.Error(ex.Exception, "UIThread_UnhandledException");

            if (SentrySdk.IsEnabled)
            {
                SentrySdk.CaptureException(ex.Exception);
                SentrySdk.FlushAsync().SafeFireAndForget();
            }

            ShowErrorAsync(ex.Exception.Message);
        }
        finally
        {
            ex.Handled = true;
        }
    }

    private async Task CheckForProfileUpdatesAsync()
    {
        if (_serviceProvider != null)
        {
            try
            {
                Log.Information("Checking for profile updates...");
                await _serviceProvider.GetService<IProfileRepository>().CheckForProfileUpdates();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error checking for profile updates", false);
            }
        }
    }

    private async Task UpdateAvailableVoicesAsync()
    {
        if (_serviceProvider != null)
        {
            EventBus.Instance.Publish(new StartupStatusChanged("Updating available voices..."));
            await _serviceProvider.GetService<ITextToSpeechService>().Initialize();
        }
    }

    private async Task UpdateNavDataAsync()
    {
        EventBus.Instance.Publish(new StartupStatusChanged("Checking for navdata updates..."));
        if (_serviceProvider != null)
        {
            await _serviceProvider.GetService<INavDataRepository>().CheckForUpdates();
            await _serviceProvider.GetService<INavDataRepository>().Initialize();
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _serviceProvider?.GetService<IWebsocketService>().StopAsync();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception ex)
            return;

        if (SentrySdk.IsEnabled)
        {
            ex.SetSentryMechanism("UnhandledException", handled: false);
            SentrySdk.CaptureException(ex);
            SentrySdk.FlushAsync().SafeFireAndForget();
            Log.Warning(ex, "Unhandled {Type}: {Message}", ex.GetType().Name, ex.Message);
        }
        else
        {
            Log.Fatal(ex, "Unhandled {Type}: {Message}", ex.GetType().Name, ex.Message);
        }

        ShowErrorAsync(ex.Message);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Observed || e.Exception is not Exception unobservedEx)
            return;

        try
        {
            var originalException = unobservedEx.InnerException ?? unobservedEx;
            Log.Error(originalException, "OnUnobservedTaskException");

            if (SentrySdk.IsEnabled)
            {
                originalException.SetSentryMechanism("UnobservedTaskException");
                SentrySdk.CaptureException(originalException);
                SentrySdk.FlushAsync().SafeFireAndForget();
            }

            ShowErrorAsync(originalException.Message);

            // Consider the exception observed if we were able to show the error to the user.
            e.SetObserved();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to show UnobservedTaskException notification.");
        }
    }

    private void HandleError(Exception ex, string context, bool fatal)
    {
        _startupWindow?.Close();

        Log.Error(ex, context);

        if (SentrySdk.IsEnabled)
        {
            SentrySdk.CaptureException(ex);
            SentrySdk.FlushAsync().SafeFireAndForget();
        }

        ShowErrorAsync(ex.Message, fatal);
    }
}
