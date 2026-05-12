using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;

        [ObservableProperty]
        private ObservableCollection<Product> lowStockProducts = new();

        [ObservableProperty]
        private Product? selectedProduct;

        [ObservableProperty]
        private int restockQuantity = 500;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private int lowStockThreshold = 500;

        public InventoryViewModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public int ProjectedRestockQuantity => (SelectedProduct?.TotalQuantity ?? 0) + RestockQuantity;

        partial void OnSelectedProductChanged(Product? value)
        {
            OnPropertyChanged(nameof(ProjectedRestockQuantity));
        }

        partial void OnRestockQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(ProjectedRestockQuantity));
        }

        [RelayCommand]
        public async Task LoadLowStockProductsAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                LowStockThreshold = await _inventoryService.GetLowStockThresholdAsync();

                var products = await _inventoryService.GetLowStockProductsAsync();
                LowStockProducts.Clear();
                foreach (var product in products.OrderBy(p => p.TotalQuantity))
                {
                    LowStockProducts.Add(product);
                }

                if (LowStockProducts.Count == 0)
                {
                    SuccessMessage = "All products are adequately stocked";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load low-stock products: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task RestockProductAsync()
        {
            if (SelectedProduct == null)
            {
                ErrorMessage = "Please select a product to restock";
                return;
            }

            if (RestockQuantity <= 0)
            {
                ErrorMessage = "Restock quantity must be greater than 0";
                return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                var restocked = await _inventoryService.RestockProductAsync(SelectedProduct.ProductId, RestockQuantity);
                if (restocked != null)
                {
                    SuccessMessage = $"{restocked.Name} restocked by {RestockQuantity}g. New quantity: {restocked.TotalQuantity}g";

                    await LoadLowStockProductsAsync();
                    SelectedProduct = null;
                    RestockQuantity = 500;
                }
                else
                {
                    ErrorMessage = "Failed to restock product";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error restocking product: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void SetRestockQuantity(string quantity)
        {
            if (int.TryParse(quantity, out var parsedQuantity) && parsedQuantity > 0)
            {
                RestockQuantity = parsedQuantity;
            }
        }

        public string GetStockStatusDisplay(int currentQuantity, int threshold)
        {
            if (currentQuantity == 0)
                return "Out of Stock";
            else if (currentQuantity < threshold / 2)
                return "Critical";
            else if (currentQuantity < threshold)
                return "Low";
            else
                return "Adequate";
        }

        public string GetStockStatusColor(int currentQuantity, int threshold)
        {
            if (currentQuantity == 0)
                return "#E74C3C";
            else if (currentQuantity < threshold / 2)
                return "#C0392B";
            else if (currentQuantity < threshold)
                return "#E67E22";
            else
                return "#27AE60";
        }

        public double GetStockPercentage(int currentQuantity, int threshold)
        {
            if (threshold <= 0) return 0;
            var percentage = (double)currentQuantity / threshold * 100;
            return Math.Min(percentage, 100);
        }

        public string FormatQuantity(int quantity)
        {
            return $"{quantity}g";
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }
}
