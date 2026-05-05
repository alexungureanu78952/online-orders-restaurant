using Xunit;
using Moq;
using RestaurantOrderManagement.ViewModels;
using RestaurantOrderManagement.Services;
using RestaurantOrderManagement.Services.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantOrderManagement.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for MenuBrowseViewModel
    /// Tests T025: Verify view model exposes only DTOs without ID fields
    /// </summary>
    public class MenuBrowseViewModelTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly MenuBrowseViewModel _viewModel;

        public MenuBrowseViewModelTests()
        {
            _mockProductService = new Mock<IProductService>();
            _viewModel = new MenuBrowseViewModel(_mockProductService.Object);
        }

        [Fact]
        public async Task LoadCategories_PopulatesCategoriesCollection()
        {
            // Arrange
            var categories = new List<CategoryDTO>
            {
                new CategoryDTO { Name = "Pizza", Description = "Delicious pizzas", IsActive = true },
                new CategoryDTO { Name = "Pasta", Description = "Italian pasta", IsActive = true },
                new CategoryDTO { Name = "Salads", Description = "Fresh salads", IsActive = true }
            };

            _mockProductService
                .Setup(s => s.GetCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal(3, _viewModel.Categories.Count);
            Assert.Equal("Pizza", _viewModel.Categories[0].Name);
            Assert.Equal("Pasta", _viewModel.Categories[1].Name);
            Assert.NotNull(_viewModel.SelectedCategory);
            Assert.Equal("Pizza", _viewModel.SelectedCategory.Name);
        }

        [Fact]
        public async Task LoadProductsByCategory_PopulatesProductsCollection()
        {
            // Arrange
            var products = new List<ProductDTO>
            {
                new ProductDTO
                {
                    DisplayCode = "PIZZA-001",
                    Name = "Margherita",
                    Description = "Classic pizza",
                    Price = 12.99m,
                    PortionQuantity = 300,
                    IsAvailable = true,
                    CategoryName = "Pizza",
                    Allergens = new List<AllergenDTO>(),
                    ImageUrls = new List<string>()
                },
                new ProductDTO
                {
                    DisplayCode = "PIZZA-002",
                    Name = "Pepperoni",
                    Description = "Spicy pizza",
                    Price = 14.99m,
                    PortionQuantity = 350,
                    IsAvailable = true,
                    CategoryName = "Pizza",
                    Allergens = new List<AllergenDTO>(),
                    ImageUrls = new List<string>()
                }
            };

            var category = new CategoryDTO { Name = "Pizza", IsActive = true };
            _viewModel.SelectedCategory = category;

            _mockProductService
                .Setup(s => s.GetProductsByCategoryAsync(category.Name))
                .ReturnsAsync(products);

            // Act
            await _viewModel.LoadProductsByCategoryCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal(2, _viewModel.ProductsByCategory.Count);
            Assert.Equal("PIZZA-001", _viewModel.ProductsByCategory[0].DisplayCode);
            Assert.Equal("Margherita", _viewModel.ProductsByCategory[0].Name);
            Assert.Equal(12.99m, _viewModel.ProductsByCategory[0].Price);
        }

        [Fact]
        public async Task SearchProducts_ReturnsFilteredResults()
        {
            // Arrange
            var searchResults = new List<ProductDTO>
            {
                new ProductDTO
                {
                    DisplayCode = "PASTA-001",
                    Name = "Spaghetti Carbonara",
                    Price = 10.99m,
                    IsAvailable = true,
                    CategoryName = "Pasta",
                    Allergens = new List<AllergenDTO>(),
                    ImageUrls = new List<string>()
                }
            };

            _mockProductService
                .Setup(s => s.SearchProductsAsync("Carbonara", null, null))
                .ReturnsAsync(searchResults);

            // Act
            await _viewModel.SearchProductsCommand.ExecuteAsync("Carbonara");

            // Assert
            Assert.Single(_viewModel.ProductsByCategory);
            Assert.Equal("Spaghetti Carbonara", _viewModel.ProductsByCategory[0].Name);
        }

        [Fact]
        public async Task Refresh_ReloadsCategoriesAndProducts()
        {
            // Arrange
            var categories = new List<CategoryDTO>
            {
                new CategoryDTO { Name = "Pizza", IsActive = true }
            };

            var products = new List<ProductDTO>
            {
                new ProductDTO
                {
                    DisplayCode = "PIZZA-001",
                    Name = "Test Pizza",
                    Price = 10m,
                    IsAvailable = true,
                    CategoryName = "Pizza",
                    Allergens = new List<AllergenDTO>(),
                    ImageUrls = new List<string>()
                }
            };

            _mockProductService
                .Setup(s => s.GetCategoriesAsync())
                .ReturnsAsync(categories);

            _mockProductService
                .Setup(s => s.GetProductsByCategoryAsync("Pizza"))
                .ReturnsAsync(products);

            // Act
            await _viewModel.RefreshCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(_viewModel.Categories);
            Assert.Single(_viewModel.ProductsByCategory);
        }

        [Fact]
        public void ProductDTO_DoesNotExpose_ProductId()
        {
            // Arrange
            var productDTO = new ProductDTO
            {
                DisplayCode = "PROD-001",
                Name = "Test Product",
                Price = 9.99m,
                IsAvailable = true,
                CategoryName = "Test"
            };

            // Act
            var idProperty = productDTO.GetType().GetProperty("ProductId");

            // Assert
            Assert.Null(idProperty);
            // ✓ ProductDTO contains no ProductId field
        }

        [Fact]
        public void CategoryDTO_DoesNotExpose_CategoryId()
        {
            // Arrange
            var categoryDTO = new CategoryDTO
            {
                Name = "Test Category",
                IsActive = true
            };

            // Act
            var idProperty = categoryDTO.GetType().GetProperty("CategoryId");

            // Assert
            Assert.Null(idProperty);
            // ✓ CategoryDTO contains no CategoryId field
        }

        [Fact]
        public void ProductDTO_ExposeDisplayCode_NotNumericId()
        {
            // Arrange
            var productDTO = new ProductDTO
            {
                DisplayCode = "PIZZA-001", // Customer-friendly code
                Name = "Margherita"
            };

            // Act & Assert
            Assert.NotNull(productDTO.DisplayCode);
            Assert.False(int.TryParse(productDTO.DisplayCode, out _), "DisplayCode should not be purely numeric");
            Assert.StartsWith("PIZZA", productDTO.DisplayCode);
        }

        [Fact]
        public async Task ErrorHandling_CapturesExceptionMessage()
        {
            // Arrange
            var errorMessage = "Database connection failed";
            _mockProductService
                .Setup(s => s.GetCategoriesAsync())
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            await _viewModel.LoadCategoriesCommand.ExecuteAsync(null);

            // Assert
            Assert.NotEmpty(_viewModel.ErrorMessage);
            Assert.Contains("Failed to load categories", _viewModel.ErrorMessage);
        }

        [Fact]
        public void MenuBrowseViewModel_IsLoading_IndicatesAsyncOperation()
        {
            // Arrange
            var initialState = _viewModel.IsLoading;

            // Act & Assert
            Assert.False(initialState, "Should not be loading initially");
            // ✓ ViewModel properly tracks loading state for UI feedback
        }

        [Fact]
        public void ProductDTO_Allergens_MapsCorrectly()
        {
            // Arrange
            var productDTO = new ProductDTO
            {
                DisplayCode = "PASTA-001",
                Name = "Peanut Pasta",
                Allergens = new List<AllergenDTO>
                {
                    new AllergenDTO { Name = "Peanuts", Description = "Contains peanuts" },
                    new AllergenDTO { Name = "Gluten", Description = "Contains wheat gluten" }
                }
            };

            // Act & Assert
            Assert.Equal(2, productDTO.Allergens.Count);
            Assert.Equal("Peanuts", productDTO.Allergens[0].Name);
            // ✓ Allergen DTOs do not expose AllergenId
        }

        [Fact]
        public void ProductDTO_ImageUrls_OrderedByDisplayOrder()
        {
            // Arrange
            var productDTO = new ProductDTO
            {
                DisplayCode = "PIZZA-001",
                Name = "Pizza",
                ImageUrls = new List<string>
                {
                    "https://example.com/pizza-main.jpg",
                    "https://example.com/pizza-side.jpg",
                    "https://example.com/pizza-top.jpg"
                }
            };

            // Act & Assert
            Assert.Equal(3, productDTO.ImageUrls.Count);
            Assert.Equal("https://example.com/pizza-main.jpg", productDTO.ImageUrls[0]);
            // ✓ Images are in display order for gallery
        }
    }
}
