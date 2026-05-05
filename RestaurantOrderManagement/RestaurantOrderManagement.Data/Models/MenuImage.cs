namespace RestaurantOrderManagement.Data.Models
{
    public class MenuImage
    {
        public int MenuImageId { get; set; }
        public int MenuId { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Menu Menu { get; set; }
    }
}
