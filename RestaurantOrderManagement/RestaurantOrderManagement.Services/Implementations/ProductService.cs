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
    
    public class ProductService : IProductService
    {
        private readonly ProductRepository _productRepository;
        private readonly ConfigurationRepository? _configurationRepository;

        public ProductService(ProductRepository productRepository)
            : this(productRepository, null)
        {
        }

        public ProductService(ProductRepository productRepository, ConfigurationRepository? configurationRepository)
        {
            _productRepository = productRepository;
            _configurationRepository = configurationRepository;
        }

        #region Read Operations - Public APIs

        public async Task<IEnumerable<CategoryDTO>> GetCategoriesAsync()
        {
            var categories = await _productRepository.GetCategoriesAsync();
            return categories.Select(c => new CategoryDTO
            {
                Name = c.Name,
                Description = c.Description ?? string.Empty,
                IsActive = c.IsActive
            }).ToList();
        }

        public async Task<IEnumerable<RestaurantMenuGroupDTO>> GetRestaurantMenuAsync(string? categoryName = null, string? keyword = null)
        {
            return await BuildRestaurantMenuAsync(categoryName, keyword, null, null);
        }
        public async Task<IEnumerable<RestaurantMenuGroupDTO>> SearchRestaurantMenuAsync(string? keyword = null, string? includeAllergens = null, string? excludeAllergens = null)
        {
            return await BuildRestaurantMenuAsync(null, keyword, includeAllergens, excludeAllergens);
        }

        private async Task<IEnumerable<RestaurantMenuGroupDTO>> BuildRestaurantMenuAsync(
            string? categoryName,
            string? keyword,
            string? includeAllergens,
            string? excludeAllergens)
        {
            var products = await _productRepository.GetRestaurantProductsAsync();
            var menus = await _productRepository.GetRestaurantMenusAsync();
            var configuration = _configurationRepository != null
                ? await _configurationRepository.GetConfigurationAsync()
                : new Configuration();

            var normalizedCategory = NormalizeFilter(categoryName);
            var normalizedKeyword = NormalizeFilter(keyword);
            var includeAllergenTokens = ParseSearchTokens(includeAllergens);
            var excludeAllergenTokens = ParseSearchTokens(excludeAllergens);

            var productItems = products
                .Where(p => MatchesCategory(p.Category?.Name, normalizedCategory))
                .Where(p => MatchesProductKeyword(p, normalizedKeyword))
                .Where(p => MatchesAllergenFilter(GetProductAllergenNames(p), includeAllergenTokens, excludeAllergenTokens))
                .Select(MapProductToRestaurantMenuItem);

            var menuItems = menus
                .Where(m => MatchesCategory(m.Category?.Name, normalizedCategory))
                .Where(m => MatchesMenuKeyword(m, normalizedKeyword))
                .Where(m => MatchesAllergenFilter(GetMenuAllergenNames(m), includeAllergenTokens, excludeAllergenTokens))
                .Select(menu => MapMenuToRestaurantMenuItem(menu, configuration.MenuBundleDiscountPercent));

            return productItems
                .Concat(menuItems)
                .OrderBy(item => item.CategoryName)
                .ThenByDescending(item => item.IsAvailable)
                .ThenBy(item => item.ItemType)
                .ThenBy(item => item.Name)
                .GroupBy(item => item.CategoryName)
                .Select(group => new RestaurantMenuGroupDTO
                {
                    CategoryName = group.Key,
                    Items = group.ToList()
                })
                .ToList();
        }

        public async Task<IEnumerable<ProductDTO>> GetProductsByCategoryAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return Array.Empty<ProductDTO>();

            var categories = await _productRepository.GetCategoriesAsync();
            var category = categories.FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return Array.Empty<ProductDTO>();

            var products = await _productRepository.GetProductsByCategoryAsync(category.CategoryId);

            return products
                .Where(p => p.IsAvailable && !p.IsDeleted)
                .Select(p => MapProductToDTO(p))
                .ToList();
        }

        public async Task<IEnumerable<ProductDTO>> SearchProductsAsync(string keyword, string? includeAllergens = null, string? excludeAllergens = null)
        {
            var products = await _productRepository.SearchProductsAsync(keyword, includeAllergens, excludeAllergens);

            return products
                .Where(p => p.IsAvailable && !p.IsDeleted)
                .Select(p => MapProductToDTO(p))
                .ToList();
        }

        #endregion

        #region CRUD Operations - Employee Management

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

        public async Task<IEnumerable<Menu>> GetAllMenusAsync(bool includeDeleted = false)
        {
            try
            {
                var menus = await _productRepository.GetAllMenusAsync();
                if (!includeDeleted)
                    menus = menus.Where(m => !m.IsDeleted).ToList();
                return menus.OrderBy(m => m.Category.Name).ThenBy(m => m.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve menus", ex);
            }
        }

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

        public async Task<int> CreateProductAsync(int categoryId, string name, string description, decimal price, int portionQuantity, int totalQuantity)
        {
            try
            {
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

                var category = await GetCategoryByIdAsync(categoryId);
                int productId = await _productRepository.CreateProductAsync(categoryId, name.Trim(), description?.Trim() ?? string.Empty, price, portionQuantity, totalQuantity);
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

        public async Task<int> CreateCategoryAsync(string name, string description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Category name is required", nameof(name));

                var result = await _productRepository.CreateCategoryAsync(name.Trim(), description?.Trim() ?? string.Empty);
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

                var product = await GetProductByIdAsync(productId);

                bool success = await _productRepository.UpdateProductAsync(productId, name.Trim(), description?.Trim() ?? string.Empty, price, portionQuantity, isAvailable);
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

        public async Task<bool> UpdateProductImageAsync(int productId, string imageUrl)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Product ID must be greater than 0", nameof(productId));

                _ = await GetProductByIdAsync(productId);

                return await _productRepository.UpdateProductImageAsync(productId, imageUrl);
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
                throw new InvalidOperationException("Failed to update product image", ex);
            }
        }

        public async Task<bool> UpdateMenuImageAsync(int menuId, string imageUrl)
        {
            try
            {
                if (menuId <= 0)
                    throw new ArgumentException("Menu ID must be greater than 0", nameof(menuId));

                var menu = await _productRepository.GetMenuByIdAsync(menuId);
                if (menu == null)
                    throw new KeyNotFoundException($"Menu with ID {menuId} not found");

                return await _productRepository.UpdateMenuImageAsync(menuId, imageUrl);
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
                throw new InvalidOperationException("Failed to update menu image", ex);
            }
        }

        public async Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, bool isActive)
        {
            try
            {
                if (categoryId <= 0)
                    throw new ArgumentException("Category ID must be greater than 0", nameof(categoryId));
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Category name is required", nameof(name));

                var category = await GetCategoryByIdAsync(categoryId);

                var result = await _productRepository.UpdateCategoryAsync(categoryId, name.Trim(), description?.Trim() ?? string.Empty, isActive);
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

        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                    throw new ArgumentException("Product ID must be greater than 0", nameof(productId));

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

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    throw new ArgumentException("Category ID must be greater than 0", nameof(categoryId));

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

        private ProductDTO MapProductToDTO(Product product)
        {
            return new ProductDTO
            {
                DisplayCode = product.DisplayCode ?? $"PROD-{product.ProductId:D5}",
                Name = product.Name,
                Description = product.Description ?? string.Empty,
                Price = product.Price,
                PortionQuantity = product.PortionQuantity,
                IsAvailable = product.IsAvailable,
                CategoryName = product.Category?.Name ?? "Unknown",
                Allergens = product.ProductAllergens
                    ?.Select(pa => new AllergenDTO
                    {
                        Name = pa.Allergen.Name,
                        Description = pa.Allergen.Description ?? string.Empty
                    })
                    .ToList() ?? new List<AllergenDTO>(),
                ImageUrls = product.ProductImages
                    ?.OrderBy(pi => pi.DisplayOrder)
                    .Select(pi => pi.ImageUrl)
                    .Where(IsDisplayImageUrl)
                    .Distinct()
                    .ToList() ?? new List<string>()
            };
        }

        private static RestaurantMenuItemDTO MapProductToRestaurantMenuItem(Product product)
        {
            var isAvailable = IsProductAvailable(product);

            return new RestaurantMenuItemDTO
            {
                OrderKey = $"P:{product.ProductId}",
                ItemType = "Preparat",
                Name = product.Name,
                Description = product.Description ?? string.Empty,
                CategoryName = product.Category?.Name ?? "Necategorizat",
                Price = product.Price,
                PortionDisplay = $"{product.PortionQuantity}g/portie",
                IsAvailable = isAvailable,
                Allergens = product.ProductAllergens
                    ?.Where(pa => pa.Allergen != null)
                    .Select(pa => new AllergenDTO
                    {
                        Name = pa.Allergen.Name,
                        Description = pa.Allergen.Description ?? string.Empty
                    })
                    .OrderBy(a => a.Name)
                    .ToList() ?? new List<AllergenDTO>(),
                ImageUrls = product.ProductImages
                    ?.OrderBy(pi => pi.DisplayOrder)
                    .Select(pi => pi.ImageUrl)
                    .Where(IsDisplayImageUrl)
                    .Distinct()
                    .Take(1)
                    .ToList() ?? new List<string>()
            };
        }

        private static RestaurantMenuItemDTO MapMenuToRestaurantMenuItem(Menu menu, decimal configuredDiscountPercent)
        {
            var components = menu.MenuProducts
                ?.Where(mp => mp.Product != null && !mp.Product.IsDeleted)
                .OrderBy(mp => mp.Product.Name)
                .Select(mp => new RestaurantMenuComponentDTO
                {
                    ProductName = mp.Product.Name,
                    Quantity = mp.Quantity,
                    PortionQuantity = mp.Product.PortionQuantity,
                    IsAvailable = IsProductAvailable(mp.Product)
                })
                .ToList() ?? new List<RestaurantMenuComponentDTO>();

            var subtotal = menu.MenuProducts
                ?.Where(mp => mp.Product != null && !mp.Product.IsDeleted)
                .Sum(mp => mp.Product.Price * mp.Quantity) ?? 0m;
            var discountPercent = menu.BundleDiscountPercent > 0
                ? menu.BundleDiscountPercent
                : configuredDiscountPercent;
            var price = Math.Round(subtotal * (1 - discountPercent / 100), 2);
            var componentProducts = menu.MenuProducts
                ?.Where(mp => mp.Product != null && !mp.Product.IsDeleted)
                .Select(mp => mp.Product)
                .ToList() ?? new List<Product>();

            return new RestaurantMenuItemDTO
            {
                OrderKey = $"M:{menu.MenuId}",
                ItemType = "Meniu",
                Name = menu.Name,
                Description = menu.Description ?? string.Empty,
                CategoryName = menu.Category?.Name ?? "Necategorizat",
                Price = price,
                PortionDisplay = string.Join(" / ", components.Select(c => c.Quantity > 1
                    ? $"{c.Quantity}x{c.PortionQuantity}g"
                    : $"{c.PortionQuantity}g")),
                IsAvailable = menu.IsAvailable && components.Count > 0 && components.All(c => c.IsAvailable),
                Components = components,
                Allergens = componentProducts
                    .SelectMany(p => p.ProductAllergens ?? new List<ProductAllergen>())
                    .Where(pa => pa.Allergen != null)
                    .GroupBy(pa => pa.Allergen.Name)
                    .Select(group => new AllergenDTO
                    {
                        Name = group.Key,
                        Description = group.First().Allergen.Description ?? string.Empty
                    })
                    .OrderBy(a => a.Name)
                    .ToList(),
                ImageUrls = GetMenuImageUrls(menu, componentProducts)
            };
        }

        private static List<string> GetMenuImageUrls(Menu menu, IEnumerable<Product> componentProducts)
        {
            var menuImages = menu.MenuImages
                ?.OrderBy(mi => mi.DisplayOrder)
                .Select(mi => mi.ImageUrl)
                .Where(IsDisplayImageUrl)
                .Distinct()
                .Take(1)
                .ToList() ?? new List<string>();

            if (menuImages.Count > 0)
                return menuImages;

            return componentProducts
                .SelectMany(p => p.ProductImages ?? new List<ProductImage>())
                .OrderBy(pi => pi.DisplayOrder)
                .Select(pi => pi.ImageUrl)
                .Where(IsDisplayImageUrl)
                .Distinct()
                .Take(1)
                .ToList();
        }

        private static bool IsDisplayImageUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url) &&
                   !url.Contains("loremflickr.com", StringComparison.OrdinalIgnoreCase) &&
                   !url.Contains("placehold.co", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProductAvailable(Product product)
        {
            return product.IsAvailable && !product.IsDeleted && product.TotalQuantity > 0;
        }

        private static bool MatchesCategory(string? categoryName, string? normalizedCategory)
        {
            return string.IsNullOrWhiteSpace(normalizedCategory) ||
                   string.Equals(categoryName, normalizedCategory, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesProductKeyword(Product product, string? keyword)
        {
            return string.IsNullOrWhiteSpace(keyword) ||
                   Contains(product.Name, keyword) ||
                   Contains(product.Description, keyword) ||
                   product.ProductAllergens.Any(pa => Contains(pa.Allergen?.Name, keyword));
        }

        private static bool MatchesMenuKeyword(Menu menu, string? keyword)
        {
            return string.IsNullOrWhiteSpace(keyword) ||
                   Contains(menu.Name, keyword) ||
                   Contains(menu.Description, keyword) ||
                   menu.MenuProducts.Any(mp =>
                       Contains(mp.Product?.Name, keyword) ||
                       mp.Product?.ProductAllergens.Any(pa => Contains(pa.Allergen?.Name, keyword)) == true);
        }

        private static bool Contains(string? value, string keyword)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyCollection<string> ParseSearchTokens(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToList();
        }

        private static bool MatchesAllergenFilter(
            IEnumerable<string?> allergenNames,
            IReadOnlyCollection<string> includeAllergens,
            IReadOnlyCollection<string> excludeAllergens)
        {
            var names = allergenNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToList();

            return includeAllergens.All(token => names.Any(name => Contains(name, token))) &&
                   !excludeAllergens.Any(token => names.Any(name => Contains(name, token)));
        }

        private static IEnumerable<string?> GetProductAllergenNames(Product product)
        {
            return product.ProductAllergens
                ?.Select(pa => pa.Allergen?.Name) ?? Enumerable.Empty<string?>();
        }

        private static IEnumerable<string?> GetMenuAllergenNames(Menu menu)
        {
            return menu.MenuProducts
                ?.Where(mp => mp.Product != null)
                .SelectMany(mp => GetProductAllergenNames(mp.Product)) ?? Enumerable.Empty<string?>();
        }

        private static string? NormalizeFilter(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = value.Trim();
            return string.Equals(normalized, "Toate categoriile", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalized, "All categories", StringComparison.OrdinalIgnoreCase)
                ? null
                : normalized;
        }
    }
}


