namespace RestaurantOrderManagement.Services
{
    public interface IAuthenticationService
    {
        Task<(bool Success, string Message, int? UserId)> RegisterAsync(string email, string password,
            string firstName, string lastName, string? phoneNumber = null, string? deliveryAddress = null);

        Task<(bool Success, string Message, int? UserId, string? Role)> LoginAsync(string email, string password);

        bool IsEmployeeRole(string role);
        bool IsClientRole(string role);
    }
}
