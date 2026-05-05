namespace RestaurantOrderManagement.Services.DTOs
{
    /// <summary>
    /// Data Transfer Object for Allergen
    /// Does NOT expose internal AllergenId to UI layer
    /// </summary>
    public class AllergenDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
