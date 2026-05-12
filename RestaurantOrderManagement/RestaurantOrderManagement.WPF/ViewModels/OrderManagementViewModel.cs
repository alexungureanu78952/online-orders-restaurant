using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    public partial class OrderManagementViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;

        [ObservableProperty]
        private ObservableCollection<Order> allOrders = new();

        [ObservableProperty]
        private Order? selectedOrder;

        [ObservableProperty]
        private string statusFilter = "active";

        [ObservableProperty]
        private string newStatusSelection = "se pregateste";

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> statusOptions = new(new[]
        {
            "inregistrata",
            "se pregateste",
            "a plecat la client",
            "livrata",
            "anulata"
        });

        public OrderManagementViewModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [RelayCommand]
        public async Task LoadAllOrdersAsync()
        {
            await LoadOrdersAsync(clearMessages: true);
        }

        [RelayCommand]
        public async Task SetStatusFilterAsync(string filter)
        {
            StatusFilter = string.IsNullOrWhiteSpace(filter) ? "active" : filter;
            await LoadOrdersAsync(clearMessages: true);
        }

        private async Task LoadOrdersAsync(bool clearMessages)
        {
            try
            {
                IsLoading = true;
                if (clearMessages)
                    ClearMessages();

                IEnumerable<Order> orders;

                if (StatusFilter == "active")
                {
                    orders = await _orderService.GetAllOrdersAsync(limit: 500);
                    orders = orders.Where(o => o.Status != "livrata" && o.Status != "anulata");
                }
                else if (StatusFilter == "all")
                {
                    orders = await _orderService.GetAllOrdersAsync(limit: 500);
                }
                else
                {
                    orders = await _orderService.GetAllOrdersAsync(limit: 500);
                    orders = orders.Where(o => o.Status == StatusFilter);
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

                var updatedOrderId = SelectedOrder.OrderId;
                var updatedOrderCode = SelectedOrder.OrderCode;
                var updatedStatus = NewStatusSelection;

                var updated = await _orderService.UpdateOrderStatusAsync(updatedOrderId, updatedStatus);
                if (updated)
                {
                    var switchedToAllOrders = StatusFilter == "active" && IsTerminalStatus(updatedStatus);
                    if (switchedToAllOrders)
                        StatusFilter = "all";

                    await LoadOrdersAsync(clearMessages: false);
                    SelectedOrder = AllOrders.FirstOrDefault(order => order.OrderId == updatedOrderId);
                    NewStatusSelection = GetNextStatusSelection(SelectedOrder?.Status ?? updatedStatus);

                    SuccessMessage = switchedToAllOrders
                        ? $"Order {updatedOrderCode} updated to {updatedStatus}. Switched to All Orders so it remains visible."
                        : $"Order {updatedOrderCode} updated to {updatedStatus}";
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

        private static bool IsTerminalStatus(string status)
        {
            return status == "livrata" || status == "anulata";
        }

        private static string GetNextStatusSelection(string currentStatus)
        {
            return currentStatus switch
            {
                "inregistrata" => "se pregateste",
                "se pregateste" => "a plecat la client",
                "a plecat la client" => "livrata",
                _ => "se pregateste"
            };
        }

        public string GetCustomerName(Order order)
        {
            return order?.User != null
                ? $"{order.User.FirstName} {order.User.LastName}"
                : "Unknown";
        }

        public string GetOrderSummary(Order order)
        {
            if (order?.OrderItems == null || order.OrderItems.Count == 0)
                return "No items";

            var itemCount = order.OrderItems.Count;
            var itemSum = order.OrderItems.Sum(oi => oi.Quantity);
            return $"{itemCount} items, {itemSum} buc total";
        }

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

        public string GetStatusColor(string status)
        {
            return status switch
            {
                "inregistrata" => "#3498DB",
                "se pregateste" => "#F39C12",
                "a plecat la client" => "#E67E22",
                "livrata" => "#27AE60",
                "anulata" => "#E74C3C",
                _ => "#95A5A6"
            };
        }

        public bool CanTransitionTo(string currentStatus, string newStatus)
        {
            if (currentStatus == "livrata" || currentStatus == "anulata")
                return false;

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
