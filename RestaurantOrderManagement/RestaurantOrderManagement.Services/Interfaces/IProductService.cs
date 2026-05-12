using System.Collections.Generic;
using System.Threading.Tasks;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.DTOs;

namespace RestaurantOrderManagement.Services.Interfaces
{
    /// <summary>
    /// Service for product and category operations
    /// Returns DTOs without internal IDs for read operations
    /// Handles full entity models for CRUD operations in employee views
    /// </summary>
    public interface IProductService
    {
        // Read operations - return DTOs
        Task<IEnumerable<CategoryDTO>> GetCategoriesAsync();
        Task<IEnumerable<RestaurantMenuGroupDTO>> GetRestaurantMenuAsync(string? categoryName = null, string? keyword = null);
        Task<IEnumerable<RestaurantMenuGroupDTO>> SearchRestaurantMenuAsync(string? keyword = null, string? includeAllergens = null, string? excludeAllergens = null);
        Task<IEnumerable<ProductDTO>> GetProductsByCategoryAsync(string categoryName);
        Task<IEnumerable<ProductDTO>> SearchProductsAsync(string keyword, string? includeAllergens = null, string? excludeAllergens = null);

        // CRUD operations - return entities for employee management
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<IEnumerable<Product>> GetAllProductsAsync(bool includeDeleted = false);
        Task<IEnumerable<Menu>> GetAllMenusAsync(bool includeDeleted = false);
        Task<Product> GetProductByIdAsync(int productId);
        Task<Category> GetCategoryByIdAsync(int categoryId);

        // Create
        Task<int> CreateProductAsync(int categoryId, string name, string description, decimal price, int portionQuantity, int totalQuantity);
        Task<int> CreateCategoryAsync(string name, string description);

        // Update
        Task<bool> UpdateProductAsync(int productId, string name, string description, decimal price, int portionQuantity, bool isAvailable);
        Task<bool> UpdateProductImageAsync(int productId, string imageUrl);
        Task<bool> UpdateMenuImageAsync(int menuId, string imageUrl);
        Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, bool isActive);

        // Delete (soft delete)
        Task<bool> DeleteProductAsync(int productId);
        Task<bool> DeleteCategoryAsync(int categoryId);
    }
}
