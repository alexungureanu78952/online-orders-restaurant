namespace RestaurantOrderManagement.Data.Models
{
    public class Allergen
    {
        public int AllergenId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<ProductAllergen> ProductAllergens { get; set; } = new List<ProductAllergen>();
    }
}
