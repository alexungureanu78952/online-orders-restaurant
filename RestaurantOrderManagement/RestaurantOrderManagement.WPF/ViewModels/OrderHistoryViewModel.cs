using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    /// <summary>
    /// MVVM ViewModel for order history and order tracking
    /// Allows clients to view past orders and active orders, and cancel active orders
    /// </summary>
    public partial class OrderHistoryViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;
        private int _currentUserId;

        [ObservableProperty]
        private ObservableCollection<Order> userOrders = new();

        [ObservableProperty]
        private Order selectedOrder;

        [ObservableProperty]
        private string statusFilter = "all"; // all, active, completed, cancelled

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        public OrderHistoryViewModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Initialize view model with current user
        /// </summary>
        public void Initialize(int userId)
        {
            _currentUserId = userId;
            _ = LoadUserOrdersAsync();
        }

        /// <summary>
        /// Load all orders for current user
        /// </summary>
        [RelayCommand]
        public async Task LoadUserOrdersAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var orders = await _orderService.GetUserOrdersAsync(_currentUserId);
                UserOrders.Clear();

                foreach (var order in orders.OrderByDescending(o => o.OrderDate))
                {
                    UserOrders.Add(order);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load orders: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Filter orders based on current status filter
        /// </summary>
        public IEnumerable<Order> FilteredOrders
        {
            get
            {
                return StatusFilter switch
                {
                    "active" => UserOrders.Where(o => o.Status != "livrata" && o.Status != "anulata"),
                    "completed" => UserOrders.Where(o => o.Status == "livrata"),
                    "cancelled" => UserOrders.Where(o => o.Status == "anulata"),
                    _ => UserOrders
                };
            }
        }

        /// <summary>
        /// Cancel selected order
        /// </summary>
        [RelayCommand]
        public async Task CancelOrderAsync()
        {
            if (SelectedOrder == null)
            {
                ErrorMessage = "Please select an order to cancel";
                return;
            }

            if (SelectedOrder.Status == "livrata")
            {
                ErrorMessage = "Cannot cancel delivered orders";
                return;
            }

            if (SelectedOrder.Status == "anulata")
            {
                ErrorMessage = "Order is already cancelled";
                return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                var cancelled = await _orderService.CancelOrderAsync(SelectedOrder.OrderId);
                if (cancelled)
                {
                    SuccessMessage = $"Order {SelectedOrder.OrderCode} cancelled successfully";
                    await LoadUserOrdersAsync();
                    SelectedOrder = null;
                }
                else
                {
                    ErrorMessage = "Failed to cancel order";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error cancelling order: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Get status display text
        /// </summary>
        public string GetStatusDisplay(string status)
        {
            return status switch
            {
                "inregistrata" => "Registered",
                "se pregateste" => "Preparing",
                "a plecat la client" => "In Transit",
                "livrata" => "Delivered",
                "anulata" => "Cancelled",
                _ => status
            };
        }

        /// <summary>
        /// Get status color for UI
        /// </summary>
        public string GetStatusColor(string status)
        {
            return status switch
            {
                "inregistrata" => "#3498DB", // Blue
                "se pregateste" => "#F39C12", // Orange
                "a plecat la client" => "#E67E22", // Dark Orange
                "livrata" => "#27AE60", // Green
                "anulata" => "#E74C3C", // Red
                _ => "#95A5A6" // Gray
            };
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }
}
