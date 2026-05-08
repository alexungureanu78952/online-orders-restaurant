using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    /// <summary>
    /// MVVM ViewModel for product and category management
    /// Allows employees to create, update, and delete products and categories
    /// Supports both product and category CRUD operations
    /// </summary>
    public partial class ProductManagementViewModel : ObservableObject
    {
        private readonly IProductService _productService;

        // Products
        [ObservableProperty]
        private ObservableCollection<Product> allProducts = new();

        [ObservableProperty]
        private Product selectedProduct;

        [ObservableProperty]
        private string productName = string.Empty;

        [ObservableProperty]
        private string productDescription = string.Empty;

        [ObservableProperty]
        private decimal productPrice = 0;

        [ObservableProperty]
        private int productPortionQuantity = 500;

        [ObservableProperty]
        private int productTotalQuantity = 0;

        [ObservableProperty]
        private bool productIsAvailable = true;

        // Categories
        [ObservableProperty]
        private ObservableCollection<Category> allCategories = new();

        [ObservableProperty]
        private Category selectedCategory;

        [ObservableProperty]
        private string categoryName = string.Empty;

        [ObservableProperty]
        private string categoryDescription = string.Empty;

        [ObservableProperty]
        private bool categoryIsActive = true;

        // UI State
        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private bool showProductForm = false;

        [ObservableProperty]
        private bool showCategoryForm = false;

        [ObservableProperty]
        private bool isEditMode = false;

        public ProductManagementViewModel(IProductService productService)
        {
            _productService = productService;
        }

        #region Product CRUD Operations

        /// <summary>
        /// Load all products and categories
        /// </summary>
        [RelayCommand]
        public async Task LoadProductsAndCategoriesAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var products = await _productService.GetAllProductsAsync(includeDeleted: false);
                AllProducts.Clear();
                foreach (var product in products)
                {
                    AllProducts.Add(product);
                }

                var categories = await _productService.GetAllCategoriesAsync();
                AllCategories.Clear();
                foreach (var category in categories)
                {
                    AllCategories.Add(category);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Show product creation form
        /// </summary>
        [RelayCommand]
        public void ShowProductCreateForm()
        {
            IsEditMode = false;
            ClearProductForm();
            ShowProductForm = true;
        }

        /// <summary>
        /// Edit selected product
        /// </summary>
        [RelayCommand]
        public void EditSelectedProduct()
        {
            if (SelectedProduct == null)
            {
                ErrorMessage = "Please select a product to edit";
                return;
            }

            IsEditMode = true;
            ProductName = SelectedProduct.Name;
            ProductDescription = SelectedProduct.Description;
            ProductPrice = SelectedProduct.Price;
            ProductPortionQuantity = SelectedProduct.PortionQuantity;
            ProductTotalQuantity = SelectedProduct.TotalQuantity;
            ProductIsAvailable = SelectedProduct.IsAvailable;
            SelectedCategory = SelectedProduct.Category;
            ShowProductForm = true;
        }

        /// <summary>
        /// Save product (create or update)
        /// </summary>
        [RelayCommand]
        public async Task SaveProductAsync()
        {
            try
            {
                if (!ValidateProductForm())
                    return;

                IsLoading = true;
                ClearMessages();

                if (IsEditMode)
                {
                    // Update existing product
                    bool success = await _productService.UpdateProductAsync(
                        SelectedProduct.ProductId,
                        ProductName.Trim(),
                        ProductDescription?.Trim() ?? string.Empty,
                        ProductPrice,
                        ProductPortionQuantity,
                        ProductIsAvailable);

                    if (success)
                    {
                        SuccessMessage = $"Product '{ProductName}' updated successfully";
                        await LoadProductsAndCategoriesAsync();
                        ShowProductForm = false;
                    }
                }
                else
                {
                    // Create new product
                    if (SelectedCategory == null)
                    {
                        ErrorMessage = "Please select a category";
                        return;
                    }

                    int productId = await _productService.CreateProductAsync(
                        SelectedCategory.CategoryId,
                        ProductName.Trim(),
                        ProductDescription?.Trim() ?? string.Empty,
                        ProductPrice,
                        ProductPortionQuantity,
                        ProductTotalQuantity);

                    if (productId > 0)
                    {
                        SuccessMessage = $"Product '{ProductName}' created successfully (ID: {productId})";
                        await LoadProductsAndCategoriesAsync();
                        ShowProductForm = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving product: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Delete selected product
        /// </summary>
        [RelayCommand]
        public async Task DeleteSelectedProductAsync()
        {
            if (SelectedProduct == null)
            {
                ErrorMessage = "Please select a product to delete";
                return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                bool success = await _productService.DeleteProductAsync(SelectedProduct.ProductId);
                if (success)
                {
                    SuccessMessage = $"Product '{SelectedProduct.Name}' deleted successfully";
                    await LoadProductsAndCategoriesAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete product: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Category CRUD Operations

        /// <summary>
        /// Show category creation form
        /// </summary>
        [RelayCommand]
        public void ShowCategoryCreateForm()
        {
            IsEditMode = false;
            ClearCategoryForm();
            ShowCategoryForm = true;
        }

        /// <summary>
        /// Edit selected category
        /// </summary>
        [RelayCommand]
        public void EditSelectedCategory()
        {
            if (SelectedCategory == null)
            {
                ErrorMessage = "Please select a category to edit";
                return;
            }

            IsEditMode = true;
            CategoryName = SelectedCategory.Name;
            CategoryDescription = SelectedCategory.Description;
            CategoryIsActive = SelectedCategory.IsActive;
            ShowCategoryForm = true;
        }

        /// <summary>
        /// Save category (create or update)
        /// </summary>
        [RelayCommand]
        public async Task SaveCategoryAsync()
        {
            try
            {
                if (!ValidateCategoryForm())
                    return;

                IsLoading = true;
                ClearMessages();

                if (IsEditMode)
                {
                    // Update existing category
                    bool success = await _productService.UpdateCategoryAsync(
                        SelectedCategory.CategoryId,
                        CategoryName.Trim(),
                        CategoryDescription?.Trim() ?? string.Empty,
                        CategoryIsActive);

                    if (success)
                    {
                        SuccessMessage = $"Category '{CategoryName}' updated successfully";
                        await LoadProductsAndCategoriesAsync();
                        ShowCategoryForm = false;
                    }
                }
                else
                {
                    // Create new category
                    int categoryId = await _productService.CreateCategoryAsync(
                        CategoryName.Trim(),
                        CategoryDescription?.Trim() ?? string.Empty);

                    if (categoryId > 0)
                    {
                        SuccessMessage = $"Category '{CategoryName}' created successfully (ID: {categoryId})";
                        await LoadProductsAndCategoriesAsync();
                        ShowCategoryForm = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving category: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Delete selected category
        /// </summary>
        [RelayCommand]
        public async Task DeleteSelectedCategoryAsync()
        {
            if (SelectedCategory == null)
            {
                ErrorMessage = "Please select a category to delete";
                return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                bool success = await _productService.DeleteCategoryAsync(SelectedCategory.CategoryId);
                if (success)
                {
                    SuccessMessage = $"Category '{SelectedCategory.Name}' deleted successfully";
                    await LoadProductsAndCategoriesAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to delete category: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Validation & Helpers

        /// <summary>
        /// Validate product form inputs
        /// </summary>
        private bool ValidateProductForm()
        {
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                ErrorMessage = "Product name is required";
                return false;
            }

            if (ProductPrice < 0)
            {
                ErrorMessage = "Price cannot be negative";
                return false;
            }

            if (ProductPortionQuantity <= 0)
            {
                ErrorMessage = "Portion quantity must be greater than 0 grams";
                return false;
            }

            if (ProductTotalQuantity < 0)
            {
                ErrorMessage = "Total quantity cannot be negative";
                return false;
            }

            if (!IsEditMode && SelectedCategory == null)
            {
                ErrorMessage = "Please select a category";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate category form inputs
        /// </summary>
        private bool ValidateCategoryForm()
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                ErrorMessage = "Category name is required";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clear product form
        /// </summary>
        private void ClearProductForm()
        {
            ProductName = string.Empty;
            ProductDescription = string.Empty;
            ProductPrice = 0;
            ProductPortionQuantity = 500;
            ProductTotalQuantity = 0;
            ProductIsAvailable = true;
            SelectedCategory = null;
        }

        /// <summary>
        /// Clear category form
        /// </summary>
        private void ClearCategoryForm()
        {
            CategoryName = string.Empty;
            CategoryDescription = string.Empty;
            CategoryIsActive = true;
        }

        /// <summary>
        /// Clear all messages
        /// </summary>
        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        /// <summary>
        /// Get stock status display string
        /// </summary>
        public string GetStockStatusDisplay(int quantity)
        {
            if (quantity == 0)
                return "Out of Stock";
            if (quantity < 100)
                return "Critical";
            if (quantity < 500)
                return "Low";
            return "Adequate";
        }

        /// <summary>
        /// Get status display color (hex)
        /// </summary>
        public string GetStockStatusColor(int quantity)
        {
            if (quantity == 0)
                return "#E74C3C"; // Red
            if (quantity < 100)
                return "#C0392B"; // Dark red
            if (quantity < 500)
                return "#E67E22"; // Orange
            return "#27AE60"; // Green
        }

        #endregion
    }
}
