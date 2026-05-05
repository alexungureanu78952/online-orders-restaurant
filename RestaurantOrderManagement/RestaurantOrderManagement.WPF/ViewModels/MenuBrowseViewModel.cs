using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Services;
using RestaurantOrderManagement.Services.DTOs;
using System.Collections.ObjectModel;

namespace RestaurantOrderManagement.ViewModels
{
    /// <summary>
    /// ViewModel for menu browsing view
    /// Manages categories and products, exposing only DTOs (no internal IDs)
    /// </summary>
    public partial class MenuBrowseViewModel : BaseViewModel
    {
        private readonly IProductService _productService;

        [ObservableProperty]
        private ObservableCollection<CategoryDTO> categories = new();

        [ObservableProperty]
        private ObservableCollection<ProductDTO> productsByCategory = new();

        [ObservableProperty]
        private CategoryDTO selectedCategory;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage;

        public MenuBrowseViewModel(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Load all categories
        /// </summary>
        [RelayCommand]
        public async Task LoadCategoriesAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var categories = await _productService.GetCategoriesAsync();
                Categories = new ObservableCollection<CategoryDTO>(categories);

                // Auto-select first category
                if (Categories.Count > 0)
                {
                    SelectedCategory = Categories[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load categories: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load products for selected category
        /// </summary>
        [RelayCommand]
        public async Task LoadProductsByCategoryAsync()
        {
            if (SelectedCategory == null)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var products = await _productService.GetProductsByCategoryAsync(SelectedCategory.Name);
                ProductsByCategory = new ObservableCollection<ProductDTO>(products);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load products: {ex.Message}";
                ProductsByCategory.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Refresh menu data
        /// </summary>
        [RelayCommand]
        public async Task RefreshAsync()
        {
            ProductsByCategory.Clear();
            await LoadCategoriesAsync();
            if (SelectedCategory != null)
            {
                await LoadProductsByCategoryAsync();
            }
        }

        /// <summary>
        /// Search products by keyword
        /// </summary>
        [RelayCommand]
        public async Task SearchProductsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                await LoadProductsByCategoryAsync();
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var products = await _productService.SearchProductsAsync(keyword);
                ProductsByCategory = new ObservableCollection<ProductDTO>(products);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Search failed: {ex.Message}";
                ProductsByCategory.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Validate that no ID fields are exposed in DTOs
        /// </summary>
        public void ValidateNoIdsExposed()
        {
            // Verify Categories contain no ID fields
            foreach (var cat in Categories)
            {
                // CategoryDTO should not have CategoryId property
                var idProperty = cat.GetType().GetProperty("CategoryId");
                if (idProperty != null)
                    throw new InvalidOperationException("CategoryDTO should not expose CategoryId");
            }

            // Verify Products contain no ProductId field
            foreach (var prod in ProductsByCategory)
            {
                // ProductDTO should only have DisplayCode, not ProductId
                var idProperty = prod.GetType().GetProperty("ProductId");
                if (idProperty != null)
                    throw new InvalidOperationException("ProductDTO should not expose ProductId");
            }
        }
    }
}
