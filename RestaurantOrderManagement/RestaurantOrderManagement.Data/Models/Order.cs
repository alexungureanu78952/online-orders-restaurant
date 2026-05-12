namespace RestaurantOrderManagement.Data.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "inregistrata";
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalCost { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; }
        public DateTime? ActualDeliveryTime { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? Notes { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
