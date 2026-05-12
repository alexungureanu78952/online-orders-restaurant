namespace RestaurantOrderManagement.Data.Models
{
    public class Menu
    {
        public int MenuId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BundleDiscountPercent { get; set; } = 0;
        public bool IsAvailable { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        public Category Category { get; set; } = null!;
        public ICollection<MenuProduct> MenuProducts { get; set; } = new List<MenuProduct>();
        public ICollection<MenuImage> MenuImages { get; set; } = new List<MenuImage>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
