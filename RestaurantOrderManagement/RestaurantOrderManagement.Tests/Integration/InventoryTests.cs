using Moq;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Implementations;
using RestaurantOrderManagement.Services.Interfaces;
using Xunit;

namespace RestaurantOrderManagement.Tests.Integration
{
    /// <summary>
    /// Integration tests for inventory management workflows
    /// Verifies low-stock detection and restock operations
    /// </summary>
    public class InventoryTests
    {
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<IConfigurationService> _configurationMock;
        private readonly InventoryService _inventoryService;

        public InventoryTests()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
            _configurationMock = new Mock<IConfigurationService>();

            // Setup configuration defaults
            _configurationMock.Setup(c => c.GetLowStockThresholdAsync()).ReturnsAsync(500); // 500g default threshold

            _inventoryService = new InventoryService(
                _productRepositoryMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task GetLowStockProducts_ReturnsProductsBelowThreshold()
        {
            // Arrange: Products below low-stock threshold
            var lowStockProducts = new List<Product>
            {
                new Product { ProductId = 1, Name = "Bread", TotalQuantity = 200, Category = new Category { Name = "Bakery" } },
                new Product { ProductId = 2, Name = "Milk", TotalQuantity = 300, Category = new Category { Name = "Dairy" } }
            };

            _productRepositoryMock
                .Setup(r => r.GetLowStockProductsAsync())
                .ReturnsAsync(lowStockProducts);

            // Act
            var result = await _inventoryService.GetLowStockProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.True(p.TotalQuantity < 500));
            _productRepositoryMock.Verify(r => r.GetLowStockProductsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetLowStockProducts_EmptyListWhenAllStocked()
        {
            // Arrange: No low-stock products
            var lowStockProducts = new List<Product>();

            _productRepositoryMock
                .Setup(r => r.GetLowStockProductsAsync())
                .ReturnsAsync(lowStockProducts);

            // Act
            var result = await _inventoryService.GetLowStockProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateProductInventory_DecrementOnOrderCreation()
        {
            // Arrange: Decrement inventory for order (negative change)
            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(1, -500)) // Order uses 500g
                .ReturnsAsync(true);

            // Act
            var result = await _inventoryService.UpdateProductInventoryAsync(1, -500);

            // Assert
            Assert.True(result);
            _productRepositoryMock.Verify(r => r.UpdateInventoryAsync(1, -500), Times.Once);
        }

        [Fact]
        public async Task UpdateProductInventory_IncrementOnCancellation()
        {
            // Arrange: Increment inventory on order cancellation (positive change)
            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(1, 500)) // Restore 500g
                .ReturnsAsync(true);

            // Act
            var result = await _inventoryService.UpdateProductInventoryAsync(1, 500);

            // Assert
            Assert.True(result);
            _productRepositoryMock.Verify(r => r.UpdateInventoryAsync(1, 500), Times.Once);
        }

        [Fact]
        public async Task UpdateProductInventory_InvalidProductId_ThrowsException()
        {
            // Arrange: Invalid product ID
            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _inventoryService.UpdateProductInventoryAsync(0, 100)
            );
            Assert.Contains("Invalid product ID", ex.Message);
        }

        [Fact]
        public async Task RestockProduct_AddsQuantitySuccessfully()
        {
            // Arrange: Restock operation adds quantity
            var product = new Product { ProductId = 1, Name = "Bread", TotalQuantity = 300, Category = new Category { Name = "Bakery" } };
            var restockedProduct = new Product { ProductId = 1, Name = "Bread", TotalQuantity = 800, Category = new Category { Name = "Bakery" } };

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(1, 500))
                .ReturnsAsync(true);

            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(restockedProduct);

            // Act
            var result = await _inventoryService.RestockProductAsync(1, 500);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(800, result.TotalQuantity);
            _productRepositoryMock.Verify(r => r.UpdateInventoryAsync(1, 500), Times.Once);
            _productRepositoryMock.Verify(r => r.GetProductByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task RestockProduct_NegativeQuantity_ThrowsException()
        {
            // Arrange: Negative restock quantity
            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _inventoryService.RestockProductAsync(1, -100)
            );
            Assert.Contains("must be positive", ex.Message);
        }

        [Fact]
        public async Task RestockProduct_ZeroQuantity_ThrowsException()
        {
            // Arrange: Zero restock quantity
            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _inventoryService.RestockProductAsync(1, 0)
            );
            Assert.Contains("must be positive", ex.Message);
        }

