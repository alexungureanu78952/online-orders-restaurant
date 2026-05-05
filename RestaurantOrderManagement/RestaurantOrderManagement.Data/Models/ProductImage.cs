namespace RestaurantOrderManagement.Data.Models
{
    public class ProductImage
    {
        public int ProductImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Product Product { get; set; }
    }
}
