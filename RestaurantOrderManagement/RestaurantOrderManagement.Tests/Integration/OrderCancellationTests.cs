using Moq;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Implementations;
using RestaurantOrderManagement.Services.Interfaces;
using Xunit;

namespace RestaurantOrderManagement.Tests.Integration
{
    /// <summary>
    /// Integration tests for order cancellation workflow
    /// Verifies inventory restoration, status updates, and constraint validation
    /// </summary>
    public class OrderCancellationTests
    {
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IConfigurationService> _configurationMock;
        private readonly OrderService _orderService;

        public OrderCancellationTests()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _productRepositoryMock = new Mock<IProductRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _configurationMock = new Mock<IConfigurationService>();

            // Setup configuration defaults
            _configurationMock.Setup(c => c.GetShippingFeeAsync()).ReturnsAsync(25.00m);
            _configurationMock.Setup(c => c.GetMinimumOrderValueAsync()).ReturnsAsync(50.00m);
            _configurationMock.Setup(c => c.GetMaximumOrderValueAsync()).ReturnsAsync(5000.00m);
            _configurationMock.Setup(c => c.GetDiscountPercentageAsync()).ReturnsAsync(0.10m);

            _orderService = new OrderService(
                _orderRepositoryMock.Object,
                _productRepositoryMock.Object,
                _userRepositoryMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task CancelActiveOrder_Successful_RestoresInventory()
        {
            // Arrange
            int productId1 = 1, productId2 = 2;
            int quantity1 = 100, quantity2 = 50; // quantities in grams

            var order = new Order
            {
                OrderId = 1,
                OrderCode = "ORD-001",
                UserId = 1,
                Status = "inregistrata",
                SubTotal = 150.00m,
                ShippingFee = 25.00m,
                TotalCost = 175.00m,
                OrderDate = DateTime.Now,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { OrderId = 1, ProductId = productId1, Quantity = quantity1, UnitPrice = 10.00m },
                    new OrderItem { OrderId = 1, ProductId = productId2, Quantity = quantity2, UnitPrice = 8.00m }
                }
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(1))
                .ReturnsAsync(order);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(1, "anulata"))
                .ReturnsAsync(true);

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _orderService.CancelOrderAsync(1);

            // Assert
            Assert.True(result);
            
            // Verify inventory was restored for both items
            _productRepositoryMock.Verify(
                r => r.UpdateInventoryAsync(productId1, quantity1),
                Times.Once,
                $"Inventory for product {productId1} should be restored by {quantity1}g"
            );

            _productRepositoryMock.Verify(
                r => r.UpdateInventoryAsync(productId2, quantity2),
                Times.Once,
                $"Inventory for product {productId2} should be restored by {quantity2}g"
            );

            _orderRepositoryMock.Verify(
                r => r.UpdateOrderStatusAsync(1, "anulata"),
                Times.Once
            );
        }

