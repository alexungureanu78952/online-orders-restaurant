using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Services;
using RestaurantOrderManagement.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantOrderManagement.ViewModels
{
    /// <summary>
    /// ViewModel for advanced search and filtering
    /// Supports keyword search, allergen include/exclude filters, and category grouping
    /// </summary>
    public partial class SearchViewModel : BaseViewModel
    {
        private readonly IProductService _productService;

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private string includeAllergens = string.Empty;

        [ObservableProperty]
        private string excludeAllergens = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ProductGroupDTO> groupedResults = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private int resultCount = 0;

        public SearchViewModel(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Execute search with filters
        /// </summary>
        [RelayCommand]
        public async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery) &&
                string.IsNullOrWhiteSpace(IncludeAllergens) &&
                string.IsNullOrWhiteSpace(ExcludeAllergens))
            {
                ErrorMessage = "Please enter search criteria";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                GroupedResults.Clear();

                // Call service with parameters (convert empty strings to null)
                var keyword = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();
                var includeAllergensParam = string.IsNullOrWhiteSpace(IncludeAllergens) ? null : IncludeAllergens.Trim();
                var excludeAllergensParam = string.IsNullOrWhiteSpace(ExcludeAllergens) ? null : ExcludeAllergens.Trim();

                var results = await _productService.SearchProductsAsync(keyword, includeAllergensParam, excludeAllergensParam);

                if (results == null || !results.Any())
                {
                    ErrorMessage = "No products found matching your criteria";
                    ResultCount = 0;
                    return;
                }

                // Group by category
                var grouped = results
                    .GroupBy(p => p.CategoryName)
                    .OrderBy(g => g.Key)
                    .Select(g => new ProductGroupDTO
                    {
                        CategoryName = g.Key,
                        Products = new ObservableCollection<ProductDTO>(g.OrderBy(p => p.Name))
                    })
                    .ToList();

                foreach (var group in grouped)
                {
                    GroupedResults.Add(group);
                }

                ResultCount = results.Count();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Search failed: {ex.Message}";
                GroupedResults.Clear();
                ResultCount = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Clear all filters and results
        /// </summary>
        [RelayCommand]
        public void ClearFilters()
        {
            SearchQuery = string.Empty;
            IncludeAllergens = string.Empty;
            ExcludeAllergens = string.Empty;
            ErrorMessage = string.Empty;
            GroupedResults.Clear();
            ResultCount = 0;
        }

        /// <summary>
        /// Add allergen to include filter
        /// Format: comma-separated allergen names or IDs
        /// </summary>
        [RelayCommand]
        public void AddIncludeAllergen(string allergenId)
        {
            if (!string.IsNullOrWhiteSpace(allergenId))
            {
                if (string.IsNullOrEmpty(IncludeAllergens))
                {
                    IncludeAllergens = allergenId.Trim();
                }
                else if (!IncludeAllergens.Contains(allergenId))
                {
                    IncludeAllergens += "," + allergenId.Trim();
                }
            }
        }

        /// <summary>
        /// Add allergen to exclude filter
        /// </summary>
        [RelayCommand]
        public void AddExcludeAllergen(string allergenId)
        {
            if (!string.IsNullOrWhiteSpace(allergenId))
            {
                if (string.IsNullOrEmpty(ExcludeAllergens))
                {
                    ExcludeAllergens = allergenId.Trim();
                }
                else if (!ExcludeAllergens.Contains(allergenId))
                {
                    ExcludeAllergens += "," + allergenId.Trim();
                }
            }
        }

        /// <summary>
        /// Remove allergen from include filter
        /// </summary>
        [RelayCommand]
        public void RemoveIncludeAllergen(string allergenId)
        {
            if (!string.IsNullOrWhiteSpace(allergenId) && IncludeAllergens.Contains(allergenId))
            {
                var allergens = IncludeAllergens.Split(',')
                    .Where(a => a.Trim() != allergenId.Trim())
                    .ToList();
                IncludeAllergens = string.Join(",", allergens);
            }
        }

        /// <summary>
        /// Remove allergen from exclude filter
        /// </summary>
        [RelayCommand]
        public void RemoveExcludeAllergen(string allergenId)
        {
            if (!string.IsNullOrWhiteSpace(allergenId) && ExcludeAllergens.Contains(allergenId))
            {
                var allergens = ExcludeAllergens.Split(',')
                    .Where(a => a.Trim() != allergenId.Trim())
                    .ToList();
                ExcludeAllergens = string.Join(",", allergens);
            }
        }
    }

    /// <summary>
    /// DTO for grouping products by category in search results
    /// </summary>
    public class ProductGroupDTO
    {
        public string CategoryName { get; set; }
        public ObservableCollection<ProductDTO> Products { get; set; } = new ObservableCollection<ProductDTO>();
    }
}
