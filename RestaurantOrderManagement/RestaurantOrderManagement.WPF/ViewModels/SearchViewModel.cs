using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Services.DTOs;
using RestaurantOrderManagement.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantOrderManagement.ViewModels
{
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
        private ObservableCollection<RestaurantMenuGroupDTO> groupedResults = new();

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

                var keyword = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();
                var includeAllergensParam = string.IsNullOrWhiteSpace(IncludeAllergens) ? null : IncludeAllergens.Trim();
                var excludeAllergensParam = string.IsNullOrWhiteSpace(ExcludeAllergens) ? null : ExcludeAllergens.Trim();

                var results = (await _productService.SearchRestaurantMenuAsync(keyword, includeAllergensParam, excludeAllergensParam))
                    .ToList();

                var resultCount = results.Sum(group => group.ItemCount);
                if (resultCount == 0)
                {
                    ErrorMessage = "No menu items found matching your criteria";
                    ResultCount = 0;
                    return;
                }

                foreach (var group in results.OrderBy(group => group.CategoryName))
                {
                    GroupedResults.Add(group);
                }

                ResultCount = resultCount;
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

}
