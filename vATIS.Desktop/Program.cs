// <copyright file="Program.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Serilog;
using Velopack;

namespace Vatsim.Vatis;

/// <summary>
/// Entry point of the Vatsim.Vatis application.
/// This class contains the Main method which is the startup method of the application.
/// It configures the synchronization context, initializes the underlying framework,
/// and handles application-level exceptions.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The entry point of the Vatsim.Vatis application. This method is responsible for initializing
    /// the synchronization context, starting the application, and handling unhandled exceptions.
    /// </summary>
    /// <param name="args">An array of command-line arguments passed to the application.</param>
    /// <remarks>
    /// The method sets up the synchronization context using <see cref="SynchronizationContext"/>
    /// before initializing and starting the Avalonia UI framework. It ensures exception handling
    /// for unexpected runtime errors and logs them using <see cref="Serilog.Log"/>.
    /// </remarks>
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            VelopackApp.Build().Run();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled Exception");
        }

        Log.CloseAndFlush();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .LogToTrace();
    }
}
