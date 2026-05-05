namespace RestaurantOrderManagement.Data.Models
{
    public class MenuProduct
    {
        public int MenuProductId { get; set; }
        public int MenuId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;

        public Menu Menu { get; set; }
        public Product Product { get; set; }
    }
}