        [Fact]
        public async Task GetProductWithInventory_ReturnsProductDetails()
        {
            // Arrange: Get product with inventory info
            var product = new Product { ProductId = 1, Name = "Bread", TotalQuantity = 200, UnitPrice = 10.00m };

            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(product);

            // Act
            var result = await _inventoryService.GetProductWithInventoryAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ProductId);
            Assert.Equal("Bread", result.Name);
            Assert.Equal(200, result.TotalQuantity);
        }

        [Fact]
        public async Task GetProductWithInventory_NotFound_ThrowsException()
        {
            // Arrange: Product not found
            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync((Product)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _inventoryService.GetProductWithInventoryAsync(1)
            );
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task GetLowStockThreshold_ReturnsConfiguredValue()
        {
            // Arrange: Configuration returns threshold
            _configurationMock
                .Setup(c => c.GetLowStockThresholdAsync())
                .ReturnsAsync(750);

            // Act
            var result = await _inventoryService.GetLowStockThresholdAsync();

            // Assert
            Assert.Equal(750, result);
        }

        [Fact]
        public async Task GetLowStockThreshold_ReturnDefaultOnFailure()
        {
            // Arrange: Configuration throws exception
            _configurationMock
                .Setup(c => c.GetLowStockThresholdAsync())
                .ThrowsAsync(new Exception("Config error"));

            // Act
            var result = await _inventoryService.GetLowStockThresholdAsync();

            // Assert
            Assert.Equal(500, result); // Default threshold
        }

        [Fact]
        public async Task InventoryWorkflow_OrderCreationReducesStock()
        {
            // Arrange: Simulate order creation reducing inventory
            var productBefore = new Product { ProductId = 1, Name = "Pasta", TotalQuantity = 1000 };
            var productAfter = new Product { ProductId = 1, Name = "Pasta", TotalQuantity = 500 };

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(1, -500))
                .ReturnsAsync(true);

            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(productAfter);

            // Act: Simulate order using 500g
            var updateResult = await _inventoryService.UpdateProductInventoryAsync(1, -500);
            var productResult = await _inventoryService.GetProductWithInventoryAsync(1);

            // Assert
            Assert.True(updateResult);
            Assert.Equal(500, productResult.TotalQuantity);
        }

        [Fact]
        public async Task InventoryWorkflow_OrderCancellationRestoresStock()
        {
            // Arrange: Simulate order cancellation restoring inventory
            var productBeforeCancel = new Product { ProductId = 1, Name = "Cheese", TotalQuantity = 200 };
            var productAfterCancel = new Product { ProductId = 1, Name = "Cheese", TotalQuantity = 700 };

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(1, 500)) // Restore 500g
                .ReturnsAsync(true);

            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(productAfterCancel);

            // Act: Simulate order cancellation
            var updateResult = await _inventoryService.UpdateProductInventoryAsync(1, 500);
            var productResult = await _inventoryService.GetProductWithInventoryAsync(1);

            // Assert
            Assert.True(updateResult);
            Assert.Equal(700, productResult.TotalQuantity);
        }

        [Fact]
        public async Task InventoryWorkflow_EmployeeRestocksBelowThreshold()
        {
            // Arrange: Employee restocks product below threshold
            var lowStockProducts = new List<Product>
            {
                new Product { ProductId = 1, Name = "Rice", TotalQuantity = 250, Category = new Category { Name = "Grains" } }
            };

            var restockedProduct = new Product { ProductId = 1, Name = "Rice", TotalQuantity = 1250, Category = new Category { Name = "Grains" } };

            _productRepositoryMock
                .Setup(r => r.GetLowStockProductsAsync())
                .ReturnsAsync(lowStockProducts);

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(1, 1000))
                .ReturnsAsync(true);

            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(restockedProduct);

            // Act
            var lowStockResult = await _inventoryService.GetLowStockProductsAsync();
            var restockResult = await _inventoryService.RestockProductAsync(1, 1000);

            // Assert
            Assert.Single(lowStockResult);
            Assert.NotNull(restockResult);
            Assert.Equal(1250, restockResult.TotalQuantity);
        }

        [Fact]
        public async Task InventoryWorkflow_MultipleProductsLowStock()
        {
            // Arrange: Multiple products below threshold
            var lowStockProducts = new List<Product>
            {
                new Product { ProductId = 1, Name = "Bread", TotalQuantity = 200, Category = new Category { Name = "Bakery" } },
                new Product { ProductId = 2, Name = "Milk", TotalQuantity = 300, Category = new Category { Name = "Dairy" } },
                new Product { ProductId = 3, Name = "Eggs", TotalQuantity = 100, Category = new Category { Name = "Dairy" } }
            };

            _productRepositoryMock
                .Setup(r => r.GetLowStockProductsAsync())
                .ReturnsAsync(lowStockProducts);

            // Act
            var result = await _inventoryService.GetLowStockProductsAsync();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.True(result.All(p => p.TotalQuantity < 500));
        }

        [Fact]
        public async Task InventoryWorkflow_PartialRestock_StillBelowThreshold()
        {
            // Arrange: Small restock that keeps product below threshold
            var product = new Product { ProductId = 1, Name = "Flour", TotalQuantity = 100, Category = new Category { Name = "Bakery" } };
            var restockedProduct = new Product { ProductId = 1, Name = "Flour", TotalQuantity = 350, Category = new Category { Name = "Bakery" } };

            _productRepositoryMock
                .Setup(r => r.UpdateInventoryAsync(1, 250))
                .ReturnsAsync(true);

            _productRepositoryMock
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(restockedProduct);

            // Act
            var result = await _inventoryService.RestockProductAsync(1, 250);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(350, result.TotalQuantity);
            Assert.True(result.TotalQuantity < 500); // Still below threshold
        }
    }
}
