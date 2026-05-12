namespace RestaurantOrderManagement.Services.DTOs
{
    
    public class CategoryDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
