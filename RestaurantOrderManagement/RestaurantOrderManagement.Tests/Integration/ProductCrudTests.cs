using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using RestaurantOrderManagement.Data.Models;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Services.Implementations;
using RestaurantOrderManagement.Services.Interfaces;

namespace RestaurantOrderManagement.Tests.Integration
{
    /// <summary>
    /// Integration tests for Product and Category CRUD operations
    /// Tests both direct repository calls and service layer business logic
    /// </summary>
    public class ProductCrudTests
    {
        #region Product CRUD Tests

        [Fact]
        public async Task CreateProduct_WithValidInputs_ReturnsProductId()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var mockConfigService = new Mock<IConfigurationService>();
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Category { CategoryId = 1, Name = "Pizzas", IsActive = true });

            mockRepository
                .Setup(r => r.CreateProductAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(101);

            // Act
            int productId = await service.CreateProductAsync(1, "Margherita", "Classic cheese pizza", 12.99m, 400, 1000);

            // Assert
            Assert.Equal(101, productId);
            mockRepository.Verify(r => r.CreateProductAsync(1, "Margherita", "Classic cheese pizza", 12.99m, 400, 1000), Times.Once);
        }

        [Fact]
        public async Task CreateProduct_WithNegativePrice_ThrowsException()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Category { CategoryId = 1, Name = "Pizzas" });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CreateProductAsync(1, "Margherita", "Pizza", -5.00m, 400, 1000));
        }

        [Fact]
        public async Task CreateProduct_WithoutCategoryId_ThrowsException()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CreateProductAsync(0, "Margherita", "Pizza", 12.99m, 400, 1000));
        }

        [Fact]
        public async Task CreateProduct_WithoutProductName_ThrowsException()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Category { CategoryId = 1 });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CreateProductAsync(1, "", "Pizza", 12.99m, 400, 1000));
        }

        [Fact]
        public async Task CreateProduct_WithNegativePortionQuantity_ThrowsException()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Category { CategoryId = 1 });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CreateProductAsync(1, "Margherita", "Pizza", 12.99m, -100, 1000));
        }

        [Fact]
        public async Task GetProductById_WithValidId_ReturnsProduct()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);
            var testProduct = new Product { ProductId = 1, Name = "Margherita", Price = 12.99m };

            mockRepository
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(testProduct);

            // Act
            var product = await service.GetProductByIdAsync(1);

            // Assert
            Assert.NotNull(product);
            Assert.Equal("Margherita", product.Name);
            Assert.Equal(12.99m, product.Price);
        }

        [Fact]
        public async Task GetProductById_WithInvalidId_ThrowsException()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetProductByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Product)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await service.GetProductByIdAsync(999));
        }

        [Fact]
        public async Task UpdateProduct_WithValidInputs_ReturnsSuccess()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);
            var existingProduct = new Product { ProductId = 1, Name = "Margherita", Price = 12.99m };

            mockRepository
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(existingProduct);

            mockRepository
                .Setup(r => r.UpdateProductAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            // Act
            bool success = await service.UpdateProductAsync(1, "Updated Margherita", "Updated description", 14.99m, 450, true);

            // Assert
            Assert.True(success);
            mockRepository.Verify(r => r.UpdateProductAsync(1, "Updated Margherita", "Updated description", 14.99m, 450, true), Times.Once);
        }

        [Fact]
        public async Task UpdateProduct_WithNegativePrice_ThrowsException()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetProductByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Product { ProductId = 1 });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.UpdateProductAsync(1, "Margherita", "Pizza", -5.00m, 400, true));
        }

        [Fact]
        public async Task DeleteProduct_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetProductByIdAsync(1))
                .ReturnsAsync(new Product { ProductId = 1, Name = "Margherita" });

            mockRepository
                .Setup(r => r.DeleteProductAsync(1))
                .ReturnsAsync(true);

            // Act
            bool success = await service.DeleteProductAsync(1);

            // Assert
            Assert.True(success);
            mockRepository.Verify(r => r.DeleteProductAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetAllProducts_ReturnsAllNonDeletedProducts()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Margherita", IsDeleted = false },
                new Product { ProductId = 2, Name = "Pepperoni", IsDeleted = false },
                new Product { ProductId = 3, Name = "Deleted Product", IsDeleted = true }
            };

            mockRepository
                .Setup(r => r.GetAllProductsAsync())
                .ReturnsAsync(products);

            // Act
            var result = await service.GetAllProductsAsync(includeDeleted: false);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.DoesNotContain(result, p => p.Name == "Deleted Product");
        }

        [Fact]
        public async Task GetAllProducts_WithDeletedFlag_ReturnsAllProducts()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Margherita", IsDeleted = false },
                new Product { ProductId = 2, Name = "Pepperoni", IsDeleted = false },
                new Product { ProductId = 3, Name = "Deleted Product", IsDeleted = true }
            };

            mockRepository
                .Setup(r => r.GetAllProductsAsync())
                .ReturnsAsync(products);

            // Act
            var result = await service.GetAllProductsAsync(includeDeleted: true);

            // Assert
            Assert.Equal(3, result.Count());
        }

        #endregion

        #region Category CRUD Tests

        [Fact]
        public async Task CreateCategory_WithValidInputs_ReturnsCategoryId()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.CreateCategoryAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(5);

            // Act
            int categoryId = await service.CreateCategoryAsync("Pizzas", "All our delicious pizzas");

            // Assert
            Assert.Equal(5, categoryId);
            mockRepository.Verify(r => r.CreateCategoryAsync("Pizzas", "All our delicious pizzas"), Times.Once);
        }

        [Fact]
        public async Task CreateCategory_WithoutName_ThrowsException()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CreateCategoryAsync("", "Description"));
        }

        [Fact]
        public async Task GetCategoryById_WithValidId_ReturnsCategory()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);
            var testCategory = new Category { CategoryId = 1, Name = "Pizzas", IsActive = true };

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(1))
                .ReturnsAsync(testCategory);

            // Act
            var category = await service.GetCategoryByIdAsync(1);

            // Assert
            Assert.NotNull(category);
            Assert.Equal("Pizzas", category.Name);
            Assert.True(category.IsActive);
        }

        [Fact]
        public async Task GetCategoryById_WithInvalidId_ThrowsException()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Category)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await service.GetCategoryByIdAsync(999));
        }

        [Fact]
        public async Task UpdateCategory_WithValidInputs_ReturnsSuccess()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(1))
                .ReturnsAsync(new Category { CategoryId = 1, Name = "Pizzas" });

            mockRepository
                .Setup(r => r.UpdateCategoryAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            // Act
            bool success = await service.UpdateCategoryAsync(1, "Italian Pizzas", "Updated description", true);

            // Assert
            Assert.True(success);
            mockRepository.Verify(r => r.UpdateCategoryAsync(1, "Italian Pizzas", "Updated description", true), Times.Once);
        }

        [Fact]
        public async Task DeleteCategory_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(1))
                .ReturnsAsync(new Category { CategoryId = 1, Name = "Pizzas" });

            mockRepository
                .Setup(r => r.DeleteCategoryAsync(1))
                .ReturnsAsync(true);

            // Act
            bool success = await service.DeleteCategoryAsync(1);

            // Assert
            Assert.True(success);
            mockRepository.Verify(r => r.DeleteCategoryAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetAllCategories_ReturnsCategoriesSortedByName()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Pizzas", IsActive = true },
                new Category { CategoryId = 2, Name = "Appetizers", IsActive = true },
                new Category { CategoryId = 3, Name = "Desserts", IsActive = true }
            };

            mockRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await service.GetAllCategoriesAsync();

            // Assert
            var resultList = result.ToList();
            Assert.Equal(3, resultList.Count);
            Assert.Equal("Appetizers", resultList[0].Name);
            Assert.Equal("Desserts", resultList[1].Name);
            Assert.Equal("Pizzas", resultList[2].Name);
        }

        #endregion

        #region End-to-End Workflow Tests

        [Fact]
        public async Task WorkflowTest_CreateCategoryCreateProductUpdateProduct()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            // Setup category creation
            mockRepository
                .Setup(r => r.CreateCategoryAsync("Beverages", "Drinks and beverages"))
                .ReturnsAsync(10);

            // Setup category retrieval
            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(10))
                .ReturnsAsync(new Category { CategoryId = 10, Name = "Beverages", IsActive = true });

            // Setup product creation
            mockRepository
                .Setup(r => r.CreateProductAsync(10, "Iced Tea", "Refreshing iced tea", 3.99m, 500, 2000))
                .ReturnsAsync(201);

            // Setup product retrieval
            mockRepository
                .Setup(r => r.GetProductByIdAsync(201))
                .ReturnsAsync(new Product { ProductId = 201, Name = "Iced Tea", Price = 3.99m, TotalQuantity = 2000 });

            // Setup product update
            mockRepository
                .Setup(r => r.UpdateProductAsync(201, "Premium Iced Tea", "Premium refreshing iced tea", 4.99m, 500, true))
                .ReturnsAsync(true);

            // Act - Create category
            int categoryId = await service.CreateCategoryAsync("Beverages", "Drinks and beverages");
            Assert.Equal(10, categoryId);

            // Act - Create product
            int productId = await service.CreateProductAsync(categoryId, "Iced Tea", "Refreshing iced tea", 3.99m, 500, 2000);
            Assert.Equal(201, productId);

            // Act - Update product
            bool updateSuccess = await service.UpdateProductAsync(201, "Premium Iced Tea", "Premium refreshing iced tea", 4.99m, 500, true);
            Assert.True(updateSuccess);

            // Assert - Verify repository calls
            mockRepository.Verify(r => r.CreateCategoryAsync("Beverages", "Drinks and beverages"), Times.Once);
            mockRepository.Verify(r => r.CreateProductAsync(10, "Iced Tea", "Refreshing iced tea", 3.99m, 500, 2000), Times.Once);
            mockRepository.Verify(r => r.UpdateProductAsync(201, "Premium Iced Tea", "Premium refreshing iced tea", 4.99m, 500, true), Times.Once);
        }

        [Fact]
        public async Task WorkflowTest_CreateMultipleProductsInCategory()
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetCategoryByIdAsync(5))
                .ReturnsAsync(new Category { CategoryId = 5, Name = "Pasta" });

            var productIdSequence = new[] { 301, 302 };
            var callCount = 0;

            mockRepository
                .Setup(r => r.CreateProductAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<int, string, string, decimal, int, int>((cid, name, desc, price, portion, total) =>
                {
                    return Task.FromResult(productIdSequence[callCount++]);
                });

            // Act & Assert
            int id1 = await service.CreateProductAsync(5, "Spaghetti", "Classic spaghetti", 10.99m, 400, 1000);
            int id2 = await service.CreateProductAsync(5, "Fettuccine", "Rich fettuccine", 11.99m, 450, 1000);

            Assert.True(id1 > 0);
            Assert.True(id2 > 0);
            Assert.NotEqual(id1, id2);
            mockRepository.Verify(r => r.CreateProductAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
        }

        #endregion

        #region Validation Tests

        [Theory]
        [InlineData(0, "Invalid category ID")]
        [InlineData(-1, "Negative category ID")]
        public async Task CreateProduct_InvalidCategoryId_ThrowsException(int categoryId, string testName)
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CreateProductAsync(categoryId, "Product", "Desc", 10m, 400, 1000));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public async Task UpdateProduct_InvalidProductId_ThrowsException(int productId)
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.UpdateProductAsync(productId, "Product", "Desc", 10m, 400, true));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task DeleteProduct_InvalidProductId_ThrowsException(int productId)
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.DeleteProductAsync(productId));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task CreateCategory_InvalidName_ThrowsException(string name)
        {
            // Arrange
            var mockRepository = new Mock<ProductRepository>(null);
            var service = new ProductService(mockRepository.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CreateCategoryAsync(name, "Description"));
        }

        #endregion
    }
}
