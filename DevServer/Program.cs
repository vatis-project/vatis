using System.Text.Json.Serialization;
using DevServer.Hub;
using Serilog;

namespace DevServer;

public static class Program
{
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

            builder.Services.AddSingleton<ICacheService, CacheService>();

            var app = builder.Build();
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapGet("/", async (context) => { await context.Response.WriteAsync($"vATIS Dev Server"); });

            app.MapControllers();
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
