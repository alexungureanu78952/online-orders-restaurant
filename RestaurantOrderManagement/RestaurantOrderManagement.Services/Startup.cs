using Microsoft.Extensions.DependencyInjection;
using RestaurantOrderManagement.Data.Context;
using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Interfaces;
using RestaurantOrderManagement.Services.Implementations;
using Microsoft.Extensions.Logging;

namespace RestaurantOrderManagement.Services
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services, string connectionString)
        {
            // Configure DbContext
            services.AddDbContext<RestaurantDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Register repositories
            services.AddScoped<ProductRepository>();
            services.AddScoped<OrderRepository>();
            services.AddScoped<UserRepository>();
            services.AddScoped<ConfigurationRepository>();

            // Register services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IConfigurationService, ConfigurationService>();

            // Logging
            services.AddLogging(builder => builder.AddConsole());
        }
    }
}
