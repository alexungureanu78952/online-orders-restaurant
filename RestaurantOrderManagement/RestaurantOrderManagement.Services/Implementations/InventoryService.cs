using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Interfaces;

namespace RestaurantOrderManagement.Services.Implementations
{
    public class InventoryService : IInventoryService
    {
        private readonly ProductRepository _productRepository;
        private readonly IConfigurationService _configurationService;

        public InventoryService(ProductRepository productRepository, IConfigurationService configurationService)
        {
            _productRepository = productRepository;
            _configurationService = configurationService;
        }


        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            try
            {
                var lowStockProducts = await _productRepository.GetLowStockProductsAsync();
                return lowStockProducts.ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve low-stock products", ex);
            }
        }


        public async Task<bool> UpdateProductInventoryAsync(int productId, int quantityChange)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Invalid product ID", nameof(productId));

               
                var result = await _productRepository.UpdateInventoryAsync(productId, quantityChange);
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update inventory for product {productId}", ex);
            }
        }


        public async Task<Product> RestockProductAsync(int productId, int restockQuantity)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Invalid product ID", nameof(productId));

                if (restockQuantity <= 0)
                    throw new ArgumentException("Restock quantity must be positive", nameof(restockQuantity));


                var updated = await _productRepository.UpdateInventoryAsync(productId, restockQuantity);
                if (!updated)
                    throw new InvalidOperationException("Failed to restock product");


                return await GetProductWithInventoryAsync(productId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restock product {productId}", ex);
            }
        }


        public async Task<Product> GetProductWithInventoryAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Invalid product ID", nameof(productId));

                var product = await _productRepository.GetProductByIdAsync(productId);
                if (product == null)
                    throw new KeyNotFoundException($"Product {productId} not found");

                return product;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve product {productId}", ex);
            }
        }


        public async Task<int> GetLowStockThresholdAsync()
        {
            try
            {
                return await _configurationService.GetLowStockThresholdAsync();
            }
            catch
            {
                return 500;
            }
        }
    }
}
