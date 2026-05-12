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

        public virtual async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            var productIds = await _context.Products
                .FromSqlRaw("EXEC dbo.sp_GetProductsByCategory @CategoryId = {0}", categoryId)
                .AsNoTracking()
                .Select(p => p.ProductId)
                .ToListAsync();

            if (productIds.Count == 0)
                return Array.Empty<Product>();

            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductAllergens)
                    .ThenInclude(pa => pa.Allergen)
                .Include(p => p.ProductImages)
                .Where(p => productIds.Contains(p.ProductId))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        
        public virtual async Task<IEnumerable<Product>> GetRestaurantProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductAllergens)
                    .ThenInclude(pa => pa.Allergen)
                .Include(p => p.ProductImages)
                .Where(p => !p.IsDeleted && p.Category.IsActive)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<Menu>> GetRestaurantMenusAsync()
        {
            return await _context.Menus
                .AsNoTracking()
                .Include(m => m.Category)
                .Include(m => m.MenuImages)
                .Include(m => m.MenuProducts)
                    .ThenInclude(mp => mp.Product)
                        .ThenInclude(p => p.ProductAllergens)
                            .ThenInclude(pa => pa.Allergen)
                .Include(m => m.MenuProducts)
                    .ThenInclude(mp => mp.Product)
                        .ThenInclude(p => p.ProductImages)
                .Where(m => !m.IsDeleted && m.Category.IsActive)
                .ToListAsync();
        }

        
        public virtual async Task<Menu?> GetMenuByIdAsync(int menuId)
        {
            return await _context.Menus
                .AsNoTracking()
                .Include(m => m.Category)
                .Include(m => m.MenuProducts)
                    .ThenInclude(mp => mp.Product)
                        .ThenInclude(p => p.ProductAllergens)
                            .ThenInclude(pa => pa.Allergen)
                .Include(m => m.MenuProducts)
                    .ThenInclude(mp => mp.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.MenuId == menuId && !m.IsDeleted);
        }

        public virtual async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductAllergens)
                    .ThenInclude(pa => pa.Allergen)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId && !p.IsDeleted);
        }

        public virtual async Task<IEnumerable<Product>> SearchProductsAsync(string? keyword, string? includeAllergens, string? excludeAllergens)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductAllergens)
                    .ThenInclude(pa => pa.Allergen)
                .Include(p => p.ProductImages)
                .Where(p => !p.IsDeleted && p.IsAvailable);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) || (p.Description != null && p.Description.Contains(keyword)));
            }

            foreach (var token in ParseAllergenTokens(includeAllergens))
            {
                if (int.TryParse(token, out var allergenId))
                {
                    query = query.Where(p => p.ProductAllergens.Any(pa => pa.AllergenId == allergenId));
                }
                else
                {
                    var allergenName = token;
                    query = query.Where(p => p.ProductAllergens.Any(pa => pa.Allergen.Name.Contains(allergenName)));
                }
            }

            foreach (var token in ParseAllergenTokens(excludeAllergens))
            {
                if (int.TryParse(token, out var allergenId))
                {
                    query = query.Where(p => !p.ProductAllergens.Any(pa => pa.AllergenId == allergenId));
                }
                else
                {
                    var allergenName = token;
                    query = query.Where(p => !p.ProductAllergens.Any(pa => pa.Allergen.Name.Contains(allergenName)));
                }
            }

            return await query
                .OrderBy(p => p.Category.Name)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public virtual async Task<int> CreateProductAsync(int categoryId, string name, string description, decimal price, int portionQuantity, int totalQuantity)
        {
            var result = await ExecuteScalarStoredProcedureAsync(
                "dbo.sp_CreateProduct",
                ("@CategoryId", categoryId),
                ("@Name", name),
                ("@Description", description),
                ("@Price", price),
                ("@PortionQuantity", portionQuantity),
                ("@TotalQuantity", totalQuantity));
            return result != null ? Convert.ToInt32(result) : 0;
        }


        public virtual async Task<bool> UpdateProductAsync(int productId, string name, string description, decimal price, int portionQuantity, bool isAvailable)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_UpdateProduct @ProductId = {0}, @Name = {1}, @Description = {2}, @Price = {3}, @PortionQuantity = {4}, @IsAvailable = {5}",
                productId, name, description, price, portionQuantity, isAvailable);

            return await _context.Products.AnyAsync(p => p.ProductId == productId && !p.IsDeleted);
        }


        public virtual async Task<bool> UpdateProductImageAsync(int productId, string? imageUrl)
        {
            var images = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .OrderBy(pi => pi.DisplayOrder)
                .ThenBy(pi => pi.ProductImageId)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                _context.ProductImages.RemoveRange(images);
                await _context.SaveChangesAsync();
                return true;
            }

            var primaryImage = images.FirstOrDefault();
            if (primaryImage == null)
            {
                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = imageUrl.Trim(),
                    DisplayOrder = 1
                });
            }
            else
            {
                primaryImage.ImageUrl = imageUrl.Trim();
                primaryImage.DisplayOrder = 1;
            }

            if (images.Count > 1)
            {
                _context.ProductImages.RemoveRange(images.Skip(1));
            }

            await _context.SaveChangesAsync();
            return true;
        }

        
        public virtual async Task<bool> DeleteProductAsync(int productId)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteProduct @ProductId = {0}",
                productId);
            return await _context.Products.AnyAsync(p => p.ProductId == productId && p.IsDeleted);
        }

        
        public virtual async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            var threshold = await _context.Configurations
                .AsNoTracking()
                .Select(c => (int?)c.LowStockThreshold)
                .FirstOrDefaultAsync() ?? 500;

            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted && p.TotalQuantity < threshold)
                .OrderBy(p => p.TotalQuantity)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        
        public virtual async Task<bool> UpdateInventoryAsync(int productId, int quantityChange)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_UpdateInventory @ProductId = {0}, @QuantityChange = {1}",
                productId, quantityChange);
            return await _context.Products.AnyAsync(p => p.ProductId == productId && !p.IsDeleted);
        }

        public virtual async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .ToListAsync();
        }


        public virtual async Task<IEnumerable<Menu>> GetAllMenusAsync()
        {
            return await _context.Menus
                .AsNoTracking()
                .Include(m => m.Category)
                .Include(m => m.MenuImages)
                .Include(m => m.MenuProducts)
                    .ThenInclude(mp => mp.Product)
                .ToListAsync();
        }

        
        public virtual async Task<bool> UpdateMenuImageAsync(int menuId, string? imageUrl)
        {
            var images = await _context.MenuImages
                .Where(mi => mi.MenuId == menuId)
                .OrderBy(mi => mi.DisplayOrder)
                .ThenBy(mi => mi.MenuImageId)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                _context.MenuImages.RemoveRange(images);
                await _context.SaveChangesAsync();
                return true;
            }

            var primaryImage = images.FirstOrDefault();
            if (primaryImage == null)
            {
                _context.MenuImages.Add(new MenuImage
                {
                    MenuId = menuId,
                    ImageUrl = imageUrl.Trim(),
                    DisplayOrder = 1
                });
            }
            else
            {
                primaryImage.ImageUrl = imageUrl.Trim();
                primaryImage.DisplayOrder = 1;
            }

            if (images.Count > 1)
            {
                _context.MenuImages.RemoveRange(images.Skip(1));
            }

            await _context.SaveChangesAsync();
            return true;
        }

        
        public virtual async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _context.Categories
                .FromSqlRaw("EXEC dbo.sp_GetCategories")
                .AsNoTracking()
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .ToListAsync();
        }

        
        public virtual async Task<Category?> GetCategoryByIdAsync(int categoryId)
        {
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        
        public virtual async Task<int> CreateCategoryAsync(string name, string description)
        {
            var result = await ExecuteScalarStoredProcedureAsync(
                "dbo.sp_CreateCategory",
                ("@Name", name),
                ("@Description", description));
            return result != null ? Convert.ToInt32(result) : 0;
        }

        
        public virtual async Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, bool isActive)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_UpdateCategory @CategoryId = {0}, @Name = {1}, @Description = {2}, @IsActive = {3}",
                categoryId, name, description, isActive);
            return await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

        
        public virtual async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_DeleteCategory @CategoryId = {0}",
                categoryId);
            return await _context.Categories.AnyAsync(c => c.CategoryId == categoryId && !c.IsActive);
        }

        private static IReadOnlyCollection<string> ParseAllergenTokens(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();
        }
    }
}
