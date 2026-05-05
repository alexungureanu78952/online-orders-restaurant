using Moq;
using Xunit;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Services.Interfaces;
using RestaurantOrderManagement.Services.Implementations;

namespace RestaurantOrderManagement.Tests.Services
{
    /// <summary>
    /// Comprehensive unit tests for OrderService
    /// Tests order creation, pricing calculations, discount/fee application, and status management
    /// All repository calls mocked to isolate business logic
    /// </summary>
    public class OrderServiceTests
    {
        private readonly Mock<OrderRepository> _mockOrderRepository;
        private readonly Mock<ProductRepository> _mockProductRepository;
        private readonly Mock<UserRepository> _mockUserRepository;
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _mockOrderRepository = new Mock<OrderRepository>();
            _mockProductRepository = new Mock<ProductRepository>();
            _mockUserRepository = new Mock<UserRepository>();
            _mockConfigService = new Mock<IConfigurationService>();

            _orderService = new OrderService(
                _mockOrderRepository.Object,
                _mockProductRepository.Object,
                _mockUserRepository.Object,
                _mockConfigService.Object
            );
        }

        #region Order Creation Tests

        [Fact]
        public async Task CreateOrderAsync_WithValidItems_CreatesOrderSuccessfully()
        {
            // Arrange
            int userId = 1;
            var items = new List<(int, int)> { (1, 2), (2, 1) };
            string deliveryAddress = "123 Main St";

            var user = new User { UserId = userId, Email = "test@test.com" };
            var product1 = new Product { ProductId = 1, Name = "Pizza", Price = 10.00m, IsAvailable = true };
            var product2 = new Product { ProductId = 2, Name = "Pasta", Price = 12.00m, IsAvailable = true };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product1);
            _mockProductRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(product2);

            _mockConfigService.Setup(c => c.GetLargeOrderDiscountAsync())
                .ReturnsAsync((200m, 10m)); // 10% discount at $200
            _mockConfigService.Setup(c => c.GetMinOrderForFreeShippingAsync())
                .ReturnsAsync(50m); // Free shipping above $50
            _mockConfigService.Setup(c => c.GetShippingFeeAsync())
                .ReturnsAsync(15m);

            _mockOrderRepository.Setup(r => r.CreateOrderAsync(userId, 32m, 0m, 0m, deliveryAddress, null))
                .ReturnsAsync((101, "ORD-20260505-00001"));

            var order = new Order
            {
                OrderId = 101,
                OrderCode = "ORD-20260505-00001",
                UserId = userId,
                SubTotal = 32m,
                ShippingFee = 0m,
                DiscountAmount = 0m,
                TotalCost = 32m,
                Status = "inregistrata"
            };
            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(101)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CreateOrderAsync(userId, items, deliveryAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(101, result.OrderId);
            Assert.Equal("ORD-20260505-00001", result.OrderCode);
            Assert.Equal("inregistrata", result.Status);
        }

