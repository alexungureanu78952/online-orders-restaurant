namespace RestaurantOrderManagement.Data.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? DeliveryAddress { get; set; }
        public string Role { get; set; } = "Client";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
