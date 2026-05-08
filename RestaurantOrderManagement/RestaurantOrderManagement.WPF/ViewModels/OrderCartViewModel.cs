using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services;
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

            var existingItem = CartItems.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                return;
            }

            CartItems.Add(new CartItemViewModel
            {
                ProductId = productId,
                ProductName = $"Product {productId}",
                UnitPrice = 0,
                Quantity = quantity
            });
        }

        [RelayCommand]
        public void RemoveFromCart(int productId)
        {
            var item = CartItems.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                CartItems.Remove(item);
            }
        }

        [RelayCommand]
        public void UpdateQuantity(object parameter)
        {
            if (parameter is not (int productId, int newQuantity))
                return;

            if (newQuantity <= 0)
            {
                RemoveFromCart(productId);
                return;
            }

            var item = CartItems.FirstOrDefault(c => c.ProductId == productId);
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

                var items = CartItems.Select(c => (ProductId: c.ProductId, Quantity: c.Quantity)).ToList();
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
                IsLoading = false;
            }
        }

        private void RecalculateTotals()
        {
            SubTotal = CartItems.Sum(c => c.UnitPrice * c.Quantity);
            ShippingFee = 0;
            DiscountAmount = 0;
            TotalCost = SubTotal + ShippingFee - DiscountAmount;
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }

    public partial class CartItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private int productId;

        [ObservableProperty]
        private string productName = string.Empty;

        [ObservableProperty]
        private decimal unitPrice;

        [ObservableProperty]
        private int quantity;

        public decimal ItemTotal => UnitPrice * Quantity;

        partial void OnQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(ItemTotal));
        }

        partial void OnUnitPriceChanged(decimal value)
        {
            OnPropertyChanged(nameof(ItemTotal));
        }
    }
}
