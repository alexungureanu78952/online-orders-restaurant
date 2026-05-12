using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantOrderManagement.Data.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        [NotMapped]
        public string DisplayCode { get; set; } = string.Empty; // Customer-facing product code (e.g., "PIZZA-001"), never show ProductId
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int PortionQuantity { get; set; }
        public int TotalQuantity { get; set; } = 0;
        public bool IsAvailable { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        public Category Category { get; set; } = null!;
        public ICollection<ProductAllergen> ProductAllergens { get; set; } = new List<ProductAllergen>();
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<MenuProduct> MenuProducts { get; set; } = new List<MenuProduct>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
