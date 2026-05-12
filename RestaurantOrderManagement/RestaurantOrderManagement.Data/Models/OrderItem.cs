namespace RestaurantOrderManagement.Data.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int? ProductId { get; set; }
        public int? MenuId { get; set; }
        public string ItemType { get; set; } = "Preparat";
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ItemTotal { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Order Order { get; set; } = null!;
        public Product? Product { get; set; }
        public Menu? Menu { get; set; }

        public string DisplayName => !string.IsNullOrWhiteSpace(ItemName)
            ? ItemName
            : Product?.Name ?? Menu?.Name ?? "Item";
    }
}
// Order class moved to Order.cs
