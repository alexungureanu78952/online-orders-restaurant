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
namespace RestaurantOrderManagement.Data.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "inregistrata";
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalCost { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
        public DateTime? ActualDeliveryTime { get; set; }
        public string DeliveryAddress { get; set; }
        public string Notes { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
