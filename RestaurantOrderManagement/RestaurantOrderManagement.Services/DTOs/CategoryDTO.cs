namespace RestaurantOrderManagement.Services.DTOs
{
    /// <summary>
    /// Data Transfer Object for Category
    /// Does NOT expose internal CategoryId to UI layer
    /// </summary>
    public class CategoryDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
