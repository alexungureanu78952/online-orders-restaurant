using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Services
{
    public interface IConfigurationService
    {
        Task<Configuration> GetConfigurationAsync();
        Task<Configuration> RefreshConfigurationAsync();
        Task<decimal> GetMinOrderForFreeShippingAsync();
        Task<decimal> GetShippingFeeAsync();
        Task<(decimal Threshold, decimal Percent)> GetLargeOrderDiscountAsync();
        Task<int> GetLowStockThresholdAsync();
        Task<(int Threshold, int TimeWindow, decimal Percent)> GetFrequentOrderDiscountAsync();
        Task<bool> UpdateConfigurationAsync(decimal? minOrderForFreeShipping, decimal? shippingFee,
            decimal? largeOrderDiscountThreshold, decimal? largeOrderDiscountPercent, int? lowStockThreshold);
    }
}
