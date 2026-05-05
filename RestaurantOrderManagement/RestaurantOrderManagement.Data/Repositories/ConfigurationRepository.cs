using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Context;
using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Data.Repositories
{
    public class ConfigurationRepository : GenericRepository<Configuration>
    {
        public ConfigurationRepository(RestaurantDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get current configuration using parameterized stored procedure sp_GetConfiguration
        /// </summary>
        public async Task<Configuration> GetConfigurationAsync()
        {
            return await _context.Configurations
                .FromSqlRaw("EXEC dbo.sp_GetConfiguration")
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Update configuration using parameterized stored procedure sp_UpdateConfiguration
        /// </summary>
        public async Task<bool> UpdateConfigurationAsync(decimal? minOrderForFreeShipping, decimal? shippingFee,
            decimal? largeOrderDiscountThreshold, decimal? largeOrderDiscountPercent, int? lowStockThreshold)
        {
            var result = await _context.Database.ExecuteAsync(
                "EXEC dbo.sp_UpdateConfiguration @MinOrderForFreeShipping = {0}, @ShippingFee = {1}, @LargeOrderDiscountThreshold = {2}, @LargeOrderDiscountPercent = {3}, @LowStockThreshold = {4}",
                minOrderForFreeShipping, shippingFee, largeOrderDiscountThreshold, largeOrderDiscountPercent, lowStockThreshold);
            return result > 0;
        }
    }
}
