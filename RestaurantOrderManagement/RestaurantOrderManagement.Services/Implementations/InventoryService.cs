using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Interfaces;

namespace RestaurantOrderManagement.Services.Implementations
{
    /// <summary>
    /// Service for inventory management including low-stock detection and restock operations
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly IProductRepository _productRepository;
        private readonly IConfigurationService _configurationService;

        public InventoryService(IProductRepository productRepository, IConfigurationService configurationService)
        {
            _productRepository = productRepository;
            _configurationService = configurationService;
        }

        /// <summary>
        /// Get all products with inventory below low-stock threshold
        /// </summary>
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

        /// <summary>
        /// Update product inventory by adding or removing quantity
        /// Used for order creation (negative change) and cancellation (positive change)
        /// </summary>
        public async Task<bool> UpdateProductInventoryAsync(int productId, int quantityChange)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Invalid product ID", nameof(productId));

                // Negative changes during order creation, positive during cancellation or restock
                var result = await _productRepository.UpdateInventoryAsync(productId, quantityChange);
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update inventory for product {productId}", ex);
            }
        }

        /// <summary>
        /// Restock product by specified quantity (manual restock operation for employees)
        /// </summary>
        public async Task<Product> RestockProductAsync(int productId, int restockQuantity)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Invalid product ID", nameof(productId));

                if (restockQuantity <= 0)
                    throw new ArgumentException("Restock quantity must be positive", nameof(restockQuantity));

                // Update inventory by adding the restock quantity
                var updated = await _productRepository.UpdateInventoryAsync(productId, restockQuantity);
                if (!updated)
                    throw new InvalidOperationException("Failed to restock product");

                // Return updated product details
                return await GetProductWithInventoryAsync(productId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restock product {productId}", ex);
            }
        }

        /// <summary>
        /// Get product with current inventory details
        /// </summary>
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

        /// <summary>
        /// Get low stock threshold from configuration
        /// </summary>
        public async Task<int> GetLowStockThresholdAsync()
        {
            try
            {
                return await _configurationService.GetLowStockThresholdAsync();
            }
            catch (Exception ex)
            {
                // Return default threshold if configuration retrieval fails
                // Default: 500 grams - threshold for low stock alert
                return 500;
            }
        }
    }
}
