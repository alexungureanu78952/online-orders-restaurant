namespace RestaurantOrderManagement.Data.Models
{
    public class ProductAllergen
    {
        public int ProductAllergenId { get; set; }
        public int ProductId { get; set; }
        public int AllergenId { get; set; }

        public Product Product { get; set; }
        public Allergen Allergen { get; set; }
    }
}
