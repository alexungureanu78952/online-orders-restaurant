namespace RestaurantOrderManagement.Services.DTOs
{
    public class RestaurantMenuItemDTO
    {
        public string OrderKey { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PortionDisplay { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public List<AllergenDTO> Allergens { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        public List<RestaurantMenuComponentDTO> Components { get; set; } = new();

        public string AvailabilityText => IsAvailable ? "disponibil" : "indisponibil";
        public string PriceText => $"{Price:F2} lei";
        public string PrimaryImageUrl => ImageUrls.Count > 0 ? ImageUrls[0] : string.Empty;
        public bool IsBundleMenu => string.Equals(ItemType, "Meniu", StringComparison.OrdinalIgnoreCase);
        public bool HasImages => ImageUrls.Count > 0;
        public bool HasAllergens => Allergens.Count > 0;
        public bool HasComponents => Components.Count > 0;
    }

    public class RestaurantMenuComponentDTO
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int PortionQuantity { get; set; }
        public bool IsAvailable { get; set; }

        public string QuantityDisplay => Quantity > 1
            ? $"{Quantity} x {PortionQuantity}g"
            : $"{PortionQuantity}g";
    }

    public class RestaurantMenuGroupDTO
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<RestaurantMenuItemDTO> Items { get; set; } = new();
        public int ItemCount => Items.Count;
    }
}
