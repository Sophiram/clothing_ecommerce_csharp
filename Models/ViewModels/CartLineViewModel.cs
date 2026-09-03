namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class CartLineViewModel
    {
        public Guid CartItemId { get; set; }

        public Guid VariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public int Available { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;
    }
}