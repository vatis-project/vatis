// <copyright file="Program.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;
using DevServer.Hub;
using DevServer.Services;
using Serilog;

namespace DevServer;

/// <summary>
/// Defines the entry point of the application and its configuration.
/// </summary>
public static class Program
{
    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.File("./logs/log-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting dev server...");

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.AddSignalR().AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddSingleton<IMetarRepository, MetarRepository>();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapControllers();
            app.MapFallbackToFile("index.html");
            app.MapHub<ClientHub>("/hub");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
