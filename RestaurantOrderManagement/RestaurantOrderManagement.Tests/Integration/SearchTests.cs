using Xunit;
using Moq;
using RestaurantOrderManagement.Services;
using RestaurantOrderManagement.Services.DTOs;
using RestaurantOrderManagement.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantOrderManagement.Tests.Integration
{
    /// <summary>
    /// Integration tests for search and filter functionality
    /// Tests T030: Search with keyword, allergen filters, grouping, case-insensitive matching
    /// </summary>
    public class SearchTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly SearchViewModel _searchViewModel;

        public SearchTests()
        {
            _mockProductService = new Mock<IProductService>();
            _searchViewModel = new SearchViewModel(_mockProductService.Object);
        }

        [Fact]
        public async Task PerformSearch_WithKeyword_ReturnsCaseInsensitiveResults()
        {
            // Arrange
            var products = new List<ProductDTO>
            {
                new ProductDTO
                {
                    DisplayCode = "PIZZA-001",
                    Name = "Margherita Pizza",
                    Description = "Classic tomato and mozzarella",
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
                    Name = "pepperoni PIZZA",
                    Description = "Spicy pepperoni on cheese",
                    Price = 14.99m,
                    PortionQuantity = 350,
                    IsAvailable = true,
                    CategoryName = "Pizza",
                    Allergens = new List<AllergenDTO>(),
                    ImageUrls = new List<string>()
                }
            };

            _searchViewModel.SearchQuery = "pizza";

            _mockProductService
                .Setup(s => s.SearchProductsAsync("pizza", null, null))
                .ReturnsAsync(products);

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal(2, _searchViewModel.GroupedResults.Count);
            Assert.Equal("Pizza", _searchViewModel.GroupedResults[0].CategoryName);
            Assert.Equal(2, _searchViewModel.GroupedResults[0].Products.Count);
            Assert.Equal(2, _searchViewModel.ResultCount);
            _mockProductService.Verify(s => s.SearchProductsAsync("pizza", null, null), Times.Once);
        }

        [Fact]
        public async Task PerformSearch_WithIncludeAllergens_FiltersProducts()
        {
            // Arrange
            var productsWithAllergens = new List<ProductDTO>
            {
                new ProductDTO
                {
                    DisplayCode = "PASTA-001",
                    Name = "Peanut Pasta",
                    Price = 10.99m,
                    IsAvailable = true,
                    CategoryName = "Pasta",
                    Allergens = new List<AllergenDTO>
                    {
                        new AllergenDTO { Name = "Peanuts", Description = "Contains peanuts" }
                    },
                    ImageUrls = new List<string>()
                }
            };

            _searchViewModel.IncludeAllergens = "1"; // Peanuts ID

            _mockProductService
                .Setup(s => s.SearchProductsAsync(null, "1", null))
                .ReturnsAsync(productsWithAllergens);

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(_searchViewModel.GroupedResults);
            Assert.Single(_searchViewModel.GroupedResults[0].Products);
            Assert.Equal("Peanut Pasta", _searchViewModel.GroupedResults[0].Products[0].Name);
            Assert.Single(_searchViewModel.GroupedResults[0].Products[0].Allergens);
            Assert.Equal("Peanuts", _searchViewModel.GroupedResults[0].Products[0].Allergens[0].Name);
        }

        [Fact]
        public async Task PerformSearch_WithExcludeAllergens_RemovesProducts()
        {
            // Arrange
            var productsWithoutAllergen = new List<ProductDTO>
            {
                new ProductDTO
                {
                    DisplayCode = "SALAD-001",
                    Name = "Caesar Salad",
                    Price = 8.99m,
                    IsAvailable = true,
                    CategoryName = "Salads",
                    Allergens = new List<AllergenDTO>(),
                    ImageUrls = new List<string>()
                }
            };

            _searchViewModel.ExcludeAllergens = "3"; // Exclude dairy

            _mockProductService
                .Setup(s => s.SearchProductsAsync(null, null, "3"))
                .ReturnsAsync(productsWithoutAllergen);

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(_searchViewModel.GroupedResults);
            Assert.Single(_searchViewModel.GroupedResults[0].Products);
            Assert.Empty(_searchViewModel.GroupedResults[0].Products[0].Allergens);
        }

        [Fact]
        public async Task PerformSearch_WithMultipleAllergens_CommaDelimited()
        {
            // Arrange
            var productsMultiAllergen = new List<ProductDTO>
            {
                new ProductDTO
                {
                    DisplayCode = "BURGER-001",
                    Name = "Classic Burger",
                    Price = 11.99m,
                    IsAvailable = true,
                    CategoryName = "Burgers",
                    Allergens = new List<AllergenDTO>
                    {
                        new AllergenDTO { Name = "Gluten", Description = "Wheat bread" },
                        new AllergenDTO { Name = "Dairy", Description = "Cheese" }
                    },
                    ImageUrls = new List<string>()
                }
            };

            _searchViewModel.IncludeAllergens = "2,3"; // Multiple IDs

            _mockProductService
                .Setup(s => s.SearchProductsAsync(null, "2,3", null))
                .ReturnsAsync(productsMultiAllergen);

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(_searchViewModel.GroupedResults);
            Assert.Equal(2, _searchViewModel.GroupedResults[0].Products[0].Allergens.Count);
        }

        [Fact]
        public async Task PerformSearch_ResultsGroupedByCategory()
        {
            // Arrange
            var mixedProducts = new List<ProductDTO>
            {
                new ProductDTO { DisplayCode = "PIZZA-001", Name = "Margherita", CategoryName = "Pizza", Price = 12.99m, IsAvailable = true, Allergens = new List<AllergenDTO>(), ImageUrls = new List<string>() },
                new ProductDTO { DisplayCode = "PIZZA-002", Name = "Pepperoni", CategoryName = "Pizza", Price = 14.99m, IsAvailable = true, Allergens = new List<AllergenDTO>(), ImageUrls = new List<string>() },
                new ProductDTO { DisplayCode = "PASTA-001", Name = "Carbonara", CategoryName = "Pasta", Price = 10.99m, IsAvailable = true, Allergens = new List<AllergenDTO>(), ImageUrls = new List<string>() },
                new ProductDTO { DisplayCode = "SALAD-001", Name = "Caesar", CategoryName = "Salads", Price = 8.99m, IsAvailable = true, Allergens = new List<AllergenDTO>(), ImageUrls = new List<string>() }
            };

            _searchViewModel.SearchQuery = ""; // No keyword filter

            _mockProductService
                .Setup(s => s.SearchProductsAsync(null, null, null))
                .ReturnsAsync(mixedProducts);

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal(3, _searchViewModel.GroupedResults.Count); // 3 categories
            Assert.Equal("Pasta", _searchViewModel.GroupedResults[0].CategoryName);
            Assert.Equal("Pizza", _searchViewModel.GroupedResults[1].CategoryName);
            Assert.Equal("Salads", _searchViewModel.GroupedResults[2].CategoryName);
            Assert.Equal(2, _searchViewModel.GroupedResults[1].Products.Count); // 2 pizzas
            Assert.Equal(4, _searchViewModel.ResultCount);
        }

        [Fact]
        public async Task PerformSearch_NoResults_ShowsErrorMessage()
        {
            // Arrange
            _searchViewModel.SearchQuery = "nonexistent";

            _mockProductService
                .Setup(s => s.SearchProductsAsync("nonexistent", null, null))
                .ReturnsAsync(new List<ProductDTO>());

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.Empty(_searchViewModel.GroupedResults);
            Assert.Equal(0, _searchViewModel.ResultCount);
            Assert.NotEmpty(_searchViewModel.ErrorMessage);
            Assert.Contains("No products found", _searchViewModel.ErrorMessage);
        }

        [Fact]
        public async Task PerformSearch_EmptyFilters_ShowsValidationError()
        {
            // Arrange
            _searchViewModel.SearchQuery = "";
            _searchViewModel.IncludeAllergens = "";
            _searchViewModel.ExcludeAllergens = "";

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.NotEmpty(_searchViewModel.ErrorMessage);
            Assert.Contains("search criteria", _searchViewModel.ErrorMessage);
            _mockProductService.Verify(s => s.SearchProductsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ClearFilters_ResetsAllFields()
        {
            // Arrange
            _searchViewModel.SearchQuery = "pizza";
            _searchViewModel.IncludeAllergens = "1,2";
            _searchViewModel.ExcludeAllergens = "3";
            _searchViewModel.ErrorMessage = "Some error";

            // Act
            _searchViewModel.ClearFiltersCommand.Execute(null);

            // Assert
            Assert.Empty(_searchViewModel.SearchQuery);
            Assert.Empty(_searchViewModel.IncludeAllergens);
            Assert.Empty(_searchViewModel.ExcludeAllergens);
            Assert.Empty(_searchViewModel.ErrorMessage);
            Assert.Empty(_searchViewModel.GroupedResults);
            Assert.Equal(0, _searchViewModel.ResultCount);
        }

        [Fact]
        public void AddIncludeAllergen_AppendsToList()
        {
            // Arrange
            _searchViewModel.IncludeAllergens = "1";

            // Act
            _searchViewModel.AddIncludeAllergenCommand.Execute("2");

            // Assert
            Assert.Equal("1,2", _searchViewModel.IncludeAllergens);

            // Act - Add third
            _searchViewModel.AddIncludeAllergenCommand.Execute("3");

            // Assert
            Assert.Equal("1,2,3", _searchViewModel.IncludeAllergens);
        }

        [Fact]
        public void AddIncludeAllergen_FirstTime_DoesNotAddComma()
        {
            // Arrange
            _searchViewModel.IncludeAllergens = "";

            // Act
            _searchViewModel.AddIncludeAllergenCommand.Execute("1");

            // Assert
            Assert.Equal("1", _searchViewModel.IncludeAllergens);
        }

        [Fact]
        public void RemoveIncludeAllergen_RemovesFromList()
        {
            // Arrange
            _searchViewModel.IncludeAllergens = "1,2,3";

            // Act
            _searchViewModel.RemoveIncludeAllergenCommand.Execute("2");

            // Assert
            Assert.Equal("1,3", _searchViewModel.IncludeAllergens);
        }

        [Fact]
        public void AddExcludeAllergen_AppendsToList()
        {
            // Arrange
            _searchViewModel.ExcludeAllergens = "5";

            // Act
            _searchViewModel.AddExcludeAllergenCommand.Execute("6");

            // Assert
            Assert.Equal("5,6", _searchViewModel.ExcludeAllergens);
        }

        [Fact]
        public void RemoveExcludeAllergen_RemovesFromList()
        {
            // Arrange
            _searchViewModel.ExcludeAllergens = "5,6,7";

            // Act
            _searchViewModel.RemoveExcludeAllergenCommand.Execute("6");

            // Assert
            Assert.Equal("5,7", _searchViewModel.ExcludeAllergens);
        }

        [Fact]
        public async Task PerformSearch_WithKeywordAndFilters_CombinesAll()
        {
            // Arrange
            var filteredResults = new List<ProductDTO>
            {
                new ProductDTO
                {
                    DisplayCode = "PASTA-002",
                    Name = "Peanut Pasta Special",
                    Description = "Pasta with peanut sauce and no dairy",
                    Price = 11.99m,
                    IsAvailable = true,
                    CategoryName = "Pasta",
                    Allergens = new List<AllergenDTO>
                    {
                        new AllergenDTO { Name = "Peanuts", Description = "Peanut sauce" }
                    },
                    ImageUrls = new List<string>()
                }
            };

            _searchViewModel.SearchQuery = "peanut";
            _searchViewModel.IncludeAllergens = "1"; // Peanuts
            _searchViewModel.ExcludeAllergens = "3"; // No dairy

            _mockProductService
                .Setup(s => s.SearchProductsAsync("peanut", "1", "3"))
                .ReturnsAsync(filteredResults);

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(_searchViewModel.GroupedResults);
            Assert.Equal("Peanut Pasta Special", _searchViewModel.GroupedResults[0].Products[0].Name);
            Assert.Equal(1, _searchViewModel.ResultCount);
            _mockProductService.Verify(s => s.SearchProductsAsync("peanut", "1", "3"), Times.Once);
        }

        [Fact]
        public async Task PerformSearch_LoadingState_IndicatesAsyncOperation()
        {
            // Arrange
            var initialState = _searchViewModel.IsLoading;
            _searchViewModel.SearchQuery = "test";

            _mockProductService
                .Setup(s => s.SearchProductsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<ProductDTO>());

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.False(initialState, "Should not be loading initially");
            Assert.False(_searchViewModel.IsLoading, "Should complete loading");
        }

        [Fact]
        public async Task PerformSearch_Exception_CapturesErrorMessage()
        {
            // Arrange
            _searchViewModel.SearchQuery = "error";
            var errorMsg = "Database connection timeout";

            _mockProductService
                .Setup(s => s.SearchProductsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception(errorMsg));

            // Act
            await _searchViewModel.PerformSearchCommand.ExecuteAsync(null);

            // Assert
            Assert.NotEmpty(_searchViewModel.ErrorMessage);
            Assert.Contains("Search failed", _searchViewModel.ErrorMessage);
        }
    }
}
