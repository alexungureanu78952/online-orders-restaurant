using Moq;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Implementations;
using RestaurantOrderManagement.Services.Interfaces;
using Xunit;

namespace RestaurantOrderManagement.Tests.Integration
{
    /// <summary>
    /// Integration tests for employee order management workflows
    /// Verifies employee-only access, status updates, and client visibility
    /// </summary>
    public class EmployeeFlowTests
    {
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IConfigurationService> _configurationMock;
        private readonly OrderService _orderService;
        private readonly AuthenticationService _authService;

        public EmployeeFlowTests()
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

            _authService = new AuthenticationService(_userRepositoryMock.Object);
        }

        [Fact]
        public void IsEmployeeRole_WithEmployeeRole_ReturnsTrue()
        {
            // Act
            var result = _authService.IsEmployeeRole("Employee");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsEmployeeRole_WithAdminRole_ReturnsTrue()
        {
            // Act
            var result = _authService.IsEmployeeRole("Admin");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsEmployeeRole_WithClientRole_ReturnsFalse()
        {
            // Act
            var result = _authService.IsEmployeeRole("Client");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsClientRole_WithClientRole_ReturnsTrue()
        {
            // Act
            var result = _authService.IsClientRole("Client");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsClientRole_WithEmployeeRole_ReturnsFalse()
        {
            // Act
            var result = _authService.IsClientRole("Employee");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EmployeeCanViewAllOrders()
        {
            // Arrange: Setup multiple orders from different customers
            var orders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    OrderCode = "ORD-001",
                    UserId = 1,
                    Status = "inregistrata",
                    TotalCost = 100.00m,
                    OrderDate = DateTime.Now
                },
                new Order
                {
                    OrderId = 2,
                    OrderCode = "ORD-002",
                    UserId = 2,
                    Status = "se pregateste",
                    TotalCost = 150.00m,
                    OrderDate = DateTime.Now.AddHours(-1)
                }
            };

            _orderRepositoryMock
                .Setup(r => r.GetAllOrdersAsync(null, null, null, 500))
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetAllOrdersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _orderRepositoryMock.Verify(
                r => r.GetAllOrdersAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()),
                Times.Once
            );
        }

        [Fact]
        public async Task EmployeeCanFilterActiveOrders()
        {
            // Arrange: Setup orders with different statuses
            var allOrders = new List<Order>
            {
                new Order { OrderId = 1, OrderCode = "ORD-001", Status = "inregistrata", OrderDate = DateTime.Now },
                new Order { OrderId = 2, OrderCode = "ORD-002", Status = "livrata", OrderDate = DateTime.Now },
                new Order { OrderId = 3, OrderCode = "ORD-003", Status = "se pregateste", OrderDate = DateTime.Now }
            };

            _orderRepositoryMock
                .Setup(r => r.GetAllOrdersAsync(null, null, null, 500))
                .ReturnsAsync(allOrders);

            // Act
            var result = await _orderService.GetAllOrdersAsync();
            var activeOrders = result.Where(o => o.Status != "livrata" && o.Status != "anulata");

            // Assert
            Assert.Equal(2, activeOrders.Count());
            Assert.True(activeOrders.All(o => o.Status != "livrata"));
        }

        [Fact]
        public async Task EmployeeCanUpdateOrderStatus_FromRegisteredToPreparation()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 1,
                OrderCode = "ORD-001",
                Status = "inregistrata",
                OrderItems = new List<OrderItem>()
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(1))
                .ReturnsAsync(order);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(1, "se pregateste"))
                .ReturnsAsync(true);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(1, "se pregateste");

