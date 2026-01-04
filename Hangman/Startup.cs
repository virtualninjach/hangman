using AutoMapper;
using Hangman.Business;
using Hangman.Models;
using Hangman.Repository;
using Hangman.Repository.Interfaces;
using Hangman.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hangman
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            // Swagger/OpenAPI configuration
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Hangman API",
                    Version = "v1",
                    Description = "A multiplayer Hangman game API for creating rooms, managing players, and playing word guessing games.",
                    Contact = new OpenApiContact
                    {
                        Name = "Hangman API",
                        Url = new System.Uri("https://github.com/virtualninjach/hangman")
                    }
                });

                // Include XML comments for better documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Group endpoints by controller
                c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
                c.DocInclusionPredicate((name, api) => true);
            });

            // injection of other services
            services.AddHttpContextAccessor()
                .AddDbContext<HangmanDbContext>(options =>
                    options.UseNpgsql(Configuration.GetConnectionString("DBConnection")))
                .AddScoped(typeof(IHangmanRepositoryAsync<>), typeof(HangmanRepositoryAsync<>)) // generic repository
                .AddScoped<IGameRoomServiceAsync, GameRoomServiceAsync>()
                .AddScoped<IPlayerServiceAsync, PlayerServiceAsync>()
                .AddScoped<IHangmanGame, HangmanGame>()
                .AddAutoMapper(typeof(Startup))
                .AddControllers()
                .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);  // ignore loops when serializing JSON
            
            // ADD THIS LINE:
            services.AddRazorPages();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            logger.LogInformation("Configuring start up with environment: {EnvironmentName}", env.EnvironmentName);
            logger.LogInformation("Application Name: {ApplicationName}", env.ApplicationName);
            logger.LogInformation("Content Root Path: {ContentRootPath}", env.ContentRootPath);
            logger.LogInformation("Web Root Path: {WebRootPath}", env.WebRootPath);

            if (env.IsDevelopment() || env.IsEnvironment("Local"))
            {
                app.UseDeveloperExceptionPage();
                logger.LogInformation("Developer Exception Page enabled");
            }

            // Enable Swagger middleware
            app.UseSwagger();
            logger.LogInformation("Swagger enabled at /swagger/v1/swagger.json");
            
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hangman API v1");
                c.RoutePrefix = "api-docs"; // Access Swagger UI at /api-docs
                c.DocumentTitle = "Hangman API Documentation";
                c.DefaultModelsExpandDepth(2);
                c.DefaultModelExpandDepth(2);
            });
            logger.LogInformation("Swagger UI enabled at /api-docs");

            // Only use HTTPS redirection in production
            if (!env.IsDevelopment() && !env.IsEnvironment("Local"))
            {
                app.UseHttpsRedirection();
                logger.LogInformation("HTTPS redirection enabled");
            }
            else
            {
                logger.LogInformation("HTTPS redirection disabled (Development/Local)");
            }

            app.UseSerilogRequestLogging();

            // ADD THIS LINE before app.UseRouting():
            app.UseSession();

            app.UseRouting();
            app.UseAuthorization();
    
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages(); // ADD THIS LINE
            });

            // Migrations and seed db (when in development/compose ONLY)
            try
            {
                Migrate(app, logger,
                    executeSeedDb: env.IsDevelopment() || env.IsEnvironment("Local") || env.IsEnvironment("DockerCompose"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply migrations or seed database");
                throw;
            }
        }

        /**
         * Applies possible missing migrations from the database.
         */
        public static void Migrate(IApplicationBuilder app, ILogger<Startup> logger, bool executeSeedDb = false)
        {
            try
            {
                using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
                using var context = serviceScope.ServiceProvider.GetService<HangmanDbContext>();

                logger.LogInformation("Checking for pending migrations...");

                // always execute possible missing migrations
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                        pendingMigrations.Count, 
                        string.Join(", ", pendingMigrations));
                    context.Database.Migrate();
                    logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    logger.LogInformation("No pending migrations");
                }

                // seeding DB only when asked
                if (!executeSeedDb) return;

                logger.LogInformation("Seeding the database...");
                SeedDb(context, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during migration or seeding");
                throw;
            }
        }

        /**
         * Seeds DB with pre-defined entities/models.
         */
        private static void SeedDb(HangmanDbContext context, ILogger<Startup> logger)
        {
            try
            {
                if (context.GameRooms.Any())
                {
                    logger.LogInformation("Database has already been seeded. Skipping it...");
                    return;
                }

                logger.LogInformation("Saving entities...");
                var gameRooms = new List<GameRoom>
                {
                    new GameRoom {Name = "Game Room 1"},
                    new GameRoom {Name = "Game Room 2"},
                    new GameRoom {Name = "Game Room 3"}
                };
                context.AddRange(gameRooms);
                context.SaveChanges();

                logger.LogInformation("Database has been seeded successfully with {Count} game rooms", gameRooms.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding database");
                throw;
            }
        }
    }
}