        [Fact]
        public async Task CreateOrderAsync_WithEmptyItems_ThrowsArgumentException()
        {
            // Arrange
            int userId = 1;
            var items = new List<(int, int)>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _orderService.CreateOrderAsync(userId, items, "123 Main St"));
        }

        [Fact]
        public async Task CreateOrderAsync_WithoutDeliveryAddress_ThrowsArgumentException()
        {
            // Arrange
            int userId = 1;
            var items = new List<(int, int)> { (1, 2) };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _orderService.CreateOrderAsync(userId, items, null));
        }

        [Fact]
        public async Task CreateOrderAsync_NonexistentUser_ThrowsInvalidOperationException()
        {
            // Arrange
            int userId = 999;
            var items = new List<(int, int)> { (1, 2) };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateOrderAsync(userId, items, "123 Main St"));
        }

        [Fact]
        public async Task CreateOrderAsync_NonexistentProduct_ThrowsInvalidOperationException()
        {
            // Arrange
            int userId = 1;
            var items = new List<(int, int)> { (999, 2) };

            var user = new User { UserId = userId, Email = "test@test.com" };
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockProductRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateOrderAsync(userId, items, "123 Main St"));
        }

        [Fact]
        public async Task CreateOrderAsync_UnavailableProduct_ThrowsInvalidOperationException()
        {
            // Arrange
            int userId = 1;
            var items = new List<(int, int)> { (1, 2) };

            var user = new User { UserId = userId, Email = "test@test.com" };
            var unavailableProduct = new Product 
            { 
                ProductId = 1, 
                Name = "Out of Stock Pizza", 
                Price = 10.00m, 
                IsAvailable = false 
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(unavailableProduct);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateOrderAsync(userId, items, "123 Main St"));
        }

        #endregion

        #region Discount and Fee Tests

        [Fact]
        public async Task CreateOrderAsync_LargeOrder_AppliesDiscount()
        {
            // Arrange - order with subtotal >= threshold to trigger discount
            int userId = 1;
            var items = new List<(int, int)> { (1, 10) }; // 10 * $25 = $250 subtotal
            string deliveryAddress = "123 Main St";

            var user = new User { UserId = userId, Email = "test@test.com" };
            var product = new Product { ProductId = 1, Name = "Premium Pizza", Price = 25.00m, IsAvailable = true };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

            // Large order discount: 10% above $200
            _mockConfigService.Setup(c => c.GetLargeOrderDiscountAsync())
                .ReturnsAsync((200m, 10m));
            _mockConfigService.Setup(c => c.GetMinOrderForFreeShippingAsync())
                .ReturnsAsync(200m); // Free shipping above $200
            _mockConfigService.Setup(c => c.GetShippingFeeAsync())
                .ReturnsAsync(15m);

            // Expect discount to be calculated (250 * 10% = 25)
            _mockOrderRepository.Setup(r => r.CreateOrderAsync(userId, 250m, 0m, 25m, deliveryAddress, null))
                .ReturnsAsync((101, "ORD-20260505-00001"));

            var order = new Order
            {
                OrderId = 101,
                SubTotal = 250m,
                ShippingFee = 0m,
                DiscountAmount = 25m,
                TotalCost = 225m
            };
            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(101)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CreateOrderAsync(userId, items, deliveryAddress);

            // Assert
            _mockOrderRepository.Verify(
                r => r.CreateOrderAsync(userId, 250m, It.IsAny<decimal>(), It.IsInRange(20m, 30m, Moq.Range.Inclusive), deliveryAddress, null),
                Times.Once);
        }

        [Fact]
        public async Task CreateOrderAsync_BelowFreeShippingThreshold_ChargesShippingFee()
        {
            // Arrange - subtotal below threshold
            int userId = 1;
            var items = new List<(int, int)> { (1, 1) }; // $20
            string deliveryAddress = "123 Main St";

            var user = new User { UserId = userId, Email = "test@test.com" };
            var product = new Product { ProductId = 1, Name = "Small Dish", Price = 20.00m, IsAvailable = true };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

            _mockConfigService.Setup(c => c.GetLargeOrderDiscountAsync())
                .ReturnsAsync((200m, 10m));
            _mockConfigService.Setup(c => c.GetMinOrderForFreeShippingAsync())
                .ReturnsAsync(50m); // Free shipping only above $50
            _mockConfigService.Setup(c => c.GetShippingFeeAsync())
                .ReturnsAsync(15m);

            _mockOrderRepository.Setup(r => r.CreateOrderAsync(userId, 20m, 15m, 0m, deliveryAddress, null))
                .ReturnsAsync((101, "ORD-20260505-00001"));

            var order = new Order
            {
                OrderId = 101,
                SubTotal = 20m,
                ShippingFee = 15m,
                DiscountAmount = 0m,
                TotalCost = 35m
            };
            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(101)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CreateOrderAsync(userId, items, deliveryAddress);

            // Assert
            _mockOrderRepository.Verify(
                r => r.CreateOrderAsync(userId, 20m, 15m, 0m, deliveryAddress, null),
                Times.Once);
        }

        [Fact]
        public async Task CreateOrderAsync_AboveFreeShippingThreshold_FreeShipping()
        {
            // Arrange - subtotal above threshold
            int userId = 1;
            var items = new List<(int, int)> { (1, 3) }; // 3 * $20 = $60
            string deliveryAddress = "123 Main St";

            var user = new User { UserId = userId, Email = "test@test.com" };
            var product = new Product { ProductId = 1, Name = "Medium Dish", Price = 20.00m, IsAvailable = true };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

            _mockConfigService.Setup(c => c.GetLargeOrderDiscountAsync())
                .ReturnsAsync((200m, 10m));
            _mockConfigService.Setup(c => c.GetMinOrderForFreeShippingAsync())
                .ReturnsAsync(50m); // Free shipping above $50
            _mockConfigService.Setup(c => c.GetShippingFeeAsync())
                .ReturnsAsync(15m);

            _mockOrderRepository.Setup(r => r.CreateOrderAsync(userId, 60m, 0m, 0m, deliveryAddress, null))
                .ReturnsAsync((101, "ORD-20260505-00001"));

            var order = new Order
            {
                OrderId = 101,
                SubTotal = 60m,
                ShippingFee = 0m,
                DiscountAmount = 0m,
                TotalCost = 60m
            };
            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(101)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CreateOrderAsync(userId, items, deliveryAddress);

            // Assert
            _mockOrderRepository.Verify(
                r => r.CreateOrderAsync(userId, 60m, 0m, 0m, deliveryAddress, null),
                Times.Once);
        }

        #endregion

        #region Order Retrieval Tests

        [Fact]
        public async Task GetUserOrdersAsync_ReturnsUserOrders()
        {
            // Arrange
            int userId = 1;
            var orders = new List<Order>
            {
                new Order { OrderId = 1, OrderCode = "ORD-001", UserId = userId, Status = "livrata" },
                new Order { OrderId = 2, OrderCode = "ORD-002", UserId = userId, Status = "inregistrata" }
            };

            _mockOrderRepository.Setup(r => r.GetUserOrdersAsync(userId, 50))
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetUserOrdersAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, o => o.OrderCode == "ORD-001");
        }

        [Fact]
        public async Task GetUserActiveOrdersAsync_FiltersOutCompletedAndCancelledOrders()
        {
            // Arrange
            int userId = 1;
            var allOrders = new List<Order>
            {
                new Order { OrderId = 1, OrderCode = "ORD-001", Status = "inregistrata" },
                new Order { OrderId = 2, OrderCode = "ORD-002", Status = "se pregateste" },
                new Order { OrderId = 3, OrderCode = "ORD-003", Status = "livrata" }, // Exclude
                new Order { OrderId = 4, OrderCode = "ORD-004", Status = "anulata" }  // Exclude
            };

            _mockOrderRepository.Setup(r => r.GetUserOrdersAsync(userId, 50))
                .ReturnsAsync(allOrders);

            // Act
            var result = await _orderService.GetUserActiveOrdersAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, o => o.Status == "livrata");
            Assert.DoesNotContain(result, o => o.Status == "anulata");
        }

        [Fact]
        public async Task GetOrderDetailAsync_ReturnsOrderWithLineItems()
        {
            // Arrange
            int orderId = 1;
            var order = new Order
            {
                OrderId = orderId,
                OrderCode = "ORD-001",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { OrderItemId = 1, ProductId = 1, Quantity = 2 },
                    new OrderItem { OrderItemId = 2, ProductId = 2, Quantity = 1 }
                }
            };

            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.OrderItems.Count);
        }

        #endregion

        #region Status Update Tests

        [Fact]
        public async Task UpdateOrderStatusAsync_WithValidStatus_UpdatesSuccessfully()
        {
            // Arrange
            int orderId = 1;
            string newStatus = "se pregateste";
            var order = new Order { OrderId = orderId, Status = "inregistrata" };

            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(orderId)).ReturnsAsync(order);
            _mockOrderRepository.Setup(r => r.UpdateOrderStatusAsync(orderId, newStatus)).ReturnsAsync(true);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, newStatus);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_WithInvalidStatus_ThrowsArgumentException()
        {
            // Arrange
            int orderId = 1;
            string invalidStatus = "invalid_status";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _orderService.UpdateOrderStatusAsync(orderId, invalidStatus));
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_DeliveredOrder_ReturnsFalse()
        {
            // Arrange
            int orderId = 1;
            var order = new Order { OrderId = orderId, Status = "livrata" };

            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, "se pregateste");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CancelOrderAsync_ActiveOrder_CancelsSuccessfully()
        {
            // Arrange
            int orderId = 1;
            var order = new Order { OrderId = orderId, Status = "inregistrata" };

            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(orderId)).ReturnsAsync(order);
            _mockOrderRepository.Setup(r => r.UpdateOrderStatusAsync(orderId, "anulata")).ReturnsAsync(true);

            // Act
            var result = await _orderService.CancelOrderAsync(orderId);

            // Assert
            Assert.True(result);
            _mockOrderRepository.Verify(r => r.UpdateOrderStatusAsync(orderId, "anulata"), Times.Once);
        }

        [Fact]
        public async Task CancelOrderAsync_DeliveredOrder_ReturnsFalse()
        {
            // Arrange
            int orderId = 1;
            var order = new Order { OrderId = orderId, Status = "livrata" };

            _mockOrderRepository.Setup(r => r.GetOrderDetailsAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CancelOrderAsync(orderId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Employee View Tests

        [Fact]
        public async Task GetAllOrdersAsync_ReturnsAllOrders()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order { OrderId = 1, OrderCode = "ORD-001" },
                new Order { OrderId = 2, OrderCode = "ORD-002" }
            };

            _mockOrderRepository.Setup(r => r.GetAllOrdersAsync(null, null, null, 100))
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetAllOrdersAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllActiveOrdersAsync_FiltersOutCompletedAndCancelled()
        {
            // Arrange
            var allOrders = new List<Order>
            {
                new Order { OrderId = 1, OrderCode = "ORD-001", Status = "se pregateste" },
                new Order { OrderId = 2, OrderCode = "ORD-002", Status = "livrata" },
                new Order { OrderId = 3, OrderCode = "ORD-003", Status = "anulata" }
            };

            _mockOrderRepository.Setup(r => r.GetAllOrdersAsync(null, null, null, 100))
                .ReturnsAsync(allOrders);

            // Act
            var result = await _orderService.GetAllActiveOrdersAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("se pregateste", result[0].Status);
        }

        #endregion
    }
}
