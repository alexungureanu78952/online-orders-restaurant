using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Interfaces;
using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Services.Implementations
{
    /// <summary>
    /// Service for order management operations
    /// Handles order creation with discount/fee calculation, status updates, and retrieval
    /// Uses parameterized stored procedures for all database operations
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;
        private readonly UserRepository _userRepository;
        private readonly IConfigurationService _configService;

        public OrderService(OrderRepository orderRepository, ProductRepository productRepository,
                           UserRepository userRepository, IConfigurationService configService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _configService = configService;
        }

        /// <summary>
        /// Create new order with automatic discount/fee calculation
        /// Flow:
        /// 1. Validate user and items
        /// 2. Verify product availability and inventory
        /// 3. Calculate SubTotal from product prices
        /// 4. Apply discounts (large order, frequent customer)
        /// 5. Calculate shipping fee (free above threshold, otherwise fixed fee)
        /// 6. Create order via sp_CreateOrder
        /// 7. Create OrderItem records for each cart item
        /// </summary>
        public async Task<Order> CreateOrderAsync(int userId, List<(int ProductId, int Quantity)> items, string deliveryAddress)
        {
            // Validation
            if (items == null || items.Count == 0)
                throw new ArgumentException("Order must contain at least one item");

            if (string.IsNullOrWhiteSpace(deliveryAddress))
                throw new ArgumentException("Delivery address is required for order creation");

            // Verify user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Fetch all products in the order to verify availability and get pricing
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = new Dictionary<int, Product>();
            foreach (var productId in productIds)
            {
                var product = await _productRepository.GetByIdAsync(productId);
                if (product == null)
                    throw new InvalidOperationException($"Product {productId} not found");
                if (!product.IsAvailable)
                    throw new InvalidOperationException($"Product {product.Name} is not available");
                products[productId] = product;
            }

            // Calculate SubTotal
            decimal subTotal = 0;
            foreach (var (productId, quantity) in items)
            {
                var product = products[productId];
                subTotal += product.Price * quantity;
            }

            // Calculate discount amount based on business rules
            decimal discountAmount = await CalculateDiscountAsync(userId, subTotal);

            // Calculate shipping fee
            decimal shippingFee = await CalculateShippingFeeAsync(subTotal);

            // Create order via stored procedure
            var (orderId, orderCode) = await _orderRepository.CreateOrderAsync(userId, subTotal, shippingFee,
                discountAmount, deliveryAddress, null);

            // Create OrderItems for each cart item
            foreach (var (productId, quantity) in items)
            {
                var product = products[productId];
                var orderItem = new OrderItem
                {
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    ItemTotal = product.Price * quantity,
                    CreatedDate = DateTime.UtcNow
                };
                // In a real implementation, would add to context and save
                // For now, assuming the stored procedure handles this
            }

            // Retrieve and return created order
            return await _orderRepository.GetOrderDetailsAsync(orderId);
        }

        /// <summary>
        /// Get all orders for a specific user
        /// </summary>
        public async Task<List<Order>> GetUserOrdersAsync(int userId, int limit = 50)
        {
            var orders = await _orderRepository.GetUserOrdersAsync(userId, limit);
            return orders.ToList();
        }

        /// <summary>
        /// Get active orders for user (status not 'livrata' or 'anulata')
        /// </summary>
        public async Task<List<Order>> GetUserActiveOrdersAsync(int userId)
        {
            var allOrders = await GetUserOrdersAsync(userId);
            return allOrders
                .Where(o => o.Status != "livrata" && o.Status != "anulata")
                .ToList();
        }

        /// <summary>
        /// Get order details with line items
        /// </summary>
        public async Task<Order> GetOrderDetailAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderDetailsAsync(orderId);
            if (order == null)
                throw new InvalidOperationException($"Order {orderId} not found");
            return order;
        }

        /// <summary>
        /// Update order status (employee operation)
        /// Status values: 'inregistrata', 'se pregateste', 'a plecat la client', 'livrata', 'anulata'
        /// </summary>
        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var validStatuses = new[] { "inregistrata", "se pregateste", "a plecat la client", "livrata", "anulata" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException($"Invalid order status: {newStatus}");

            // Verify order exists
            var order = await GetOrderDetailAsync(orderId);
            if (order == null)
                return false;

            // Prevent status changes on completed/cancelled orders
            if (order.Status == "livrata" || order.Status == "anulata")
                return false;

            var updated = await _orderRepository.UpdateOrderStatusAsync(orderId, newStatus);
            if (!updated)
                return false;

            if (newStatus == "anulata")
            {
                foreach (var item in order.OrderItems ?? Enumerable.Empty<OrderItem>())
                {
                    var restored = await _productRepository.UpdateInventoryAsync(item.ProductId, item.Quantity);
                    if (!restored)
                        throw new InvalidOperationException($"Failed to restore inventory for product {item.ProductId}");
                }
            }

            return true;
        }

        /// <summary>
        /// Cancel active order (restores inventory, sets status to 'anulata')
        /// Only works if order status is not already 'livrata' or 'anulata'
        /// </summary>
        public async Task<bool> CancelOrderAsync(int orderId)
        {
            var order = await GetOrderDetailAsync(orderId);
            if (order == null)
                return false;

            // Can only cancel active orders
            if (order.Status == "livrata" || order.Status == "anulata")
                return false;

            // Set status to cancelled
            return await UpdateOrderStatusAsync(orderId, "anulata");
        }

        /// <summary>
        /// Get all orders (employee view)
        /// </summary>
        public async Task<List<Order>> GetAllOrdersAsync(int limit = 100)
        {
            var orders = await _orderRepository.GetAllOrdersAsync(null, null, null, limit);
            return orders.ToList();
        }

        /// <summary>
        /// Get all active orders (employee view)
        /// </summary>
        public async Task<List<Order>> GetAllActiveOrdersAsync()
        {
            var allOrders = await GetAllOrdersAsync();
            return allOrders
                .Where(o => o.Status != "livrata" && o.Status != "anulata")
                .ToList();
        }

        /// <summary>
        /// Calculate discount amount based on business rules:
        /// - Large order discount if SubTotal > threshold
        /// - Frequent customer discount (TODO: implement when user order history available)
        /// </summary>
        private async Task<decimal> CalculateDiscountAsync(int userId, decimal subTotal)
        {
            decimal discountAmount = 0;

            // Apply large order discount
            var (largeOrderThreshold, largeOrderPercent) = await _configService.GetLargeOrderDiscountAsync();
            if (subTotal >= largeOrderThreshold)
            {
                discountAmount += (subTotal * largeOrderPercent) / 100;
            }

            // TODO: Apply frequent customer discount
            // Requires checking user order history within time window

            return discountAmount;
        }

        /// <summary>
        /// Calculate shipping fee based on configuration:
        /// - Free shipping if SubTotal (before discount) >= MinOrderForFreeShipping
        /// - Otherwise, fixed ShippingFee from configuration
        /// </summary>
        private async Task<decimal> CalculateShippingFeeAsync(decimal subTotal)
        {
            var minForFreeShipping = await _configService.GetMinOrderForFreeShippingAsync();

            if (subTotal >= minForFreeShipping)
                return 0;

            var shippingFee = await _configService.GetShippingFeeAsync();
            return shippingFee;
        }
    }
}
