using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Context;
using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Data.Repositories
{
    public class ProductRepository : GenericRepository<Product>
    {
        public ProductRepository(RestaurantDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get products by category using parameterized stored procedure sp_GetProductsByCategory
        /// </summary>
        public virtual async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .FromSqlRaw("EXEC dbo.sp_GetProductsByCategory @CategoryId = {0}", categoryId)
                .ToListAsync();
        }

        /// <summary>
        /// Get product by ID using parameterized stored procedure sp_GetProductById
        /// </summary>
        public virtual async Task<Product> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .FromSqlRaw("EXEC dbo.sp_GetProductById @ProductId = {0}", productId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Search products by keyword and allergen filters using parameterized sp_SearchProducts
        /// </summary>
        public virtual async Task<IEnumerable<Product>> SearchProductsAsync(string keyword, string includeAllergens, string excludeAllergens)
        {
            return await _context.Products
                .FromSqlRaw("EXEC dbo.sp_SearchProducts @Keyword = {0}, @IncludeAllergens = {1}, @ExcludeAllergens = {2}",
                    keyword, includeAllergens, excludeAllergens)
                .ToListAsync();
        }

        /// <summary>
        /// Create new product using parameterized stored procedure sp_CreateProduct
        /// </summary>
        public virtual async Task<int> CreateProductAsync(int categoryId, string name, string description, decimal price, int portionQuantity, int totalQuantity)
        {
            var result = await _context.Database.ExecuteScalarAsync(
                "EXEC dbo.sp_CreateProduct @CategoryId = {0}, @Name = {1}, @Description = {2}, @Price = {3}, @PortionQuantity = {4}, @TotalQuantity = {5}",
                categoryId, name, description, price, portionQuantity, totalQuantity);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Update product using parameterized stored procedure sp_UpdateProduct
        /// </summary>
        public virtual async Task<bool> UpdateProductAsync(int productId, string name, string description, decimal price, int portionQuantity, bool isAvailable)
        {
            var result = await _context.Database.ExecuteAsync(
                "EXEC dbo.sp_UpdateProduct @ProductId = {0}, @Name = {1}, @Description = {2}, @Price = {3}, @PortionQuantity = {4}, @IsAvailable = {5}",
                productId, name, description, price, portionQuantity, isAvailable);
            return result > 0;
        }

        /// <summary>
        /// Delete product using parameterized stored procedure sp_DeleteProduct
        /// </summary>
        public virtual async Task<bool> DeleteProductAsync(int productId)
        {
            var result = await _context.Database.ExecuteAsync(
                "EXEC dbo.sp_DeleteProduct @ProductId = {0}",
                productId);
            return result > 0;
        }

        /// <summary>
        /// Get low stock products using parameterized stored procedure sp_GetLowStockProducts
        /// </summary>
        public virtual async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _context.Products
                .FromSqlRaw("EXEC dbo.sp_GetLowStockProducts")
                .ToListAsync();
        }

        /// <summary>
        /// Update inventory using parameterized stored procedure sp_UpdateInventory
        /// </summary>
        public virtual async Task<bool> UpdateInventoryAsync(int productId, int quantityChange)
        {
            var result = await _context.Database.ExecuteAsync(
                "EXEC dbo.sp_UpdateInventory @ProductId = {0}, @QuantityChange = {1}",
                productId, quantityChange);
            return result > 0;
        }

        /// <summary>
        /// Get all products from the context (including deleted)
        /// </summary>
        public virtual async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products.Include(p => p.Category).ToListAsync();
        }

        /// <summary>
        /// Get category by ID using parameterized stored procedure sp_GetCategoryById
        /// </summary>
        public virtual async Task<Category> GetCategoryByIdAsync(int categoryId)
        {
            return await _context.Categories
                .FromSqlRaw("EXEC dbo.sp_GetCategoryById @CategoryId = {0}", categoryId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Create new category using parameterized stored procedure sp_CreateCategory
        /// </summary>
        public virtual async Task<int> CreateCategoryAsync(string name, string description)
        {
            var result = await _context.Database.ExecuteScalarAsync(
                "EXEC dbo.sp_CreateCategory @Name = {0}, @Description = {1}",
                name, description);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Update category using parameterized stored procedure sp_UpdateCategory
        /// </summary>
        public virtual async Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, bool isActive)
        {
            var result = await _context.Database.ExecuteAsync(
                "EXEC dbo.sp_UpdateCategory @CategoryId = {0}, @Name = {1}, @Description = {2}, @IsActive = {3}",
                categoryId, name, description, isActive);
            return result > 0;
        }

        /// <summary>
        /// Delete (soft delete) category using parameterized stored procedure sp_DeleteCategory
        /// </summary>
        public virtual async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            var result = await _context.Database.ExecuteAsync(
                "EXEC dbo.sp_DeleteCategory @CategoryId = {0}",
                categoryId);
            return result > 0;
        }
    }
}
