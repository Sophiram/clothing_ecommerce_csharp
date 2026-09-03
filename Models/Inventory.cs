namespace WebApplication_ClothingEcommerce.Models
{
    public class Inventory
    {
        public Guid Id { get; set; }

        public Guid ProductVariantId { get; set; }

        public int Quantity { get; set; }

        public int ReservedQuantity { get; set; }

        public DateTime UpdatedAt { get; set; }

        public ProductVariant ProductVariant { get; set; } = null!;

        public int AvailableQuantity =>
            Math.Max(0, Quantity - ReservedQuantity);
    }
}