using System.Security.Cryptography;
using System.Text;
using RestaurantOrderManagement.Data.Repositories;

namespace RestaurantOrderManagement.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserRepository _userRepository;

        public AuthenticationService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        public async Task<(bool Success, string Message, int? UserId)> RegisterAsync(string email, string password,
            string firstName, string lastName, string? phoneNumber = null, string? deliveryAddress = null)
        {
            try
            {
                
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return (false, "Invalid email format", null);

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, "Password must be at least 6 characters", null);

                
                var existingUser = await _userRepository.GetUserByEmailAsync(email);
                if (existingUser != null)
                    return (false, "Email already registered", null);

                
                var passwordHash = HashPassword(password);

                
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

        public async Task<(bool Success, string Message, int? UserId, string? Role)> LoginAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return (false, "Email and password required", null, null);

                var user = await _userRepository.GetUserByEmailAsync(email);
                if (user == null)
                    return (false, "Invalid email or password", null, null);

                
                if (!VerifyPassword(password, user.PasswordHash))
                    return (false, "Invalid email or password", null, null);

                
                await _userRepository.UpdateLastLoginDateAsync(user.UserId);

                return (true, "Login successful", user.UserId, user.Role);
            }
            catch (Exception ex)
            {
                return (false, $"Login error: {ex.Message}", null, null);
            }
        }

        
        private string HashPassword(string password)
        {
            

            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        
        private bool VerifyPassword(string password, string hash)
        {
            

            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hash);
        }


        public bool IsEmployeeRole(string role)
        {
            return role == "Employee" || role == "Admin";
        }

        public bool IsClientRole(string role)
        {
            return role == "Client";
        }
    }
}
