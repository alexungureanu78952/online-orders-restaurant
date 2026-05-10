using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.DTOs;
using RestaurantOrderManagement.Services.Interfaces;

namespace RestaurantOrderManagement.Services.Implementations
{
    /// <summary>
    /// Service for product and category operations
    /// Returns DTOs without exposing internal IDs (public APIs)
    /// Exposes entities for employee CRUD operations
    /// Uses repositories with parameterized stored procedures
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly ProductRepository _productRepository;

        public ProductService(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        #region Read Operations - Public APIs

        /// <summary>
        /// Get all active categories
        /// </summary>
        public async Task<IEnumerable<CategoryDTO>> GetCategoriesAsync()
        {
            var categories = await _productRepository.GetCategoriesAsync();

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

        #endregion

        #region CRUD Operations - Employee Management

        /// <summary>
        /// Get all categories including inactive ones
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _productRepository.GetAllCategoriesAsync();
                return categories.OrderBy(c => c.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve categories", ex);
            }
        }

        /// <summary>
        /// Get all products including deleted ones (for management view)
        /// </summary>
        public async Task<IEnumerable<Product>> GetAllProductsAsync(bool includeDeleted = false)
        {
            try
            {
                var products = await _productRepository.GetAllProductsAsync();
                if (!includeDeleted)
                    products = products.Where(p => !p.IsDeleted).ToList();
                return products.OrderBy(p => p.Category.Name).ThenBy(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve products", ex);
            }
        }

        /// <summary>
        /// Get a product by ID
        /// </summary>
        public async Task<Product> GetProductByIdAsync(int productId)
        {
            if (productId <= 0)
                throw new ArgumentException("Product ID must be greater than 0", nameof(productId));

            try
            {
                var product = await _productRepository.GetProductByIdAsync(productId);
                if (product == null)
                    throw new KeyNotFoundException($"Product with ID {productId} not found");
                return product;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve product {productId}", ex);
            }
        }

        /// <summary>
        /// Get a category by ID
        /// </summary>
        public async Task<Category> GetCategoryByIdAsync(int categoryId)
        {
            if (categoryId <= 0)
                throw new ArgumentException("Category ID must be greater than 0", nameof(categoryId));

            try
            {
                var category = await _productRepository.GetCategoryByIdAsync(categoryId);
                if (category == null)
                    throw new KeyNotFoundException($"Category with ID {categoryId} not found");
                return category;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve category {categoryId}", ex);
            }
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        public async Task<int> CreateProductAsync(int categoryId, string name, string description, decimal price, int portionQuantity, int totalQuantity)
        {
            try
            {
                // Validate inputs
                if (categoryId <= 0)
                    throw new ArgumentException("Category ID must be greater than 0", nameof(categoryId));
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Product name is required", nameof(name));
                if (price < 0)
                    throw new ArgumentException("Price cannot be negative", nameof(price));
                if (portionQuantity <= 0)
                    throw new ArgumentException("Portion quantity must be greater than 0", nameof(portionQuantity));
                if (totalQuantity < 0)
                    throw new ArgumentException("Total quantity cannot be negative", nameof(totalQuantity));

                // Verify category exists
                var category = await GetCategoryByIdAsync(categoryId);

                // Create product
                int productId = await _productRepository.CreateProductAsync(categoryId, name.Trim(), description?.Trim(), price, portionQuantity, totalQuantity);
                return productId;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create product", ex);
            }
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        public async Task<int> CreateCategoryAsync(string name, string description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Category name is required", nameof(name));

                var result = await _productRepository.CreateCategoryAsync(name.Trim(), description?.Trim());
                return result;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create category", ex);
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        public async Task<bool> UpdateProductAsync(int productId, string name, string description, decimal price, int portionQuantity, bool isAvailable)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Product ID must be greater than 0", nameof(productId));
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Product name is required", nameof(name));
                if (price < 0)
                    throw new ArgumentException("Price cannot be negative", nameof(price));
                if (portionQuantity <= 0)
                    throw new ArgumentException("Portion quantity must be greater than 0", nameof(portionQuantity));

                // Verify product exists
                var product = await GetProductByIdAsync(productId);

                bool success = await _productRepository.UpdateProductAsync(productId, name.Trim(), description?.Trim(), price, portionQuantity, isAvailable);
                if (!success)
                    throw new InvalidOperationException($"Failed to update product {productId}");
                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to update product", ex);
            }
        }

        /// <summary>
        /// Update an existing category
        /// </summary>
        public async Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, bool isActive)
        {
            try
            {
                if (categoryId <= 0)
                    throw new ArgumentException("Category ID must be greater than 0", nameof(categoryId));
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Category name is required", nameof(name));

                // Verify category exists
                var category = await GetCategoryByIdAsync(categoryId);

                var result = await _productRepository.UpdateCategoryAsync(categoryId, name.Trim(), description?.Trim(), isActive);
                if (!result)
                    throw new InvalidOperationException($"Failed to update category {categoryId}");
                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to update category", ex);
            }
        }

        /// <summary>
        /// Soft delete a product
        /// </summary>
        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Product ID must be greater than 0", nameof(productId));

                // Verify product exists
                var product = await GetProductByIdAsync(productId);

                bool success = await _productRepository.DeleteProductAsync(productId);
                if (!success)
                    throw new InvalidOperationException($"Failed to delete product {productId}");
                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete product", ex);
            }
        }

        /// <summary>
        /// Soft delete a category
        /// </summary>
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    throw new ArgumentException("Category ID must be greater than 0", nameof(categoryId));

                // Verify category exists
                var category = await GetCategoryByIdAsync(categoryId);

                var result = await _productRepository.DeleteCategoryAsync(categoryId);
                if (!result)
                    throw new InvalidOperationException($"Failed to delete category {categoryId}");
                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to delete category", ex);
            }
        }

        #endregion

        /// <summary>
        /// Map Product entity to ProductDTO, removing internal IDs
        /// </summary>
        private ProductDTO MapProductToDTO(Product product)
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