            // Assert
            Assert.True(result);
            _orderRepositoryMock.Verify(
                r => r.UpdateOrderStatusAsync(1, "se pregateste"),
                Times.Once
            );
        }

        [Fact]
        public async Task EmployeeCanUpdateOrderStatus_FromPreparationToInTransit()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 2,
                OrderCode = "ORD-002",
                Status = "se pregateste",
                OrderItems = new List<OrderItem>()
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(2))
                .ReturnsAsync(order);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(2, "a plecat la client"))
                .ReturnsAsync(true);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(2, "a plecat la client");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task EmployeeCanUpdateOrderStatus_FromInTransitToDelivered()
        {
            // Arrange
            var order = new Order
            {
                OrderId = 3,
                OrderCode = "ORD-003",
                Status = "a plecat la client",
                OrderItems = new List<OrderItem>()
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(3))
                .ReturnsAsync(order);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(3, "livrata"))
                .ReturnsAsync(true);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(3, "livrata");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ClientCanViewUpdatedOrderStatus()
        {
            // Arrange: Simulate order update and client retrieval
            var originalOrder = new Order
            {
                OrderId = 1,
                OrderCode = "ORD-001",
                UserId = 1,
                Status = "inregistrata",
                OrderItems = new List<OrderItem>()
            };

            var updatedOrder = new Order
            {
                OrderId = 1,
                OrderCode = "ORD-001",
                UserId = 1,
                Status = "se pregateste", // Status updated by employee
                OrderItems = new List<OrderItem>()
            };

            // First call returns original, simulate employee update, then updated version
            _orderRepositoryMock
                .SetupSequence(r => r.GetOrderDetailsAsync(1))
                .ReturnsAsync(originalOrder)
                .ReturnsAsync(updatedOrder);

            // Act
            var initialStatus = await _orderService.GetOrderDetailsAsync(1);
            var finalStatus = await _orderService.GetOrderDetailsAsync(1);

            // Assert
            Assert.Equal("inregistrata", initialStatus.Status);
            Assert.Equal("se pregateste", finalStatus.Status);
        }

        [Fact]
        public async Task EmployeeCanBulkViewOrders_WithFiltering()
        {
            // Arrange: Orders with mixed statuses
            var orders = new List<Order>
            {
                new Order { OrderId = 1, OrderCode = "ORD-001", Status = "inregistrata", TotalCost = 100.00m, OrderDate = DateTime.Now.AddHours(-5) },
                new Order { OrderId = 2, OrderCode = "ORD-002", Status = "se pregateste", TotalCost = 150.00m, OrderDate = DateTime.Now.AddHours(-3) },
                new Order { OrderId = 3, OrderCode = "ORD-003", Status = "a plecat la client", TotalCost = 120.00m, OrderDate = DateTime.Now.AddHours(-1) },
                new Order { OrderId = 4, OrderCode = "ORD-004", Status = "livrata", TotalCost = 200.00m, OrderDate = DateTime.Now.AddHours(-24) }
            };

            _orderRepositoryMock
                .Setup(r => r.GetAllOrdersAsync(null, null, null, 500))
                .ReturnsAsync(orders);

            // Act
            var allOrders = await _orderService.GetAllOrdersAsync();
            var activeOrders = allOrders.Where(o => o.Status != "livrata" && o.Status != "anulata").ToList();
            var deliveredOrders = allOrders.Where(o => o.Status == "livrata").ToList();

            // Assert
            Assert.Equal(4, allOrders.Count());
            Assert.Equal(3, activeOrders.Count);
            Assert.Equal(1, deliveredOrders.Count);
        }

        [Fact]
        public async Task EmployeeStatusUpdate_DoesNotAffectOtherOrders()
        {
            // Arrange: Multiple orders, updating one
            var order1 = new Order { OrderId = 1, OrderCode = "ORD-001", Status = "inregistrata", OrderItems = new List<OrderItem>() };
            var order2 = new Order { OrderId = 2, OrderCode = "ORD-002", Status = "inregistrata", OrderItems = new List<OrderItem>() };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(1))
                .ReturnsAsync(order1);
            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(2))
                .ReturnsAsync(order2);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(1, "se pregateste"))
                .ReturnsAsync(true);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(1, "se pregateste");

            // Assert
            Assert.True(result);
            // Verify only order 1 was updated
            _orderRepositoryMock.Verify(r => r.UpdateOrderStatusAsync(1, "se pregateste"), Times.Once);
            _orderRepositoryMock.Verify(r => r.UpdateOrderStatusAsync(2, It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EmployeeCannotUpdateDeliveredOrder()
        {
            // Arrange: Already delivered order
            var order = new Order
            {
                OrderId = 10,
                OrderCode = "ORD-010",
                Status = "livrata",
                OrderItems = new List<OrderItem>()
            };

            _orderRepositoryMock
                .Setup(r => r.GetOrderDetailsAsync(10))
                .ReturnsAsync(order);

            _orderRepositoryMock
                .Setup(r => r.UpdateOrderStatusAsync(10, It.IsAny<string>()))
                .ReturnsAsync(false); // Update fails for delivered order

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(10, "anulata");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task LoginWithEmployeeRole_SucceedsAndReturnsEmployeeRole()
        {
            // Arrange
            var employee = new User
            {
                UserId = 100,
                Email = "employee@restaurant.com",
                PasswordHash = AuthenticationService_Helper.HashPassword("employee123"),
                FirstName = "John",
                LastName = "Manager",
                Role = "Employee"
            };

            _userRepositoryMock
                .Setup(r => r.GetUserByEmailAsync("employee@restaurant.com"))
                .ReturnsAsync(employee);

            _userRepositoryMock
                .Setup(r => r.UpdateLastLoginDateAsync(100))
                .Returns(Task.CompletedTask);

            // Act
            var (success, message, userId, role) = await _authService.LoginAsync("employee@restaurant.com", "employee123");

            // Assert
            Assert.True(success);
            Assert.Equal(100, userId);
            Assert.Equal("Employee", role);
            Assert.True(_authService.IsEmployeeRole(role));
        }

        [Fact]
        public async Task LoginWithClientRole_SucceedsAndReturnsClientRole()
        {
            // Arrange
            var client = new User
            {
                UserId = 200,
                Email = "client@example.com",
                PasswordHash = AuthenticationService_Helper.HashPassword("client123"),
                FirstName = "Jane",
                LastName = "Customer",
                Role = "Client"
            };

            _userRepositoryMock
                .Setup(r => r.GetUserByEmailAsync("client@example.com"))
                .ReturnsAsync(client);

            _userRepositoryMock
                .Setup(r => r.UpdateLastLoginDateAsync(200))
                .Returns(Task.CompletedTask);

            // Act
            var (success, message, userId, role) = await _authService.LoginAsync("client@example.com", "client123");

            // Assert
            Assert.True(success);
            Assert.Equal(200, userId);
            Assert.Equal("Client", role);
            Assert.True(_authService.IsClientRole(role));
            Assert.False(_authService.IsEmployeeRole(role));
        }
    }

    /// <summary>
    /// Helper class for hashing passwords in tests
    /// </summary>
    internal class AuthenticationService_Helper
    {
        public static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
