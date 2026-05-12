using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;

namespace RestaurantOrderManagement.Services.Implementations
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ConfigurationRepository _configRepository;
        private Configuration? _cachedConfig;
        private readonly object _lockObject = new object();

        public ConfigurationService(ConfigurationRepository configRepository)
        {
            _configRepository = configRepository;
        }


        public async Task<Configuration> GetConfigurationAsync()
        {
            var cachedConfig = _cachedConfig;
            if (cachedConfig != null)
                return cachedConfig;

            var loadedConfig = await _configRepository.GetConfigurationAsync();
            lock (_lockObject)
            {
                _cachedConfig ??= loadedConfig;
                return _cachedConfig;
            }
        }


        public async Task<Configuration> RefreshConfigurationAsync()
        {
            var loadedConfig = await _configRepository.GetConfigurationAsync();
            lock (_lockObject)
            {
                _cachedConfig = loadedConfig;
                return _cachedConfig;
            }
        }


        public async Task<decimal> GetMinOrderForFreeShippingAsync()
        {
            var config = await GetConfigurationAsync();
            return config.MinOrderForFreeShipping;
        }


        public async Task<decimal> GetShippingFeeAsync()
        {
            var config = await GetConfigurationAsync();
            return config.ShippingFee;
        }


        public async Task<(decimal Threshold, decimal Percent)> GetLargeOrderDiscountAsync()
        {
            var config = await GetConfigurationAsync();
            return (config.LargeOrderDiscountThreshold, config.LargeOrderDiscountPercent);
        }


        public async Task<int> GetLowStockThresholdAsync()
        {
            var config = await GetConfigurationAsync();
            return config.LowStockThreshold;
        }


        public async Task<(int Threshold, int TimeWindow, decimal Percent)> GetFrequentOrderDiscountAsync()
        {
            var config = await GetConfigurationAsync();
            return (config.FrequentOrderThreshold, config.FrequentOrderTimeWindow, config.FrequentOrderDiscountPercent);
        }


        public async Task<bool> UpdateConfigurationAsync(decimal? minOrderForFreeShipping, decimal? shippingFee,
            decimal? largeOrderDiscountThreshold, decimal? largeOrderDiscountPercent, int? lowStockThreshold)
        {
            var result = await _configRepository.UpdateConfigurationAsync(
                minOrderForFreeShipping, shippingFee, largeOrderDiscountThreshold, largeOrderDiscountPercent, lowStockThreshold);

            if (result)
            {
                await RefreshConfigurationAsync();
            }

            return result;
        }
    }
}
