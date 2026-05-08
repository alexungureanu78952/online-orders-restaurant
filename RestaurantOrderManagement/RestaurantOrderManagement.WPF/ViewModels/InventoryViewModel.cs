using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    /// <summary>
    /// MVVM ViewModel for inventory management
    /// Allows employees to view low-stock products and perform restock operations
    /// </summary>
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly IInventoryService _inventoryService;

        [ObservableProperty]
        private ObservableCollection<Product> lowStockProducts = new();

        [ObservableProperty]
        private Product selectedProduct;

        [ObservableProperty]
        private int restockQuantity = 500; // Default restock quantity in grams

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private int lowStockThreshold = 500; // Default, will be loaded from config

        public InventoryViewModel(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Load low-stock products and threshold
        /// </summary>
        [RelayCommand]
        public async Task LoadLowStockProductsAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                // Load threshold from configuration
                LowStockThreshold = await _inventoryService.GetLowStockThresholdAsync();

                // Load low-stock products
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

        /// <summary>
        /// Restock selected product
        /// </summary>
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
                    
                    // Refresh the list to show updated quantities
                    await LoadLowStockProductsAsync();
                    SelectedProduct = null;
                    RestockQuantity = 500; // Reset to default
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

        /// <summary>
        /// Get stock status display text
        /// </summary>
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

        /// <summary>
        /// Get stock status color for UI
        /// </summary>
        public string GetStockStatusColor(int currentQuantity, int threshold)
        {
            if (currentQuantity == 0)
                return "#E74C3C"; // Red - out of stock
            else if (currentQuantity < threshold / 2)
                return "#C0392B"; // Dark red - critical
            else if (currentQuantity < threshold)
                return "#E67E22"; // Orange - low
            else
                return "#27AE60"; // Green - adequate
        }

        /// <summary>
        /// Get stock percentage for progress bar
        /// </summary>
        public double GetStockPercentage(int currentQuantity, int threshold)
        {
            if (threshold <= 0) return 0;
            var percentage = (double)currentQuantity / threshold * 100;
            return Math.Min(percentage, 100); // Cap at 100%
        }

        /// <summary>
        /// Format quantity for display
        /// </summary>
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
