using Xunit;
using Moq;
using RestaurantOrderManagement.Services;
using RestaurantOrderManagement.Services.Implementations;
using RestaurantOrderManagement.Data.Repositories;
using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Tests.Services
{
    /// <summary>
    /// Unit tests for AuthenticationService
    /// Tests T035: Registration, login, duplicate email prevention, password validation
    /// </summary>
    public class AuthenticationServiceTests
    {
        private readonly Mock<UserRepository> _mockUserRepository;
        private readonly AuthenticationService _authenticationService;

        public AuthenticationServiceTests()
        {
            _mockUserRepository = new Mock<UserRepository>(null);
            _authenticationService = new AuthenticationService(_mockUserRepository.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidInput_CreatesUserSuccessfully()
        {
            // Arrange
            var email = "newuser@example.com";
            var password = "SecurePass123";
            var firstName = "John";
            var lastName = "Doe";
            var userId = 42;

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync((User)null);

            _mockUserRepository
                .Setup(r => r.CreateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(userId);

            // Act
            var (success, message, returnedUserId) = await _authenticationService.RegisterAsync(
                email, password, firstName, lastName);

            // Assert
            Assert.True(success);
            Assert.Equal(userId, returnedUserId);
            Assert.Contains("successfully", message.ToLower());
            _mockUserRepository.Verify(
                r => r.CreateUserAsync(email, It.IsAny<string>(), firstName, lastName, null, null, "Client"),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsError()
        {
            // Arrange
            var email = "existing@example.com";
            var password = "SecurePass123";

            var existingUser = new User
            {
                UserId = 1,
                Email = email,
                FirstName = "Existing"
            };

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync(existingUser);

            // Act
            var (success, message, userId) = await _authenticationService.RegisterAsync(
                email, password, "John", "Doe");

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Contains("already registered", message.ToLower());
            _mockUserRepository.Verify(
                r => r.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_InvalidEmail_ReturnsError()
        {
            // Arrange
            var invalidEmail = "not-an-email";
            var password = "SecurePass123";

            // Act
            var (success, message, userId) = await _authenticationService.RegisterAsync(
                invalidEmail, password, "John", "Doe");

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Contains("email", message.ToLower());
        }

        [Fact]
        public async Task RegisterAsync_EmptyEmail_ReturnsError()
        {
            // Arrange
            var password = "SecurePass123";

            // Act
            var (success, message, userId) = await _authenticationService.RegisterAsync(
                "", password, "John", "Doe");

            // Assert
            Assert.False(success);
            Assert.Null(userId);
        }

        [Fact]
        public async Task RegisterAsync_PasswordTooShort_ReturnsError()
        {
            // Arrange
            var email = "user@example.com";
            var shortPassword = "short"; // Less than 6 characters

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Act
            var (success, message, userId) = await _authenticationService.RegisterAsync(
                email, shortPassword, "John", "Doe");

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Contains("6 characters", message);
        }

        [Fact]
        public async Task RegisterAsync_EmptyPassword_ReturnsError()
        {
            // Arrange
            var email = "user@example.com";

            // Act
            var (success, message, userId) = await _authenticationService.RegisterAsync(
                email, "", "John", "Doe");

            // Assert
            Assert.False(success);
            Assert.Null(userId);
        }

        [Fact]
        public async Task RegisterAsync_WithPhoneAndAddress_StoresAdditionalInfo()
        {
            // Arrange
            var email = "user@example.com";
            var password = "SecurePass123";
            var firstName = "John";
            var lastName = "Doe";
            var phone = "1234567890";
            var address = "123 Main St, City, State 12345";
            var userId = 42;

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync((User)null);

            _mockUserRepository
                .Setup(r => r.CreateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(userId);

            // Act
            var (success, message, returnedUserId) = await _authenticationService.RegisterAsync(
                email, password, firstName, lastName, phone, address);

            // Assert
            Assert.True(success);
            Assert.Equal(userId, returnedUserId);
            _mockUserRepository.Verify(
                r => r.CreateUserAsync(email, It.IsAny<string>(), firstName, lastName, phone, address, "Client"),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUserInfo()
        {
            // Arrange
            var email = "user@example.com";
            var password = "SecurePass123";
            var userId = 42;
            var role = "Client";

            var user = new User
            {
                UserId = userId,
                Email = email,
                FirstName = "John",
                Role = role,
                PasswordHash = HashPassword(password)
            };

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync(user);

            _mockUserRepository
                .Setup(r => r.UpdateLastLoginDateAsync(userId))
                .ReturnsAsync(true);

            // Act
            var (success, message, returnedUserId, returnedRole) = await _authenticationService.LoginAsync(email, password);

            // Assert
            Assert.True(success);
            Assert.Equal(userId, returnedUserId);
            Assert.Equal(role, returnedRole);
            Assert.Contains("successful", message.ToLower());
            _mockUserRepository.Verify(r => r.UpdateLastLoginDateAsync(userId), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsError()
        {
            // Arrange
            var email = "user@example.com";
            var correctPassword = "SecurePass123";
            var wrongPassword = "WrongPass123";

            var user = new User
            {
                UserId = 42,
                Email = email,
                FirstName = "John",
                Role = "Client",
                PasswordHash = HashPassword(correctPassword)
            };

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var (success, message, userId, role) = await _authenticationService.LoginAsync(email, wrongPassword);

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Null(role);
            Assert.Contains("invalid", message.ToLower());
            _mockUserRepository.Verify(r => r.UpdateLastLoginDateAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_NonExistentEmail_ReturnsError()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "SecurePass123";

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Act
            var (success, message, userId, role) = await _authenticationService.LoginAsync(email, password);

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Null(role);
            Assert.Contains("invalid", message.ToLower());
        }

        [Fact]
        public async Task LoginAsync_EmptyEmail_ReturnsError()
        {
            // Arrange
            var password = "SecurePass123";

            // Act
            var (success, message, userId, role) = await _authenticationService.LoginAsync("", password);

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Null(role);
            Assert.Contains("required", message.ToLower());
        }

        [Fact]
        public async Task LoginAsync_EmptyPassword_ReturnsError()
        {
            // Arrange
            var email = "user@example.com";

            // Act
            var (success, message, userId, role) = await _authenticationService.LoginAsync(email, "");

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Null(role);
            Assert.Contains("required", message.ToLower());
        }

        [Fact]
        public async Task LoginAsync_UpdatesLastLoginDate()
        {
            // Arrange
            var email = "user@example.com";
            var password = "SecurePass123";
            var userId = 42;

            var user = new User
            {
                UserId = userId,
                Email = email,
                FirstName = "John",
                Role = "Client",
                PasswordHash = HashPassword(password)
            };

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync(user);

            _mockUserRepository
                .Setup(r => r.UpdateLastLoginDateAsync(userId))
                .ReturnsAsync(true);

            // Act
            await _authenticationService.LoginAsync(email, password);

            // Assert
            _mockUserRepository.Verify(r => r.UpdateLastLoginDateAsync(userId), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_PasswordsAreHashed_NotStoredPlaintext()
        {
            // Arrange
            var email = "user@example.com";
            var password = "SecurePass123";
            var userId = 42;

            string capturedPasswordHash = null;

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync((User)null);

            _mockUserRepository
                .Setup(r => r.CreateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Callback<string, string, string, string, string, string, string>(
                    (e, ph, fn, ln, pn, da, r) => capturedPasswordHash = ph)
                .ReturnsAsync(userId);

            // Act
            await _authenticationService.RegisterAsync(email, password, "John", "Doe");

            // Assert
            Assert.NotNull(capturedPasswordHash);
            Assert.NotEqual(password, capturedPasswordHash); // Password not stored as plaintext
            Assert.NotEmpty(capturedPasswordHash);
        }

        [Fact]
        public async Task RegisterAsync_CreatesUserWithClientRole()
        {
            // Arrange
            var email = "user@example.com";
            var password = "SecurePass123";
            var userId = 42;

            string capturedRole = null;

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ReturnsAsync((User)null);

            _mockUserRepository
                .Setup(r => r.CreateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Callback<string, string, string, string, string, string, string>(
                    (e, ph, fn, ln, pn, da, r) => capturedRole = r)
                .ReturnsAsync(userId);

            // Act
            await _authenticationService.RegisterAsync(email, password, "John", "Doe");

            // Assert
            Assert.Equal("Client", capturedRole);
        }

        [Fact]
        public async Task RegisterAsync_Exception_ReturnsErrorMessage()
        {
            // Arrange
            var email = "user@example.com";
            var password = "SecurePass123";
            var errorMsg = "Database connection failed";

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ThrowsAsync(new Exception(errorMsg));

            // Act
            var (success, message, userId) = await _authenticationService.RegisterAsync(
                email, password, "John", "Doe");

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Contains("error", message.ToLower());
        }

        [Fact]
        public async Task LoginAsync_Exception_ReturnsErrorMessage()
        {
            // Arrange
            var email = "user@example.com";
            var password = "SecurePass123";
            var errorMsg = "Database timeout";

            _mockUserRepository
                .Setup(r => r.GetUserByEmailAsync(email))
                .ThrowsAsync(new Exception(errorMsg));

            // Act
            var (success, message, userId, role) = await _authenticationService.LoginAsync(email, password);

            // Assert
            Assert.False(success);
            Assert.Null(userId);
            Assert.Null(role);
            Assert.Contains("error", message.ToLower());
        }

        /// <summary>
        /// Helper method to hash password (mirrors AuthenticationService implementation)
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
