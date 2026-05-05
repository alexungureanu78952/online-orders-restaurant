namespace RestaurantOrderManagement.Services.DTOs
{
    /// <summary>
    /// Data Transfer Object for Product display in menus
    /// Does NOT expose internal ProductId - uses DisplayCode instead
    /// Includes allergens for filtering and display
    /// </summary>
    public class ProductDTO
    {
        public string DisplayCode { get; set; } // Customer-facing code, never numeric ID
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int PortionQuantity { get; set; } // In grams
        public bool IsAvailable { get; set; }
        public string CategoryName { get; set; }
        public List<AllergenDTO> Allergens { get; set; } = new List<AllergenDTO>();
        public List<string> ImageUrls { get; set; } = new List<string>(); // First image is main/thumbnail
    }
}
