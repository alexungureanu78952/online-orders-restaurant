using System;
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
                        if (!string.IsNullOrWhiteSpace(connectionString))
                            return connectionString;
                    }
                }
                catch
                {
                    // Fall back to the local SQL Server default below.
                }
            }

            return "Server=.;Database=RestaurantOrderManagement;Trusted_Connection=True;TrustServerCertificate=True;";
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
    }
}
