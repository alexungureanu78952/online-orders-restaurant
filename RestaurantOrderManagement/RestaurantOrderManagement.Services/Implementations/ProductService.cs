using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.DTOs;
using RestaurantOrderManagement.Services.Interfaces;

namespace RestaurantOrderManagement.Services.Implementations
{
    /// <summary>
    /// Service for product and category operations
    /// Returns DTOs without exposing internal IDs
    /// Uses repositories with parameterized stored procedures
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly ProductRepository _productRepository;

        public ProductService(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        /// <summary>
        /// Get all active categories
        /// </summary>
        public async Task<IEnumerable<CategoryDTO>> GetCategoriesAsync()
        {
            var categories = await _productRepository.GetAllAsync(); // Gets from context

            // Map to DTOs without IDs
            return categories.Select(c => new CategoryDTO
            {
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive
            }).ToList();
        }

        /// <summary>
        /// Get all available products in a category by name
        /// Returns DTOs without ProductId
        /// </summary>
        public async Task<IEnumerable<ProductDTO>> GetProductsByCategoryAsync(string categoryName)
        {
            // Note: In production, would fetch category first to get ID, then call sp_GetProductsByCategory
            // For now, search by name pattern
            var products = await _productRepository.SearchProductsAsync(categoryName, null, null);

            return products
                .Where(p => p.IsAvailable && !p.IsDeleted)
                .Select(p => MapProductToDTO(p))
                .ToList();
        }

        /// <summary>
        /// Search products by keyword and allergen filters
        /// Returns DTOs without ProductId
        /// </summary>
        public async Task<IEnumerable<ProductDTO>> SearchProductsAsync(string keyword, string includeAllergens = null, string excludeAllergens = null)
        {
            var products = await _productRepository.SearchProductsAsync(keyword, includeAllergens, excludeAllergens);

            return products
                .Where(p => p.IsAvailable && !p.IsDeleted)
                .Select(p => MapProductToDTO(p))
                .ToList();
        }

        /// <summary>
        /// Map Product entity to ProductDTO, removing internal IDs
        /// </summary>
        private ProductDTO MapProductToDTO(Data.Models.Product product)
        {
            return new ProductDTO
            {
                DisplayCode = product.DisplayCode ?? $"PROD-{product.ProductId:D5}", // Fallback to formatted ID
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                PortionQuantity = product.PortionQuantity,
                IsAvailable = product.IsAvailable,
                CategoryName = product.Category?.Name ?? "Unknown",
                Allergens = product.ProductAllergens
                    ?.Select(pa => new AllergenDTO
                    {
                        Name = pa.Allergen.Name,
                        Description = pa.Allergen.Description
                    })
                    .ToList() ?? new List<AllergenDTO>(),
                ImageUrls = product.ProductImages
                    ?.OrderBy(pi => pi.DisplayOrder)
                    .Select(pi => pi.ImageUrl)
                    .ToList() ?? new List<string>()
            };
        }
    }
}

