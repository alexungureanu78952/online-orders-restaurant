using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Interfaces;
using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Services.Implementations
{

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

        public async Task<Order> CreateOrderAsync(int userId, List<(int ProductId, int Quantity)> items, string deliveryAddress)
        {
            return await CreateOrderAsync(
                userId,
                items.Select(item => OrderRequestItem.Product(item.ProductId, item.Quantity)).ToList(),
                deliveryAddress);
        }

        public async Task<Order> CreateOrderAsync(int userId, List<OrderRequestItem> items, string deliveryAddress)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Order must contain at least one item");

            if (string.IsNullOrWhiteSpace(deliveryAddress))
                throw new ArgumentException("Delivery address is required for order creation");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            var orderItems = new List<OrderItem>();
            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException("Each order item quantity must be greater than 0");

                orderItems.Add(await BuildOrderItemAsync(item));
            }

            decimal subTotal = orderItems.Sum(item => item.ItemTotal);

            var pricing = await CalculatePricingAsync(userId, subTotal);


            var (orderId, orderCode) = await _orderRepository.CreateOrderAsync(userId, pricing.SubTotal, pricing.ShippingFee,
                pricing.DiscountAmount, deliveryAddress, null);

            await _orderRepository.AddOrderItemsAsync(orderId, orderItems);

            var createdOrder = await _orderRepository.GetOrderDetailsAsync(orderId);
            return createdOrder ?? throw new InvalidOperationException($"Order {orderCode} was created but could not be loaded");
        }

        public async Task<OrderPricingQuote> CalculatePricingAsync(int userId, decimal subTotal)
        {
            if (subTotal < 0)
                throw new ArgumentException("Subtotal cannot be negative");

            var discountAmount = await CalculateDiscountAsync(userId, subTotal);
            var shippingFee = await CalculateShippingFeeAsync(subTotal);
            var totalCost = Math.Max(0, subTotal + shippingFee - discountAmount);

            return new OrderPricingQuote
            {
                SubTotal = Math.Round(subTotal, 2),
                ShippingFee = Math.Round(shippingFee, 2),
                DiscountAmount = Math.Round(discountAmount, 2),
                TotalCost = Math.Round(totalCost, 2)
            };
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId, int limit = 50)
        {
            var orders = await _orderRepository.GetUserOrdersAsync(userId, limit);
            return orders.ToList();
        }

        public async Task<List<Order>> GetUserActiveOrdersAsync(int userId)
        {
            var allOrders = await GetUserOrdersAsync(userId);
            return allOrders
                .Where(o => o.Status != "livrata" && o.Status != "anulata")
                .ToList();
        }


        public async Task<Order> GetOrderDetailAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderDetailsAsync(orderId);
            if (order == null)
                throw new InvalidOperationException($"Order {orderId} not found");
            return order;
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var validStatuses = new[] { "inregistrata", "se pregateste", "a plecat la client", "livrata", "anulata" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException($"Invalid order status: {newStatus}");

            var order = await GetOrderDetailAsync(orderId);
            if (order == null)
                return false;

            
            if (order.Status == "livrata" || order.Status == "anulata")
                return false;

            var updated = await _orderRepository.UpdateOrderStatusAsync(orderId, newStatus);
            if (!updated)
                return false;

            if (newStatus == "livrata")
            {
                await ApplyDeliveredInventoryAsync(order);
            }

            return true;
        }


        public async Task<bool> CancelOrderAsync(int orderId)
        {
            var order = await GetOrderDetailAsync(orderId);
            if (order == null)
                return false;

            
            if (order.Status == "livrata" || order.Status == "anulata")
                return false;

           
            return await UpdateOrderStatusAsync(orderId, "anulata");
        }

        
        public async Task<List<Order>> GetAllOrdersAsync(int limit = 100)
        {
            var orders = await _orderRepository.GetAllOrdersAsync(null, null, null, limit);
            return orders.ToList();
        }

        public async Task<List<Order>> GetAllActiveOrdersAsync()
        {
            var allOrders = await GetAllOrdersAsync();
            return allOrders
                .Where(o => o.Status != "livrata" && o.Status != "anulata")
                .ToList();
        }


        private async Task<decimal> CalculateDiscountAsync(int userId, decimal subTotal)
        {
            decimal discountAmount = 0;

            
            var (largeOrderThreshold, largeOrderPercent) = await _configService.GetLargeOrderDiscountAsync();
            if (largeOrderThreshold > 0 && largeOrderPercent > 0 && subTotal >= largeOrderThreshold)
            {
                discountAmount += Math.Round((subTotal * largeOrderPercent) / 100, 2);
            }

            var (frequentOrderThreshold, frequentOrderWindow, frequentOrderPercent) =
                await _configService.GetFrequentOrderDiscountAsync();
            if (userId > 0 &&
                frequentOrderThreshold > 0 &&
                frequentOrderWindow > 0 &&
                frequentOrderPercent > 0)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-frequentOrderWindow);
                var recentOrderCount = (await _orderRepository.GetUserOrdersAsync(userId, 1000))
                    .Count(order => order.Status != "anulata" && order.OrderDate >= cutoffDate);

                if (recentOrderCount > frequentOrderThreshold)
                {
                    discountAmount += Math.Round((subTotal * frequentOrderPercent) / 100, 2);
                }
            }

            return Math.Min(discountAmount, subTotal);
        }

        
        private async Task<decimal> CalculateShippingFeeAsync(decimal subTotal)
        {
            var minForFreeShipping = await _configService.GetMinOrderForFreeShippingAsync();

            if (subTotal >= minForFreeShipping)
                return 0;

            var shippingFee = await _configService.GetShippingFeeAsync();
            return shippingFee;
        }

        private async Task<OrderItem> BuildOrderItemAsync(OrderRequestItem item)
        {
            if (IsMenuItem(item))
                return await BuildMenuOrderItemAsync(item);

            if (item.ProductId is not int productId)
                throw new ArgumentException("Product order item is missing a product reference");

            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null)
                throw new InvalidOperationException($"Product {productId} not found");

            EnsureProductCanBeOrdered(product, item.Quantity);

            return new OrderItem
            {
                ProductId = product.ProductId,
                ItemType = "Preparat",
                ItemName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                ItemTotal = product.Price * item.Quantity,
                CreatedDate = DateTime.UtcNow
            };
        }

        private async Task<OrderItem> BuildMenuOrderItemAsync(OrderRequestItem item)
        {
            if (item.MenuId is not int menuId)
                throw new ArgumentException("Menu order item is missing a menu reference");

            var menu = await _productRepository.GetMenuByIdAsync(menuId);
            if (menu == null)
                throw new InvalidOperationException($"Menu {menuId} not found");
            if (!menu.IsAvailable)
                throw new InvalidOperationException($"Menu {menu.Name} is not available");
            if (menu.MenuProducts == null || menu.MenuProducts.Count == 0)
                throw new InvalidOperationException($"Menu {menu.Name} has no products");

            foreach (var component in menu.MenuProducts)
            {
                EnsureProductCanBeOrdered(component.Product, item.Quantity * component.Quantity);
            }

            var unitPrice = await CalculateMenuPriceAsync(menu);
            return new OrderItem
            {
                MenuId = menu.MenuId,
                ItemType = "Meniu",
                ItemName = menu.Name,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                ItemTotal = unitPrice * item.Quantity,
                CreatedDate = DateTime.UtcNow
            };
        }

        private async Task<decimal> CalculateMenuPriceAsync(Menu menu)
        {
            var config = await _configService.GetConfigurationAsync();
            var discountPercent = menu.BundleDiscountPercent > 0
                ? menu.BundleDiscountPercent
                : config.MenuBundleDiscountPercent;
            var subtotal = menu.MenuProducts.Sum(component => component.Product.Price * component.Quantity);

            return Math.Round(subtotal * (1 - discountPercent / 100), 2);
        }

        private static void EnsureProductCanBeOrdered(Product product, int requestedPieces)
        {
            if (product == null)
                throw new InvalidOperationException("A menu component product was not found");
            if (!product.IsAvailable || product.IsDeleted || product.TotalQuantity <= 0)
                throw new InvalidOperationException($"Product {product.Name} is not available");

            var requiredQuantity = product.PortionQuantity * requestedPieces;
            if (product.TotalQuantity < requiredQuantity)
                throw new InvalidOperationException($"Insufficient inventory for {product.Name}");
        }

        private async Task ApplyDeliveredInventoryAsync(Order order)
        {
            foreach (var item in order.OrderItems ?? Enumerable.Empty<OrderItem>())
            {
                if (item.ProductId is int productId)
                {
                    var product = item.Product ?? await _productRepository.GetProductByIdAsync(productId);
                    if (product == null)
                        throw new InvalidOperationException($"Product {productId} not found");

                    var quantityChange = -(product.PortionQuantity * item.Quantity);
                    await _productRepository.UpdateInventoryAsync(productId, quantityChange);
                    continue;
                }

                if (item.MenuId is int menuId)
                {
                    var menu = item.Menu ?? await _productRepository.GetMenuByIdAsync(menuId);
                    if (menu == null)
                        throw new InvalidOperationException($"Menu {menuId} not found");

                    foreach (var component in menu.MenuProducts)
                    {
                        var quantityChange = -(component.Product.PortionQuantity * component.Quantity * item.Quantity);
                        await _productRepository.UpdateInventoryAsync(component.ProductId, quantityChange);
                    }
                }
            }
        }

        private static bool IsMenuItem(OrderRequestItem item)
        {
            return string.Equals(item.ItemType, "Meniu", StringComparison.OrdinalIgnoreCase) ||
                   item.MenuId.HasValue;
        }
    }
}
