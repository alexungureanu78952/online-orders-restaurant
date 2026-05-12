using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Services.DTOs;
using RestaurantOrderManagement.Services.Interfaces;
using RestaurantOrderManagement.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RestaurantOrderManagement.ViewModels
{
    public partial class MenuBrowseViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private OrderCartViewModel? _orderCartViewModel;
        private readonly SemaphoreSlim _loadGate = new(1, 1);

        [ObservableProperty]
        private ObservableCollection<CategoryDTO> categories = new();

        [ObservableProperty]
        private ObservableCollection<RestaurantMenuGroupDTO> menuGroups = new();

        [ObservableProperty]
        private string searchQuery = string.Empty;

        [ObservableProperty]
        private int resultCount;

        [ObservableProperty]
        private CategoryDTO? selectedCategory;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string cartStatusMessage = string.Empty;

        [ObservableProperty]
        private bool canAddItems;

        private static readonly CategoryDTO AllCategories = new()
        {
            Name = "Toate categoriile",
            Description = "Afiseaza toate preparatele si meniurile",
            IsActive = true
        };

        public MenuBrowseViewModel(IProductService productService)
        {
            _productService = productService;
        }

        public void ConfigureCart(OrderCartViewModel orderCartViewModel, bool canAddItems)
        {
            _orderCartViewModel = orderCartViewModel;
            CanAddItems = canAddItems;
            CartStatusMessage = canAddItems
                ? "Alege preparate sau meniuri si adauga-le in cos."
                : string.Empty;
        }

        [RelayCommand]
        public async Task LoadCategoriesAsync()
        {
            await _loadGate.WaitAsync();
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var categories = (await _productService.GetCategoriesAsync()).ToList();
                Categories = new ObservableCollection<CategoryDTO>(
                    new[] { AllCategories }.Concat(categories));

                if (Categories.Count > 0)
                {
                    SelectedCategory = Categories[0];
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ViewModelErrorMessages.FromException("Failed to load categories", ex);
            }
            finally
            {
                IsLoading = false;
                _loadGate.Release();
            }
        }

        [RelayCommand]
        public async Task LoadMenuAsync()
        {
            await _loadGate.WaitAsync();
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var categoryName = SelectedCategory?.Name == AllCategories.Name ? null : SelectedCategory?.Name;
                var groups = await _productService.GetRestaurantMenuAsync(categoryName, SearchQuery);
                MenuGroups = new ObservableCollection<RestaurantMenuGroupDTO>(groups);
                ResultCount = MenuGroups.Sum(group => group.ItemCount);
            }
            catch (Exception ex)
            {
                ErrorMessage = ViewModelErrorMessages.FromException("Failed to load menu", ex);
                MenuGroups.Clear();
                ResultCount = 0;
            }
            finally
            {
                IsLoading = false;
                _loadGate.Release();
            }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            MenuGroups.Clear();
            await LoadCategoriesAsync();
            if (SelectedCategory != null)
            {
                await LoadMenuAsync();
            }
        }

        [RelayCommand]
        public async Task ApplyFiltersAsync()
        {
            await LoadMenuAsync();
        }

        [RelayCommand]
        public async Task ClearSearchAsync()
        {
            SearchQuery = string.Empty;
            await LoadMenuAsync();
        }

        [RelayCommand]
        public void AddMenuItemToCart(RestaurantMenuItemDTO item)
        {
            if (!CanAddItems || _orderCartViewModel == null)
            {
                CartStatusMessage = "Autentifica-te ca si client pentru a comanda.";
                return;
            }

            if (item == null)
                return;

            if (!item.IsAvailable)
            {
                CartStatusMessage = $"{item.Name} este indisponibil.";
                return;
            }

            if (string.IsNullOrWhiteSpace(item.OrderKey))
            {
                CartStatusMessage = "Acest item nu poate fi comandat momentan.";
                return;
            }

            _orderCartViewModel.AddMenuItem(item);
            CartStatusMessage = $"{item.Name} a fost adaugat in cos.";
        }

        public void ValidateNoIdsExposed()
        {
            foreach (var cat in Categories)
            {
                var idProperty = cat.GetType().GetProperty("CategoryId");
                if (idProperty != null)
                    throw new InvalidOperationException("CategoryDTO should not expose CategoryId");
            }

            foreach (var item in MenuGroups.SelectMany(group => group.Items))
            {
                var idProperty = item.GetType().GetProperties()
                    .FirstOrDefault(property => property.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
                if (idProperty != null)
                    throw new InvalidOperationException("Restaurant menu DTO should not expose database IDs");
            }
        }
    }
}
