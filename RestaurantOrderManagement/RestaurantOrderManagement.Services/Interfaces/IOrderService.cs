using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Services.Interfaces
{
    /// <summary>
    /// Service for managing orders, including creation, status updates, and retrieval
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Create new order from cart items with automatic discount/fee calculation
        /// </summary>
        /// <param name="userId">Customer placing the order</param>
        /// <param name="items">List of product ID + quantity pairs</param>
        /// <param name="deliveryAddress">Delivery location (can override user default)</param>
        /// <returns>Newly created order with OrderCode and calculated totals</returns>
        /// <exception cref="ArgumentException">Validation error (empty items, invalid delivery address)</exception>
        /// <exception cref="InvalidOperationException">Insufficient inventory or other business rule violation</exception>
        Task<Order> CreateOrderAsync(int userId, List<(int ProductId, int Quantity)> items, string deliveryAddress);

        /// <summary>
        /// Create new order from product and bundled-menu cart items.
        /// </summary>
        Task<Order> CreateOrderAsync(int userId, List<OrderRequestItem> items, string deliveryAddress);

        /// <summary>
        /// Calculate order pricing from the current subtotal using the same discount and shipping rules
        /// used when the order is persisted.
        /// </summary>
        Task<OrderPricingQuote> CalculatePricingAsync(int userId, decimal subTotal);

        /// <summary>
        /// Get all orders for a specific user, sorted by date descending
        /// </summary>
        /// <param name="userId">User ID to retrieve orders for</param>
        /// <param name="limit">Maximum number of orders to return (default 50)</param>
        /// <returns>List of user's orders with line items populated</returns>
        Task<List<Order>> GetUserOrdersAsync(int userId, int limit = 50);

        /// <summary>
        /// Get active orders for user (not delivered or cancelled)
        /// </summary>
        /// <param name="userId">User ID to retrieve active orders for</param>
        /// <returns>List of active orders (status not 'livrata' or 'anulata')</returns>
        Task<List<Order>> GetUserActiveOrdersAsync(int userId);

        /// <summary>
        /// Get order details including all line items
        /// </summary>
        /// <param name="orderId">Order ID to retrieve</param>
        /// <returns>Order with OrderItems populated</returns>
        Task<Order> GetOrderDetailAsync(int orderId);

        /// <summary>
        /// Update order status (employee operation)
        /// </summary>
        /// <param name="orderId">Order to update</param>
        /// <param name="newStatus">New status (e.g., 'se pregateste', 'a plecat la client', 'livrata', 'anulata')</param>
        /// <returns>True if successful, false if order not found</returns>
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);

        /// <summary>
        /// Cancel active order (client operation). Inventory is changed only when an order is delivered.
        /// </summary>
        /// <param name="orderId">Order to cancel</param>
        /// <returns>True if successful, false if already delivered/cancelled</returns>
        Task<bool> CancelOrderAsync(int orderId);

        /// <summary>
        /// Get all orders (employee view)
        /// </summary>
        /// <param name="limit">Maximum number of orders to return (default 100)</param>
        /// <returns>List of all orders sorted by date descending</returns>
        Task<List<Order>> GetAllOrdersAsync(int limit = 100);

        /// <summary>
        /// Get all active orders (employee view)
        /// </summary>
        /// <returns>List of all orders not yet delivered or cancelled</returns>
        Task<List<Order>> GetAllActiveOrdersAsync();
    }

    public class OrderRequestItem
    {
        public string ItemType { get; set; } = "Preparat";
        public int? ProductId { get; set; }
        public int? MenuId { get; set; }
        public int Quantity { get; set; }

        public static OrderRequestItem Product(int productId, int quantity)
        {
            return new OrderRequestItem
            {
                ItemType = "Preparat",
                ProductId = productId,
                Quantity = quantity
            };
        }

        public static OrderRequestItem Menu(int menuId, int quantity)
        {
            return new OrderRequestItem
            {
                ItemType = "Meniu",
                MenuId = menuId,
                Quantity = quantity
            };
        }
    }

    public class OrderPricingQuote
    {
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalCost { get; set; }
    }
}
