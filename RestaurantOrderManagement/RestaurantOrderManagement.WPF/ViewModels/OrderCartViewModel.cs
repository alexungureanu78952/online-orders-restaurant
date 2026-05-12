using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services;
using RestaurantOrderManagement.Services.DTOs;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    public partial class OrderCartViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IConfigurationService _configService;
        private readonly SemaphoreSlim _pricingLock = new(1, 1);
        private int _pricingVersion;
        private int _currentUserId;

        [ObservableProperty]
        private ObservableCollection<CartItemViewModel> cartItems = new();

        [ObservableProperty]
        private decimal subTotal;

        [ObservableProperty]
        private decimal shippingFee;

        [ObservableProperty]
        private decimal discountAmount;

        [ObservableProperty]
        private decimal totalCost;

        [ObservableProperty]
        private string deliveryAddress = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isCheckoutComplete;

        [ObservableProperty]
        private string orderCode = string.Empty;

        public int ItemCount => CartItems.Sum(c => c.Quantity);
        public bool IsCartEmpty => CartItems.Count == 0;
        public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool HasSuccessMessage => !string.IsNullOrWhiteSpace(SuccessMessage);
        public bool IsNotLoading => !IsLoading;
        public bool CanCheckout => !IsLoading && !IsCartEmpty && !string.IsNullOrWhiteSpace(DeliveryAddress);
        public string SubTotalText => FormatLei(SubTotal);
        public string ShippingFeeText => FormatLei(ShippingFee);
        public string DiscountAmountText => DiscountAmount > 0 ? $"-{FormatLei(DiscountAmount)}" : FormatLei(0);
        public string TotalCostText => FormatLei(TotalCost);

        public OrderCartViewModel(IOrderService orderService, IProductService productService, IConfigurationService configService)
        {
            _orderService = orderService;
            _productService = productService;
            _configService = configService;

            HookCartCollection(CartItems);
            RecalculateTotals();
        }

        partial void OnCartItemsChanged(ObservableCollection<CartItemViewModel> value)
        {
            HookCartCollection(value);
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(IsCartEmpty));
            OnPropertyChanged(nameof(CanCheckout));
        }

        partial void OnDeliveryAddressChanged(string value)
        {
            OnPropertyChanged(nameof(CanCheckout));
        }

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasErrorMessage));
        }

        partial void OnSuccessMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasSuccessMessage));
        }

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotLoading));
            OnPropertyChanged(nameof(CanCheckout));
        }

        partial void OnSubTotalChanged(decimal value)
        {
            OnPropertyChanged(nameof(SubTotalText));
        }

        partial void OnShippingFeeChanged(decimal value)
        {
            OnPropertyChanged(nameof(ShippingFeeText));
        }

        partial void OnDiscountAmountChanged(decimal value)
        {
            OnPropertyChanged(nameof(DiscountAmountText));
        }

        partial void OnTotalCostChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalCostText));
        }

        private void HookCartCollection(ObservableCollection<CartItemViewModel> collection)
        {
            collection.CollectionChanged -= CartItems_CollectionChanged;
            collection.CollectionChanged += CartItems_CollectionChanged;

            foreach (var item in collection)
            {
                item.PropertyChanged -= CartItem_PropertyChanged;
                item.PropertyChanged += CartItem_PropertyChanged;
            }
        }

        private void CartItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CartItemViewModel item in e.NewItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                    item.PropertyChanged += CartItem_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (CartItemViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
            }

            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(IsCartEmpty));
            OnPropertyChanged(nameof(CanCheckout));
            RecalculateTotals();
        }

        private void CartItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItemViewModel.Quantity) || e.PropertyName == nameof(CartItemViewModel.UnitPrice))
            {
                OnPropertyChanged(nameof(ItemCount));
                RecalculateTotals();
            }
        }

        public void InitializeCart(int userId, string userDeliveryAddress)
        {
            _currentUserId = userId;
            DeliveryAddress = userDeliveryAddress ?? string.Empty;
            CartItems.Clear();
            ClearMessages();
            IsCheckoutComplete = false;
            OrderCode = string.Empty;
        }

        [RelayCommand]
        public void AddToCart(object parameter)
        {
            if (parameter is not (int productId, int quantity))
                return;

            if (quantity <= 0)
            {
                ErrorMessage = "Quantity must be greater than 0";
                return;
            }

            ClearMessages();

            var orderKey = $"P:{productId}";
            var existingItem = CartItems.FirstOrDefault(c => c.OrderKey == orderKey);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                return;
            }

            CartItems.Add(new CartItemViewModel
            {
                OrderKey = orderKey,
                ProductId = productId,
                ItemType = "Preparat",
                ProductName = $"Product {productId}",
                UnitPrice = 0,
                Quantity = quantity
            });
        }

        [RelayCommand]
        public void AddMenuItem(RestaurantMenuItemDTO item)
        {
            if (item == null)
                return;

            if (!item.IsAvailable)
            {
                ErrorMessage = $"{item.Name} is unavailable.";
                return;
            }

            if (!TryParseOrderKey(item.OrderKey, out var itemType, out var itemId))
            {
                ErrorMessage = "This menu item cannot be added to the cart.";
                return;
            }

            ClearMessages();

            var existingItem = CartItems.FirstOrDefault(c => c.OrderKey == item.OrderKey);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
                return;
            }

            CartItems.Add(new CartItemViewModel
            {
                OrderKey = item.OrderKey,
                ProductId = itemType == "P" ? itemId : 0,
                MenuId = itemType == "M" ? itemId : 0,
                ItemType = item.ItemType,
                ProductName = item.Name,
                UnitPrice = item.Price,
                Quantity = 1
            });
        }

        [RelayCommand]
        public void RemoveFromCart(string orderKey)
        {
            var item = CartItems.FirstOrDefault(c => c.OrderKey == orderKey);
            if (item != null)
            {
                CartItems.Remove(item);
            }
        }

        [RelayCommand]
        public void UpdateQuantity(object parameter)
        {
            if (parameter is not (string orderKey, int newQuantity))
                return;

            if (newQuantity <= 0)
            {
                RemoveFromCart(orderKey);
                return;
            }

            var item = CartItems.FirstOrDefault(c => c.OrderKey == orderKey);
            if (item != null)
            {
                item.Quantity = newQuantity;
            }
        }

        [RelayCommand]
        public void ClearCart()
        {
            CartItems.Clear();
            ClearMessages();
            RecalculateTotals();
        }

        [RelayCommand]
        public async Task SubmitOrderAsync()
        {
            var lockTaken = false;

            try
            {
                ClearMessages();
                IsLoading = true;

                if (CartItems.Count == 0)
                {
                    ErrorMessage = "Cart is empty. Add items before checkout.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(DeliveryAddress))
                {
                    ErrorMessage = "Delivery address is required";
                    return;
                }

                var items = CartItems.Select(c => c.ToOrderRequestItem()).ToList();
                await _pricingLock.WaitAsync();
                lockTaken = true;

                var order = await _orderService.CreateOrderAsync(_currentUserId, items, DeliveryAddress);

                OrderCode = order.OrderCode;
                SuccessMessage = $"Order created successfully! Order Code: {order.OrderCode}";
                IsCheckoutComplete = true;

                CartItems.Clear();
                RecalculateTotals();
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = $"Validation error: {ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = $"Order error: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                if (lockTaken)
                    _pricingLock.Release();

                IsLoading = false;
            }
        }

        private void RecalculateTotals()
        {
            SubTotal = CartItems.Sum(c => c.UnitPrice * c.Quantity);

            var version = Interlocked.Increment(ref _pricingVersion);
            if (SubTotal <= 0)
            {
                ShippingFee = 0;
                DiscountAmount = 0;
                TotalCost = 0;
                return;
            }

            _ = ApplyConfiguredTotalsAsync(version, SubTotal);
        }

        private async Task ApplyConfiguredTotalsAsync(int version, decimal quotedSubtotal)
        {
            await _pricingLock.WaitAsync();
            try
            {
                if (version != _pricingVersion)
                    return;

                var quote = await _orderService.CalculatePricingAsync(_currentUserId, quotedSubtotal);
                if (version != _pricingVersion || SubTotal != quotedSubtotal)
                    return;

                ShippingFee = quote.ShippingFee;
                DiscountAmount = quote.DiscountAmount;
                TotalCost = quote.TotalCost;
            }
            catch
            {
                if (version != _pricingVersion)
                    return;

                ShippingFee = 0;
                DiscountAmount = 0;
                TotalCost = SubTotal;
            }
            finally
            {
                _pricingLock.Release();
            }
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        private static bool TryParseOrderKey(string orderKey, out string itemType, out int itemId)
        {
            itemType = string.Empty;
            itemId = 0;

            var parts = (orderKey ?? string.Empty).Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out itemId))
                return false;

            itemType = parts[0].ToUpperInvariant();
            return itemType is "P" or "M" && itemId > 0;
        }

        private static string FormatLei(decimal amount)
        {
            return $"{amount:F2} lei";
        }
    }

    public partial class CartItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string orderKey = string.Empty;

        [ObservableProperty]
        private int productId;

        [ObservableProperty]
        private int menuId;

        [ObservableProperty]
        private string itemType = "Preparat";

        [ObservableProperty]
        private string productName = string.Empty;

        [ObservableProperty]
        private decimal unitPrice;

        [ObservableProperty]
        private int quantity;

        public decimal ItemTotal => UnitPrice * Quantity;
        public string UnitPriceText => FormatLei(UnitPrice);
        public string ItemTotalText => FormatLei(ItemTotal);

        partial void OnQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(ItemTotal));
            OnPropertyChanged(nameof(ItemTotalText));
        }

        partial void OnUnitPriceChanged(decimal value)
        {
            OnPropertyChanged(nameof(ItemTotal));
            OnPropertyChanged(nameof(UnitPriceText));
            OnPropertyChanged(nameof(ItemTotalText));
        }

        public OrderRequestItem ToOrderRequestItem()
        {
            if (MenuId > 0)
                return OrderRequestItem.Menu(MenuId, Quantity);

            return OrderRequestItem.Product(ProductId, Quantity);
        }

        private static string FormatLei(decimal amount)
        {
            return $"{amount:F2} lei";
        }
    }
}
