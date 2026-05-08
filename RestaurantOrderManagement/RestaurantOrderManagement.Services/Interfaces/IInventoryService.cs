using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Services.Interfaces
{
    /// <summary>
    /// Service for managing product inventory, including low-stock detection and restock operations
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Get all products with inventory below configured low-stock threshold
        /// </summary>
        /// <returns>List of low-stock products with current quantities</returns>
        Task<List<Product>> GetLowStockProductsAsync();

        /// <summary>
        /// Update product inventory by adding or removing quantity
        /// </summary>
        /// <param name="productId">Product to update</param>
        /// <param name="quantityChange">Change in quantity (positive to add, negative to subtract)</param>
        /// <returns>True if update successful, false otherwise</returns>
        Task<bool> UpdateProductInventoryAsync(int productId, int quantityChange);

        /// <summary>
        /// Restock product by specified quantity
        /// </summary>
        /// <param name="productId">Product to restock</param>
        /// <param name="restockQuantity">Quantity to add (must be positive)</param>
        /// <returns>Updated product if successful, null otherwise</returns>
        Task<Product> RestockProductAsync(int productId, int restockQuantity);

        /// <summary>
        /// Get product details including current inventory
        /// </summary>
        /// <param name="productId">Product ID to retrieve</param>
        /// <returns>Product with current inventory information</returns>
        Task<Product> GetProductWithInventoryAsync(int productId);

        /// <summary>
        /// Get low stock threshold from configuration
        /// </summary>
        /// <returns>Threshold quantity in grams</returns>
        Task<int> GetLowStockThresholdAsync();
    }
}
