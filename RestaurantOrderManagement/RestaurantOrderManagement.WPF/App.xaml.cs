using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RestaurantOrderManagement.ViewModels;
using RestaurantOrderManagement.WPF.ViewModels;

namespace RestaurantOrderManagement
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private IServiceScope? _applicationScope;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
            _applicationScope = _serviceProvider.CreateScope();

            var mainWindow = _applicationScope.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _applicationScope?.Dispose();
            _serviceProvider?.Dispose();

            base.OnExit(e);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            RestaurantOrderManagement.Services.Startup.ConfigureServices(services, ResolveConnectionString());

            services.AddScoped<MainWindow>();
            services.AddScoped<MainWindowViewModel>();
            services.AddScoped<MenuBrowseViewModel>();
            services.AddScoped<SearchViewModel>();
            services.AddScoped<RegistrationViewModel>();
            services.AddScoped<OrderCartViewModel>();
            services.AddScoped<OrderHistoryViewModel>();
            services.AddScoped<OrderManagementViewModel>();
            services.AddScoped<ProductManagementViewModel>();
            services.AddScoped<InventoryViewModel>();
            services.AddScoped<ReportsViewModel>();
        }

        private static string ResolveConnectionString()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("RESTAURANT_DB_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment;

            var environmentValues = LoadDatabaseEnvironmentValues();
            var fromDatabaseEnvironment = BuildConnectionString(environmentValues);
            if (!string.IsNullOrWhiteSpace(fromDatabaseEnvironment))
                return fromDatabaseEnvironment;

            var appSettingsPath = FindAppSettingsFile();
            if (appSettingsPath != null)
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
                    if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings) &&
                        connectionStrings.TryGetProperty("RestaurantDb", out var restaurantDb))
                    {
                        var connectionString = restaurantDb.GetString();
                        if (!string.IsNullOrWhiteSpace(connectionString) && !UsesIntegratedSecurity(connectionString))
                            return connectionString;
                    }
                }
                catch
                {
                    // Fall back to the local SQL Server default below.
                }
            }

            return "Server=localhost,1433;Database=RestaurantOrderManagement;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;Encrypt=False;";
        }

        private static Dictionary<string, string> LoadDatabaseEnvironmentValues()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in new[]
            {
                "DATABASE_SERVER",
                "DATABASE_PORT",
                "DATABASE_NAME",
                "DATABASE_USERNAME",
                "DATABASE_PASSWORD"
            })
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(value))
                    values[name] = value;
            }

            var envPath = FindEnvFile();
            if (envPath == null)
                return values;

            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var separator = trimmed.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = trimmed[..separator].Trim();
                var value = trimmed[(separator + 1)..].Trim().Trim('"', '\'');
                if (!values.ContainsKey(key))
                    values[key] = value;
            }

            return values;
        }

        private static string? BuildConnectionString(IReadOnlyDictionary<string, string> values)
        {
            if (!values.TryGetValue("DATABASE_PASSWORD", out var password) || string.IsNullOrWhiteSpace(password))
                return null;

            values.TryGetValue("DATABASE_SERVER", out var server);
            values.TryGetValue("DATABASE_PORT", out var port);
            values.TryGetValue("DATABASE_NAME", out var database);
            values.TryGetValue("DATABASE_USERNAME", out var username);

            server = NormalizeDatabaseServer(server);
            database = string.IsNullOrWhiteSpace(database) ? "RestaurantOrderManagement" : database;
            username = string.IsNullOrWhiteSpace(username) ? "sa" : username;

            if (!string.IsNullOrWhiteSpace(port) && !server.Contains(','))
                server = $"{server},{port}";

            return $"Server={server};Database={database};User Id={username};Password={password};TrustServerCertificate=True;Encrypt=False;";
        }

        private static string NormalizeDatabaseServer(string? server)
        {
            if (string.IsNullOrWhiteSpace(server))
                return "localhost";

            return string.Equals(server, "sqlserver", StringComparison.OrdinalIgnoreCase)
                ? "localhost"
                : server;
        }

        private static bool UsesIntegratedSecurity(string connectionString)
        {
            return connectionString.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("Integrated Security=True", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("IntegratedSecurity=True", StringComparison.OrdinalIgnoreCase);
        }

        private static string? FindAppSettingsFile()
        {
            foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var directory = new DirectoryInfo(startPath);
                while (directory != null)
                {
                    var candidate = Path.Combine(directory.FullName, "appsettings.json");
                    if (File.Exists(candidate))
                        return candidate;

                    directory = directory.Parent;
                }
            }

            return null;
        }

        private static string? FindEnvFile()
        {
            foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var directory = new DirectoryInfo(startPath);
                while (directory != null)
                {
                    var candidate = Path.Combine(directory.FullName, ".env");
                    if (File.Exists(candidate))
                        return candidate;

                    directory = directory.Parent;
                }
            }

            return null;
        }
    }
}
