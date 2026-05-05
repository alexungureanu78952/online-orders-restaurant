using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;

namespace RestaurantOrderManagement.Services.Implementations
{
    /// <summary>
    /// Service for managing application configuration settings
    /// Caches configuration in memory on startup for performance
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ConfigurationRepository _configRepository;
        private Configuration _cachedConfig;
        private readonly object _lockObject = new object();

        public ConfigurationService(ConfigurationRepository configRepository)
        {
            _configRepository = configRepository;
        }

        /// <summary>
        /// Get current configuration (from cache or database)
        /// </summary>
        public async Task<Configuration> GetConfigurationAsync()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            lock (_lockObject)
            {
                if (_cachedConfig != null)
                    return _cachedConfig;

                _cachedConfig = _configRepository.GetConfigurationAsync().Result;
            }

            return _cachedConfig;
        }

        /// <summary>
        /// Refresh configuration from database
        /// </summary>
        public async Task<Configuration> RefreshConfigurationAsync()
        {
            lock (_lockObject)
            {
                _cachedConfig = null;
            }

            _cachedConfig = await _configRepository.GetConfigurationAsync();
            return _cachedConfig;
        }

        /// <summary>
        /// Get minimum order amount for free shipping
        /// </summary>
        public async Task<decimal> GetMinOrderForFreeShippingAsync()
        {
            var config = await GetConfigurationAsync();
            return config.MinOrderForFreeShipping;
        }

        /// <summary>
        /// Get standard shipping fee
        /// </summary>
        public async Task<decimal> GetShippingFeeAsync()
        {
            var config = await GetConfigurationAsync();
            return config.ShippingFee;
        }

        /// <summary>
        /// Get large order discount threshold and percentage
        /// </summary>
        public async Task<(decimal Threshold, decimal Percent)> GetLargeOrderDiscountAsync()
        {
            var config = await GetConfigurationAsync();
            return (config.LargeOrderDiscountThreshold, config.LargeOrderDiscountPercent);
        }

        /// <summary>
        /// Get low stock threshold in grams
        /// </summary>
        public async Task<int> GetLowStockThresholdAsync()
        {
            var config = await GetConfigurationAsync();
            return config.LowStockThreshold;
        }

        /// <summary>
        /// Get frequent order discount (if applicable)
        /// </summary>
        public async Task<(int Threshold, int TimeWindow, decimal Percent)> GetFrequentOrderDiscountAsync()
        {
            var config = await GetConfigurationAsync();
            return (config.FrequentOrderThreshold, config.FrequentOrderTimeWindow, config.FrequentOrderDiscountPercent);
        }

        /// <summary>
        /// Update configuration settings
        /// </summary>
        public async Task<bool> UpdateConfigurationAsync(decimal? minOrderForFreeShipping, decimal? shippingFee,
            decimal? largeOrderDiscountThreshold, decimal? largeOrderDiscountPercent, int? lowStockThreshold)
        {
            var result = await _configRepository.UpdateConfigurationAsync(
                minOrderForFreeShipping, shippingFee, largeOrderDiscountThreshold, largeOrderDiscountPercent, lowStockThreshold);

            if (result)
            {
                // Refresh cache after update
                await RefreshConfigurationAsync();
            }

            return result;
        }
    }
}
