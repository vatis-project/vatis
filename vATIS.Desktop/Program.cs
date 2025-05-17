// <copyright file="Program.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;
using NuGet.Versioning;
using Serilog;
using Velopack;

namespace Vatsim.Vatis;

/// <summary>
/// The main entry point for the application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Gets a value indicating whether the application has been updated, and the release notes should be displayed.
    /// </summary>
    public static bool IsUpdated { get; private set; }

    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            VelopackApp.Build().WithFirstRun(OnFirstRun).WithRestarted(OnRestarted).Run();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled Exception");
        }

        Log.CloseAndFlush();
    }

    private static void OnRestarted(SemanticVersion version)
    {
        IsUpdated = true;
    }

    private static void OnFirstRun(SemanticVersion version)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var upgradeAssistantPath = Path.Combine(localAppData, "vatis-upgrade-assistant");
            if (Directory.Exists(upgradeAssistantPath))
            {
                Directory.Delete(upgradeAssistantPath, recursive: true);
                Log.Information("Deleted upgrade assistant directory: {UpgradeAssistantPath}", upgradeAssistantPath);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to delete upgrade assistant directory");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .LogToTrace();
}
