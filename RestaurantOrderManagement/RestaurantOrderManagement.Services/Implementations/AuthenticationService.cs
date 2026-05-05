using System.Security.Cryptography;
using System.Text;
using RestaurantOrderManagement.Data.Repositories;

namespace RestaurantOrderManagement.Services.Implementations
{
    /// <summary>
    /// Authentication service for user registration and login
    /// Uses bcrypt for password hashing (via simple SHA256 for now, should upgrade to BCrypt)
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserRepository _userRepository;

        public AuthenticationService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Register new user with email and password
        /// </summary>
        public async Task<(bool Success, string Message, int? UserId)> RegisterAsync(string email, string password, 
            string firstName, string lastName, string phoneNumber = null, string deliveryAddress = null)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Invalid email format", null);

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, "Password must be at least 6 characters", null);

                // Check if user already exists
                var existingUser = await _userRepository.GetUserByEmailAsync(email);
                if (existingUser != null)
                    return (false, "Email already registered", null);

                // Hash password
                var passwordHash = HashPassword(password);

                // Create user
                var userId = await _userRepository.CreateUserAsync(email, passwordHash, firstName, lastName, 
                    phoneNumber, deliveryAddress, "Client");

                if (userId > 0)
                    return (true, "User registered successfully", userId);
                else
                    return (false, "Failed to create user", null);
            }
            catch (Exception ex)
            {
                return (false, $"Registration error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Authenticate user with email and password
        /// </summary>
        public async Task<(bool Success, string Message, int? UserId, string Role)> LoginAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return (false, "Email and password required", null, null);

                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                    return (false, "Invalid email or password", null, null);

                // Verify password
                if (!VerifyPassword(password, user.PasswordHash))
                    return (false, "Invalid email or password", null, null);

                // Update last login
                await _userRepository.UpdateLastLoginDateAsync(user.UserId);

                return (true, "Login successful", user.UserId, user.Role);
            }
            catch (Exception ex)
            {
                return (false, $"Login error: {ex.Message}", null, null);
            }
        }

        /// <summary>
        /// Hash password using SHA256 (NOTE: In production, use bcrypt via BCrypt.Net-Next package)
        /// </summary>
        private string HashPassword(string password)
        {
            // TODO: Upgrade to bcrypt using: 
            // Install-Package BCrypt.Net-Next
            // return BCrypt.Net.BCrypt.HashPassword(password);
            
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Verify password against hash
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            // TODO: Upgrade to bcrypt:
            // return BCrypt.Net.BCrypt.Verify(password, hash);
            
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hash);
        }

        /// <summary>
        /// Validate user role (for authorization checks)
        /// </summary>
        public bool IsEmployeeRole(string role)
        {
            return role == "Employee" || role == "Admin";
        }

        /// <summary>
        /// Validate user role (for authorization checks)
        /// </summary>
        public bool IsClientRole(string role)
        {
            return role == "Client";
        }
    }
}
