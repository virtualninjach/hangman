using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Hangman
{
    public class Program
    {
        private static void ConfigureLogger()
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

                Log.Information("Logger configured successfully");
            }
            catch (Exception ex)
            {
                // Fallback to console logger if configuration fails
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .CreateLogger();

                Log.Fatal(ex, "Failed to configure logger from appsettings.json");
                throw;
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                ConfigureLogger();

                Log.Information("Starting Hangman API in VERBOSE mode...");
                Log.Information("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
                Log.Information("Command line arguments: {Args}", string.Join(" ", args));
                Log.Information("Current directory: {Directory}", Directory.GetCurrentDirectory());

                var host = CreateHostBuilder(args).Build();

                Log.Information("Host built successfully, attempting to launch browser...");

                // Auto-launch Swagger UI in browser
                LaunchSwaggerInBrowser();

                Log.Information("Starting host...");
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");

                // Also write to console in case Serilog isn't working
                Console.WriteLine("===============================================");
                Console.WriteLine("FATAL ERROR:");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("===============================================");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            try
            {
                Log.Information("Creating host builder...");

                return Host.CreateDefaultBuilder(args)
                    .UseSerilog()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();

                        // Enable verbose logging for all sources
                        webBuilder.ConfigureLogging((context, logging) =>
                        {
                            Log.Information("Configuring logging with verbose settings");
                        });
                    });
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to create host builder");
                throw;
            }
        }

        /// <summary>
        /// Launches the Swagger documentation in the default browser
        /// </summary>
        private static void LaunchSwaggerInBrowser()
        {
            try
            {
                var url = "http://localhost:8080/api-docs";
                Log.Information("Attempting to launch Swagger UI at: {Url}", url);

                // Delay to allow the server to start
                System.Threading.Thread.Sleep(2000);

                // Cross-platform browser launch
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }

                Log.Information("Swagger UI launched successfully");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to automatically launch Swagger UI in browser. Please navigate to http://localhost:8080/api-docs manually");
            }
        }
    }
}