        [Fact]
        public async Task CancelActiveOrder_UpdatesStatusToAnulata()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 2,
                OrderCode = "ORD-002",
                Status = "se pregateste",
                OrderItems = new List<OrderItem> { }
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(2))
                .ReturnsAsync(order);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(2, "anulata"))
                .ReturnsAsync(true);

            // Act
            var result = await _orderService.CancelOrderAsync(2);

            // Assert
            Assert.True(result);
            _orderRepositoryMock.Verify(
                r => r.UpdateOrderStatusAsync(2, "anulata"),
                Times.Once,
                "Order status must be updated to 'anulata'"
            );
        }

        [Fact]
        public async Task CancelDeliveredOrder_ReturnsFalse()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 3,
                OrderCode = "ORD-003",
                Status = "livrata", // Already delivered
                OrderItems = new List<OrderItem> { }
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(3))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.CancelOrderAsync(3);

            // Assert
            Assert.False(result);
            _orderRepositoryMock.Verify(
                r => r.UpdateOrderStatusAsync(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never,
                "Should not update status for already delivered orders"
            );
        }

        [Fact]
        public async Task CancelAlreadyCancelledOrder_ReturnsFalse()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 4,
                OrderCode = "ORD-004",
                Status = "anulata", // Already cancelled
                OrderItems = new List<OrderItem> { }
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(4))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.CancelOrderAsync(4);

            // Assert
            Assert.False(result);
            _orderRepositoryMock.Verify(
                r => r.UpdateOrderStatusAsync(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never,
                "Should not attempt to cancel already cancelled orders"
            );
        }

        [Fact]
        public async Task CancelOrder_RestoresAllItems()
        {
            // Arrange: Order with 3 items
            var order = new Order
            {
                OrderId = 5,
                OrderCode = "ORD-005",
                Status = "inregistrata",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = 10, Quantity = 200 },
                    new OrderItem { ProductId = 11, Quantity = 150 },
                    new OrderItem { ProductId = 12, Quantity = 75 }
                }
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(5))
                .ReturnsAsync(order);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(5, "anulata"))
                .ReturnsAsync(true);

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _orderService.CancelOrderAsync(5);

            // Assert
            Assert.True(result);
            
            // Verify all items restored
            _productRepositoryMock.Verify(
                r => r.UpdateInventoryAsync(10, 200),
                Times.Once
            );
            _productRepositoryMock.Verify(
                r => r.UpdateInventoryAsync(11, 150),
                Times.Once
            );
            _productRepositoryMock.Verify(
                r => r.UpdateInventoryAsync(12, 75),
                Times.Once
            );
        }

        [Fact]
        public async Task CancelOrder_InventoryFailure_ThrowsException()
        {
            // Arrange: Inventory restoration fails
            var order = new Order
            {
                OrderId = 6,
                OrderCode = "ORD-006",
                Status = "inregistrata",
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = 20, Quantity = 100 }
                }
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(6))
                .ReturnsAsync(order);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(6, "anulata"))
                .ReturnsAsync(true);

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(20, 100))
                .ReturnsAsync(false); // Inventory restoration failed

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CancelOrderAsync(6)
            );

            Assert.Contains("Failed to restore inventory", ex.Message);
        }

        [Fact]
        public async Task OrderCreation_IncrementsProductInventory()
        {
            // Arrange
            var user = new User { UserId = 1, Email = "test@example.com" };
            var product = new Product { ProductId = 30, Name = "Test Product", UnitPrice = 12.50m };

            _userRepositoryMock
                .Setup(r => r.GetUserByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(30))
                .ReturnsAsync(product);

            _orderRepositoryMock
                .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync(new Order { OrderId = 100, OrderCode = "ORD-100" });

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            var orderToCreate = new Order
            {
                UserId = 1,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = 30, Quantity = 250 } // 250g
                },
                DeliveryAddress = "123 Main St"
            };

            // Act
            var createdOrder = await _orderService.CreateOrderAsync(orderToCreate, "test@example.com");

            // Assert
            Assert.NotNull(createdOrder);
            _productRepositoryMock.Verify(
                r => r.UpdateInventoryAsync(30, -250),
                Times.Once,
                "Inventory should be decremented by 250 on order creation"
            );
        }

        [Fact]
        public async Task OrderCreation_InventoryFailure_ThrowsException()
        {
            // Arrange: Inventory decrement fails during order creation
            var user = new User { UserId = 1, Email = "test@example.com" };
            var product = new Product { ProductId = 40, Name = "Low Stock Item", UnitPrice = 5.00m };

            _userRepositoryMock
                .Setup(r => r.GetUserByEmailAsync("test@example.com"))
                .ReturnsAsync(user);

            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(40))
                .ReturnsAsync(product);

            _orderRepositoryMock
                .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync(new Order { OrderId = 101, OrderCode = "ORD-101" });

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(40, -500))
                .ReturnsAsync(false); // Insufficient inventory

            var orderToCreate = new Order
            {
                UserId = 1,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = 40, Quantity = 500 }
                },
                DeliveryAddress = "123 Main St"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateOrderAsync(orderToCreate, "test@example.com")
            );

            Assert.Contains("Failed to update inventory", ex.Message);
        }
    }
}
