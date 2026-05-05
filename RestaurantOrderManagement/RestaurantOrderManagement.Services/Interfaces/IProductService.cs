using System.Collections.Generic;
using System.Threading.Tasks;
using RestaurantOrderManagement.Services.DTOs;

namespace RestaurantOrderManagement.Services.Interfaces
{
    /// <summary>
    /// Service for product and category operations
    /// Returns DTOs without internal IDs
    /// </summary>
    public interface IProductService
    {
        Task<IEnumerable<CategoryDTO>> GetCategoriesAsync();
        Task<IEnumerable<ProductDTO>> GetProductsByCategoryAsync(string categoryName);
        Task<IEnumerable<ProductDTO>> SearchProductsAsync(string keyword, string includeAllergens = null, string excludeAllergens = null);
    }
}
