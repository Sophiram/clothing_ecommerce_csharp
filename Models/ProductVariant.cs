using WebApplication_ClothingEcommerce.Data.Enums;

namespace WebApplication_ClothingEcommerce.Models
{
    public class ProductVariant
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public Guid SizeId { get; set; }

        public Guid ColorId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public VariantStatus Status { get; set; }
            = VariantStatus.Available;

        public decimal Price { get; set; }

        // =========================
        // NAVIGATION PROPERTIES
        // =========================

        public Product? Product { get; set; }

        public Size? Size { get; set; }

        public Color? Color { get; set; }

        public Inventory? Inventory { get; set; }
    }
}