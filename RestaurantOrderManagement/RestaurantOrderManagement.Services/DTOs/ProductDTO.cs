namespace RestaurantOrderManagement.Services.DTOs
{
    
    public class ProductDTO
    {
        public string DisplayCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int PortionQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<AllergenDTO> Allergens { get; set; } = new List<AllergenDTO>();
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}
