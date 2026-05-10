using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Context;
using RestaurantOrderManagement.Data.Models;

namespace RestaurantOrderManagement.Data.Repositories
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository(RestaurantDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get user by email using parameterized stored procedure sp_GetUserByEmail
        /// </summary>
        public virtual async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FromSqlRaw("EXEC dbo.sp_GetUserByEmail @Email = {0}", email)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Create new user using parameterized stored procedure sp_CreateUser
        /// </summary>
        public virtual async Task<int> CreateUserAsync(string email, string passwordHash, string firstName, string lastName,
            string phoneNumber = null, string deliveryAddress = null, string role = "Client")
        {
            var result = await ExecuteScalarStoredProcedureAsync(
                "dbo.sp_CreateUser",
                ("@Email", email),
                ("@PasswordHash", passwordHash),
                ("@FirstName", firstName),
                ("@LastName", lastName),
                ("@PhoneNumber", phoneNumber),
                ("@DeliveryAddress", deliveryAddress),
                ("@Role", role));
            return result != null ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Update last login date using parameterized stored procedure sp_UpdateLastLoginDate
        /// </summary>
        public virtual async Task<bool> UpdateLastLoginDateAsync(int userId)
        {
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_UpdateLastLoginDate @UserId = {0}",
                userId);
            return result > 0;
        }
    }
}
