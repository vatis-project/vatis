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
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
using Sentry;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Startup;
using Vatsim.Vatis.Ui.ViewModels;
using Vatsim.Vatis.Updates;
using Vatsim.Vatis.Voice.Audio;

namespace Vatsim.Vatis;

/// <summary>
/// Represents the main application class for the VATSIM vATIS application.
/// This class is responsible for initializing the application, setting up exception handling,
/// and determining the lifecycle of the app.
/// </summary>
/// <remarks>
/// The <see cref="App"/> class extends the <see cref="Avalonia.Application"/> base class.
/// Configuration includes the setup of Sentry SDK for error tracking and custom exception handling mechanisms.
/// </remarks>
public class App : Application
{
    private const string SingleInstanceId = "{93C4C697-85B2-42B4-936F-E07AB2C53B82}";

    private readonly string appDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "org.vatsim.vatis");

    private Container.ServiceProvider? serviceProvider;
    private StartupWindow? startupWindow;

    /// <summary>
    /// Initializes the application, configuring essential components and exception handling mechanisms.
    /// </summary>
    /// <remarks>
    /// This method sets up the application by loading XAML definitions and initializing critical services.
    /// It integrates with Sentry SDK for error monitoring, configures global exception handlers for the application,
    /// and applies a custom default exception handler for ReactiveUI.
    /// </remarks>
    /// <exception cref="Exception">
    /// Exceptions occurring during ReactiveUI operations or unhandled exceptions will be captured and logged.
    /// </exception>
    public override void Initialize()
    {
        if (!Debugger.IsAttached)
        {
            SentrySdk.Init(
                options =>
                {
                    options.Dsn =
                        "https://0df6303309d591db70c9848473373990@o477107.ingest.us.sentry.io/4508223788548096";
                    options.AutoSessionTracking = true;
                    options.TracesSampleRate = 1.0;
                    options.CacheDirectoryPath = this.appDataPath;
                });
        }

        AppDomain.CurrentDomain.UnhandledException += this.OnUnhandledException;
        TaskScheduler.UnobservedTaskException += this.OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += this.UIThread_UnhandledException;
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(
            ex =>
            {
                Log.Error(ex, "RxAppException");
                if (SentrySdk.IsEnabled)
                {
                    SentrySdk.CaptureException(ex);
                }

                ShowErrorAsync(ex.Message);
            });
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Finalizes application initialization once all framework components have been set up.
    /// </summary>
    /// <remarks>
    /// This method is responsible for completing the initialization process of the application.
    /// It determines and configures the specific application lifetime, such as a desktop or non-desktop environment.
    /// Additionally, it ensures that unhandled exceptions during the initialization are properly managed
    /// and routed through the error handling mechanism.
    /// </remarks>
    /// <exception cref="Exception">
    /// Thrown when an unhandled exception occurs during the finalization of the application's framework initialization.
    /// These exceptions are logged and managed via the custom error handling logic.
    /// </exception>
    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (!Directory.Exists(this.appDataPath))
                {
                    try
                    {
                        Directory.CreateDirectory(this.appDataPath);
                    }
                    catch (Exception ex)
                    {
                        this.HandleError(ex, "Failed to create application data directory.", true);
                        return;
                    }
                }

                PathProvider.SetAppDataPath(this.appDataPath);

                var arguments = ParseArguments(desktop.Args ?? []);

                this.serviceProvider = new Container.ServiceProvider();
                SetupLogging(arguments.ContainsKey("--debug"));

                if (OperatingSystem.IsMacOS() && AppContext.BaseDirectory.StartsWith("/Volumes"))
                {
                    ShowErrorAsync(
                        "vATIS cannot be launched from a DMG volume. " +
                        "Please move vATIS to the Applications folder.",
                        true);
                    return;
                }

                var informationalVersion = Assembly.GetEntryAssembly()
                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                Log.Information($"vATIS version {informationalVersion} starting up");

                var appConfig = this.serviceProvider.GetService<IAppConfig>();
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

                this.startupWindow = this.serviceProvider.GetService<StartupWindow>();
                desktop.MainWindow = this.startupWindow;
                this.startupWindow.Show();
                this.startupWindow.Activate();

                _ = new Mutex(true, SingleInstanceId, out var createdNew);
                if (!createdNew)
                {
                    Shutdown();
                    return;
                }

                try
                {
                    await this.serviceProvider.GetService<IAppConfigurationProvider>().Initialize();
                }
                catch (Exception ex)
                {
                    this.HandleError(ex, "Error initializing app configuration provider", true);
                    return;
                }

                try
                {
                    Log.Information("Checking for new client version...");
                    if (await this.serviceProvider.GetService<IClientUpdater>().Run())
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

                await this.CheckForProfileUpdatesAsync();
                await this.UpdateNavDataAsync();
                await this.UpdateAvailableVoicesAsync();

                this.startupWindow.Close();

                var sessionManager = this.serviceProvider.GetService<ISessionManager>();
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
            this.HandleError(ex, "Unhandled Exception", true);
        }
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
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
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

    private void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs ex)
    {
        try
        {
            Log.Error(ex.Exception, "UIThread_UnhandledException");
            if (SentrySdk.IsEnabled)
            {
                SentrySdk.CaptureException(ex.Exception);
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
        if (this.serviceProvider != null)
        {
            try
            {
                Log.Information("Checking for profile updates...");
                await this.serviceProvider.GetService<IProfileRepository>().CheckForProfileUpdates();
            }
            catch (Exception ex)
            {
                this.HandleError(ex, "Error checking for profile updates", false);
            }
        }
    }

    private async Task UpdateAvailableVoicesAsync()
    {
        if (this.serviceProvider != null)
        {
            MessageBus.Current.SendMessage(new StartupStatusChanged("Updating available voices..."));
            await this.serviceProvider.GetService<ITextToSpeechService>().Initialize();
        }
    }

    private async Task UpdateNavDataAsync()
    {
        MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for navdata updates..."));
        if (this.serviceProvider != null)
        {
            await this.serviceProvider.GetService<INavDataRepository>().CheckForUpdates();
            await this.serviceProvider.GetService<INavDataRepository>().Initialize();
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Error(ex, "OnUnhandledException");
            if (SentrySdk.IsEnabled)
            {
                SentrySdk.CaptureException(ex);
            }

            ShowErrorAsync(ex.Message);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs ex)
    {
        Log.Error(ex.Exception, "OnUnobservedTaskException");
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.CaptureException(ex.Exception);
        }

        ShowErrorAsync(ex.Exception.Message);
    }

    private void HandleError(Exception ex, string context, bool fatal)
    {
        this.startupWindow?.Close();
        Log.Error(ex, context);
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.CaptureException(ex);
        }

        ShowErrorAsync(ex.Message, fatal);
    }
}
