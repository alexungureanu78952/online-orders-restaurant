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

        
        public async Task<Configuration> GetConfigurationAsync()
        {
            var configuration = await _context.Configurations
                .FromSqlRaw("EXEC dbo.sp_GetConfiguration")
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return configuration ?? new Configuration
            {
                MinOrderForFreeShipping = 100,
                ShippingFee = 15,
                LargeOrderDiscountThreshold = 200,
                LargeOrderDiscountPercent = 10,
                FrequentOrderThreshold = 5,
                FrequentOrderTimeWindow = 30,
                FrequentOrderDiscountPercent = 8,
                LowStockThreshold = 500
            };
        }

        
        public async Task<bool> UpdateConfigurationAsync(decimal? minOrderForFreeShipping, decimal? shippingFee,
            decimal? largeOrderDiscountThreshold, decimal? largeOrderDiscountPercent, int? lowStockThreshold)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_UpdateConfiguration @MinOrderForFreeShipping = {0}, @ShippingFee = {1}, @LargeOrderDiscountThreshold = {2}, @LargeOrderDiscountPercent = {3}, @LowStockThreshold = {4}",
                minOrderForFreeShipping ?? (object)DBNull.Value,
                shippingFee ?? (object)DBNull.Value,
                largeOrderDiscountThreshold ?? (object)DBNull.Value,
                largeOrderDiscountPercent ?? (object)DBNull.Value,
                lowStockThreshold ?? (object)DBNull.Value);
            return true;
        }
    }
}
