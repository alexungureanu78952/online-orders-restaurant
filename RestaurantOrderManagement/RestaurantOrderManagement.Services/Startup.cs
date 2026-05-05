using Microsoft.Extensions.DependencyInjection;
using RestaurantOrderManagement.Data.Context;
using Microsoft.EntityFrameworkCore;
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

            // Register services
            services.AddScoped<IProductService, ProductService>();

            // Logging
            services.AddLogging(builder => builder.AddConsole());
        }
    }
}
