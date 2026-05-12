using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Context;
using RestaurantOrderManagement.Data.Models;
using System.Data;

namespace RestaurantOrderManagement.Data.Repositories
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository(RestaurantDbContext context) : base(context)
        {
        }

        
        public virtual async Task<User?> GetUserByEmailAsync(string email)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "dbo.sp_GetUserByEmail";
                command.CommandType = CommandType.StoredProcedure;

                var emailParameter = command.CreateParameter();
                emailParameter.ParameterName = "@Email";
                emailParameter.Value = email;
                command.Parameters.Add(emailParameter);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return null;

                return new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                    DeliveryAddress = reader.IsDBNull(reader.GetOrdinal("DeliveryAddress")) ? null : reader.GetString(reader.GetOrdinal("DeliveryAddress")),
                    Role = reader.GetString(reader.GetOrdinal("Role")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    LastLoginDate = reader.IsDBNull(reader.GetOrdinal("LastLoginDate")) ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginDate"))
                };
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        
        public virtual async Task<int> CreateUserAsync(string email, string passwordHash, string firstName, string lastName,
            string? phoneNumber = null, string? deliveryAddress = null, string role = "Client")
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

        
        public virtual async Task<bool> UpdateLastLoginDateAsync(int userId)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_UpdateLastLoginDate @UserId = {0}",
                userId);
            return await _context.Users.AnyAsync(user => user.UserId == userId);
        }
    }
}
