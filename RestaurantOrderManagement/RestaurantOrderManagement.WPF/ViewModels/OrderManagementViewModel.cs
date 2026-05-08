using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    /// <summary>
    /// MVVM ViewModel for employee order management
    /// Allows employees to view all orders, filter by status, and update order status
    /// </summary>
    public partial class OrderManagementViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;

        [ObservableProperty]
        private ObservableCollection<Order> allOrders = new();

        [ObservableProperty]
        private Order selectedOrder;

        [ObservableProperty]
        private string statusFilter = "active"; // active, all, pending, preparing, intransit, delivered, cancelled

        [ObservableProperty]
        private string newStatusSelection = "se pregateste"; // Status to update to

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        // Status options for dropdown
        [ObservableProperty]
        private ObservableCollection<string> statusOptions = new(new[]
        {
            "inregistrata",
            "se pregateste",
            "a plecat la client",
            "livrata"
        });

        public OrderManagementViewModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Load all orders for management
        /// </summary>
        [RelayCommand]
        public async Task LoadAllOrdersAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                // Load orders based on filter
                IEnumerable<Order> orders = null;

                if (StatusFilter == "active")
                {
                    // Active orders: not delivered and not cancelled
                    orders = await _orderService.GetAllOrdersAsync(status: null, limit: 500);
                    orders = orders.Where(o => o.Status != "livrata" && o.Status != "anulata");
                }
                else if (StatusFilter == "all")
                {
                    orders = await _orderService.GetAllOrdersAsync(status: null, limit: 500);
                }
                else
                {
                    orders = await _orderService.GetAllOrdersAsync(status: StatusFilter, limit: 500);
                }

                AllOrders.Clear();
                foreach (var order in orders.OrderByDescending(o => o.OrderDate))
                {
                    AllOrders.Add(order);
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
        /// Update selected order status
        /// </summary>
        [RelayCommand]
        public async Task UpdateOrderStatusAsync()
        {
            if (SelectedOrder == null)
            {
                ErrorMessage = "Please select an order to update";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewStatusSelection))
            {
                ErrorMessage = "Please select a new status";
                return;
            }

            try
            {
                IsLoading = true;
                ClearMessages();

                var updated = await _orderService.UpdateOrderStatusAsync(SelectedOrder.OrderId, NewStatusSelection);
                if (updated)
                {
                    SuccessMessage = $"Order {SelectedOrder.OrderCode} updated to {NewStatusSelection}";

                    // Refresh the list to show updated status
                    await LoadAllOrdersAsync();
                    SelectedOrder = null;
                    NewStatusSelection = "se pregateste";
                }
                else
                {
                    ErrorMessage = "Failed to update order status";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating order: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Get customer display name
        /// </summary>
        public string GetCustomerName(Order order)
        {
            return order?.User != null
                ? $"{order.User.FirstName} {order.User.LastName}"
                : "Unknown";
        }

        /// <summary>
        /// Get order summary
        /// </summary>
        public string GetOrderSummary(Order order)
        {
            if (order?.OrderItems == null || order.OrderItems.Count == 0)
                return "No items";

            var itemCount = order.OrderItems.Count;
            var itemSum = order.OrderItems.Sum(oi => oi.Quantity);
            return $"{itemCount} items, {itemSum}g total";
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

        /// <summary>
        /// Check if order can transition to new status
        /// </summary>
        public bool CanTransitionTo(string currentStatus, string newStatus)
        {
            // Delivered and cancelled orders cannot be modified
            if (currentStatus == "livrata" || currentStatus == "anulata")
                return false;

            // Valid transitions
            var validTransitions = new Dictionary<string, List<string>>
            {
                { "inregistrata", new[] { "se pregateste", "anulata" }.ToList() },
                { "se pregateste", new[] { "a plecat la client", "anulata" }.ToList() },
                { "a plecat la client", new[] { "livrata" }.ToList() }
            };

            return validTransitions.ContainsKey(currentStatus) && validTransitions[currentStatus].Contains(newStatus);
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }
}
