namespace RestaurantOrderManagement.Data.Models
{
    public class Configuration
    {
        public int ConfigId { get; set; }
        public decimal MinOrderForFreeShipping { get; set; } = 100;
        public decimal ShippingFee { get; set; } = 15;
        public decimal LargeOrderDiscountThreshold { get; set; } = 200;
        public decimal LargeOrderDiscountPercent { get; set; } = 10;
        public decimal MenuBundleDiscountPercent { get; set; } = 15;
        public int FrequentOrderThreshold { get; set; } = 5;
        public int FrequentOrderTimeWindow { get; set; } = 30;
        public decimal FrequentOrderDiscountPercent { get; set; } = 8;
        public int LowStockThreshold { get; set; } = 500;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}
