using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    public partial class ProductManagementViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        [ObservableProperty]
        private ObservableCollection<Product> allProducts = new();

        [ObservableProperty]
        private Product? selectedProduct;

        [ObservableProperty]
        private ObservableCollection<Menu> allMenus = new();

        [ObservableProperty]
        private Menu? selectedMenu;

        [ObservableProperty]
        private string productName = string.Empty;

        [ObservableProperty]
        private string productDescription = string.Empty;

        [ObservableProperty]
        private decimal productPrice = 0;

        [ObservableProperty]
        private int productPortionQuantity = 500;

        [ObservableProperty]
        private int productTotalQuantity = 5000;

        [ObservableProperty]
        private bool productIsAvailable = true;

        [ObservableProperty]
        private string productImageUrl = string.Empty;

        [ObservableProperty]
        private string menuImageUrl = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Category> allCategories = new();

        [ObservableProperty]
        private Category? selectedCategory;

        [ObservableProperty]
        private string categoryName = string.Empty;

        [ObservableProperty]
        private string categoryDescription = string.Empty;

        [ObservableProperty]
        private bool categoryIsActive = true;

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
        private bool showMenuImageForm = false;

        [ObservableProperty]
        private bool isEditMode = false;

        public ProductManagementViewModel(IProductService productService)
        {
            _productService = productService;
        }

        public event EventHandler? CatalogChanged;

        public string SelectedProductImageUrl => GetPrimaryProductImageUrl(SelectedProduct);
        public string SelectedMenuImageUrl => GetPrimaryMenuImageUrl(SelectedMenu);
        public bool HasSelectedProductImage => !string.IsNullOrWhiteSpace(SelectedProductImageUrl);
        public bool HasSelectedMenuImage => !string.IsNullOrWhiteSpace(SelectedMenuImageUrl);

        partial void OnSelectedProductChanged(Product? value)
        {
            if (value != null)
            {
                SelectedMenu = null;
                ShowMenuImageForm = false;
            }

            OnPropertyChanged(nameof(SelectedProductImageUrl));
            OnPropertyChanged(nameof(HasSelectedProductImage));
        }

        partial void OnSelectedMenuChanged(Menu? value)
        {
            if (value != null)
            {
                SelectedProduct = null;
                ShowProductForm = false;
                ShowCategoryForm = false;
            }

            OnPropertyChanged(nameof(SelectedMenuImageUrl));
            OnPropertyChanged(nameof(HasSelectedMenuImage));
        }

        #region Product CRUD Operations

        [RelayCommand]
        public async Task LoadProductsAndCategoriesAsync()
        {
            await LoadProductsAndCategoriesAsync(clearMessages: true);
        }

        private async Task LoadProductsAndCategoriesAsync(bool clearMessages)
        {
            try
            {
                IsLoading = true;
                if (clearMessages)
                    ClearMessages();

                var products = await _productService.GetAllProductsAsync(includeDeleted: false);
                AllProducts.Clear();
                foreach (var product in products)
                {
                    AllProducts.Add(product);
                }

                var menus = await _productService.GetAllMenusAsync(includeDeleted: false);
                AllMenus.Clear();
                foreach (var menu in menus)
                {
                    AllMenus.Add(menu);
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

        [RelayCommand]
        public void ShowProductCreateForm()
        {
            IsEditMode = false;
            ClearProductForm();
            ShowProductForm = true;
            ShowCategoryForm = false;
            ShowMenuImageForm = false;
        }

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
            ProductDescription = SelectedProduct.Description ?? string.Empty;
            ProductPrice = SelectedProduct.Price;
            ProductPortionQuantity = SelectedProduct.PortionQuantity;
            ProductTotalQuantity = SelectedProduct.TotalQuantity;
            ProductIsAvailable = SelectedProduct.IsAvailable;
            ProductImageUrl = GetPrimaryProductImageUrl(SelectedProduct);
            SelectedCategory = SelectedProduct.Category;
            ShowProductForm = true;
            ShowCategoryForm = false;
            ShowMenuImageForm = false;
        }

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
                    if (SelectedProduct == null)
                    {
                        ErrorMessage = "Please select a product to update";
                        return;
                    }

                    bool success = await _productService.UpdateProductAsync(
                        SelectedProduct.ProductId,
                        ProductName.Trim(),
                        ProductDescription?.Trim() ?? string.Empty,
                        ProductPrice,
                        ProductPortionQuantity,
                        ProductIsAvailable);

                    if (success)
                    {
                        success = await _productService.UpdateProductImageAsync(
                            SelectedProduct.ProductId,
                            ProductImageUrl?.Trim() ?? string.Empty);
                    }

                    if (success)
                    {
                        SuccessMessage = $"Product '{ProductName}' updated successfully";
                        await LoadProductsAndCategoriesAsync(clearMessages: false);
                        ShowProductForm = false;
                        NotifyCatalogChanged();
                    }
                }
                else
                {
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
                        if (!string.IsNullOrWhiteSpace(ProductImageUrl))
                        {
                            await _productService.UpdateProductImageAsync(productId, ProductImageUrl.Trim());
                        }

                        SuccessMessage = $"Product '{ProductName}' created successfully (ID: {productId})";
                        await LoadProductsAndCategoriesAsync(clearMessages: false);
                        ShowProductForm = false;
                        NotifyCatalogChanged();
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
                    await LoadProductsAndCategoriesAsync(clearMessages: false);
                    NotifyCatalogChanged();
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

        [RelayCommand]
        public void ShowCategoryCreateForm()
        {
            IsEditMode = false;
            ClearCategoryForm();
            ShowCategoryForm = true;
            ShowProductForm = false;
            ShowMenuImageForm = false;
        }

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
            CategoryDescription = SelectedCategory.Description ?? string.Empty;
            CategoryIsActive = SelectedCategory.IsActive;
            ShowCategoryForm = true;
            ShowProductForm = false;
            ShowMenuImageForm = false;
        }

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
                    if (SelectedCategory == null)
                    {
                        ErrorMessage = "Please select a category to update";
                        return;
                    }

                    bool success = await _productService.UpdateCategoryAsync(
                        SelectedCategory.CategoryId,
                        CategoryName.Trim(),
                        CategoryDescription?.Trim() ?? string.Empty,
                        CategoryIsActive);

                    if (success)
                    {
                        SuccessMessage = $"Category '{CategoryName}' updated successfully";
                        await LoadProductsAndCategoriesAsync(clearMessages: false);
                        ShowCategoryForm = false;
                        NotifyCatalogChanged();
                    }
                }
                else
                {
                    int categoryId = await _productService.CreateCategoryAsync(
                        CategoryName.Trim(),
                        CategoryDescription?.Trim() ?? string.Empty);

                    if (categoryId > 0)
                    {
                        SuccessMessage = $"Category '{CategoryName}' created successfully (ID: {categoryId})";
                        await LoadProductsAndCategoriesAsync(clearMessages: false);
                        ShowCategoryForm = false;
                        NotifyCatalogChanged();
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
                    await LoadProductsAndCategoriesAsync(clearMessages: false);
                    NotifyCatalogChanged();
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

        #region Menu Image Operations

        [RelayCommand]
        public void EditSelectedMenuImage()
        {
            if (SelectedMenu == null)
            {
                ErrorMessage = "Please select a menu to edit its image";
                return;
            }

            ClearMessages();
            MenuImageUrl = GetPrimaryMenuImageUrl(SelectedMenu);
            ShowMenuImageForm = true;
            ShowProductForm = false;
            ShowCategoryForm = false;
        }

        [RelayCommand]
        public async Task SaveMenuImageAsync()
        {
            if (SelectedMenu == null)
            {
                ErrorMessage = "Please select a menu";
                return;
            }

            if (!ValidateOptionalImageUrl("Menu image URL", MenuImageUrl))
                return;

            try
            {
                IsLoading = true;
                ClearMessages();

                await _productService.UpdateMenuImageAsync(SelectedMenu.MenuId, MenuImageUrl?.Trim() ?? string.Empty);
                SuccessMessage = $"Menu image for '{SelectedMenu.Name}' updated successfully";
                ShowMenuImageForm = false;
                await LoadProductsAndCategoriesAsync(clearMessages: false);
                NotifyCatalogChanged();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving menu image: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Validation & Helpers

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

            return ValidateOptionalImageUrl("Product image URL", ProductImageUrl);
        }

        private bool ValidateCategoryForm()
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                ErrorMessage = "Category name is required";
                return false;
            }

            return true;
        }

        private void ClearProductForm()
        {
            ProductName = string.Empty;
            ProductDescription = string.Empty;
            ProductPrice = 0;
            ProductPortionQuantity = 500;
            ProductTotalQuantity = 5000;
            ProductIsAvailable = true;
            ProductImageUrl = string.Empty;
            SelectedCategory = null;
        }

        private void ClearCategoryForm()
        {
            CategoryName = string.Empty;
            CategoryDescription = string.Empty;
            CategoryIsActive = true;
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        private void NotifyCatalogChanged()
        {
            CatalogChanged?.Invoke(this, EventArgs.Empty);
        }

        private static string GetPrimaryProductImageUrl(Product? product)
        {
            return product?.ProductImages?
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.ProductImageId)
                .FirstOrDefault()
                ?.ImageUrl ?? string.Empty;
        }

        private static string GetPrimaryMenuImageUrl(Menu? menu)
        {
            return menu?.MenuImages?
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.MenuImageId)
                .FirstOrDefault()
                ?.ImageUrl ?? string.Empty;
        }

        private bool ValidateOptionalImageUrl(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                ErrorMessage = $"{label} must be a valid http or https URL";
                return false;
            }

            return true;
        }

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

        public string GetStockStatusColor(int quantity)
        {
            if (quantity == 0)
                return "#E74C3C";
            if (quantity < 100)
                return "#C0392B";
            if (quantity < 500)
                return "#E67E22";
            return "#27AE60";
        }

        #endregion
    }
}
