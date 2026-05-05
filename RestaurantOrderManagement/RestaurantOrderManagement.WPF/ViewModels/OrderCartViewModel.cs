using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    /// <summary>
    /// MVVM ViewModel for shopping cart and order checkout
    /// Manages cart items, pricing calculations, and order submission
    /// </summary>
    public partial class OrderCartViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IConfigurationService _configService;
        private int _currentUserId = 0;

        /// <summary>
        /// Cart items with product details and quantity
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CartItemViewModel> cartItems = new();

        /// <summary>
        /// Subtotal (sum of item prices before fees/discounts)
        /// </summary>
        [ObservableProperty]
        private decimal subTotal = 0;

        /// <summary>
        /// Shipping fee (0 if free shipping threshold reached)
        /// </summary>
        [ObservableProperty]
        private decimal shippingFee = 0;

        /// <summary>
        /// Discount amount applied based on order total or customer status
        /// </summary>
        [ObservableProperty]
        private decimal discountAmount = 0;

        /// <summary>
        /// Total cost (SubTotal + ShippingFee - DiscountAmount)
        /// </summary>
        [ObservableProperty]
        private decimal totalCost = 0;

        /// <summary>
        /// Delivery address (defaults to user's address, can be overridden)
        /// </summary>
        [ObservableProperty]
        private string deliveryAddress = string.Empty;

        /// <summary>
        /// Error message display
        /// </summary>
        [ObservableProperty]
        private string errorMessage = string.Empty;

        /// <summary>
        /// Success message with OrderCode display
        /// </summary>
        [ObservableProperty]
        private string successMessage = string.Empty;

        /// <summary>
        /// Flag indicating async operation in progress
        /// </summary>
        [ObservableProperty]
        private bool isLoading = false;

        /// <summary>
        /// Flag indicating checkout has completed successfully
        /// </summary>
        [ObservableProperty]
        private bool isCheckoutComplete = false;

        /// <summary>
        /// OrderCode returned after successful order creation
        /// </summary>
        [ObservableProperty]
        private string orderCode = string.Empty;

        /// <summary>
        /// Count of items in cart
        /// </summary>
        public int ItemCount => CartItems.Sum(c => c.Quantity);

        /// <summary>
        /// True if cart is empty (no checkout allowed)
        /// </summary>
        public bool IsCartEmpty => CartItems.Count == 0;

        public OrderCartViewModel(IOrderService orderService, IProductService productService, 
                                 IConfigurationService configService)
        {
            _orderService = orderService;
            _productService = productService;
            _configService = configService;
        }

        /// <summary>
        /// Initialize cart with current user
        /// </summary>
        public void InitializeCart(int userId, string userDeliveryAddress)
        {
            _currentUserId = userId;
            DeliveryAddress = userDeliveryAddress;
            CartItems.Clear();
            ClearMessages();
            IsCheckoutComplete = false;
        }

        /// <summary>
        /// Add product to cart (or increment quantity if already exists)
        /// </summary>
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

            // Check if product already in cart
            var existingItem = CartItems.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                // Add new item (note: in real implementation, would fetch full product details)
                var newItem = new CartItemViewModel
                {
                    ProductId = productId,
                    ProductName = $"Product {productId}",
                    UnitPrice = 0,
                    Quantity = quantity
                };
                CartItems.Add(newItem);
            }

            RecalculateTotals();
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [RelayCommand]
        public void RemoveFromCart(int productId)
        {
            var item = CartItems.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                CartItems.Remove(item);
                RecalculateTotals();
            }
        }

        /// <summary>
        /// Update quantity for cart item
        /// </summary>
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
                RecalculateTotals();
            }
        }

        /// <summary>
        /// Clear all items from cart
        /// </summary>
        [RelayCommand]
        public void ClearCart()
        {
            CartItems.Clear();
            ClearMessages();
            RecalculateTotals();
        }

        /// <summary>
        /// Submit order with validation
        /// Flow: Validate cart → Calculate totals → Call OrderService.CreateOrderAsync → Display OrderCode
        /// </summary>
        [RelayCommand]
        public async Task SubmitOrderAsync()
        {
            try
            {
                ClearMessages();
                IsLoading = true;

                // Validate cart
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

                // Build items list for OrderService
                var items = CartItems
                    .Select(c => (ProductId: c.ProductId, Quantity: c.Quantity))
                    .ToList();

                // Create order
                var order = await _orderService.CreateOrderAsync(_currentUserId, items, DeliveryAddress);

                // Success - display order code
                OrderCode = order.OrderCode;
                SuccessMessage = $"Order created successfully! Order Code: {order.OrderCode}";
                IsCheckoutComplete = true;

                // Clear cart after successful order
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

        /// <summary>
        /// Recalculate totals, fees, and discounts
        /// </summary>
        private void RecalculateTotals()
        {
            // Calculate subtotal
            SubTotal = CartItems.Sum(c => c.UnitPrice * c.Quantity);

            // TODO: Calculate shipping fee and discount (requires IConfigurationService calls)
            // For now, use placeholder calculations
            ShippingFee = 0;
            DiscountAmount = 0;

            TotalCost = SubTotal + ShippingFee - DiscountAmount;
        }

        /// <summary>
        /// Clear error and success messages
        /// </summary>
        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }

    /// <summary>
    /// ViewModel for individual cart items
    /// </summary>
    public partial class CartItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private int productId;

        [ObservableProperty]
        private string productName;

        [ObservableProperty]
        private decimal unitPrice;

        [ObservableProperty]
        private int quantity;

        /// <summary>
        /// ItemTotal = UnitPrice * Quantity
        /// </summary>
        public decimal ItemTotal => UnitPrice * Quantity;
    }
}
