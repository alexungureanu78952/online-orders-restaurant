namespace RestaurantOrderManagement.Data.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ItemTotal { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}
// Order class moved to Order.cs
