using WebApplication_ClothingEcommerce.Data.Enums;

namespace WebApplication_ClothingEcommerce.Models
{
    public class Product
    {
        // =========================
        // PRIMARY KEY
        // =========================

        public Guid Id { get; set; }


        // =========================
        // FOREIGN KEYS
        // =========================

        public Guid CategoryId { get; set; }

        public Guid BrandId { get; set; }


        // =========================
        // PRODUCT INFORMATION
        // =========================

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string Material { get; set; } = string.Empty;


        // =========================
        // STATUS
        // =========================

        public ProductStatus Status { get; set; } = ProductStatus.Active;


        // =========================
        // AUDIT
        // =========================

        public DateTime CreatedAt { get; set; }

        public DateTime ModifiedAt { get; set; }


        // =========================
        // NAVIGATION
        // =========================

        public Category Category { get; set; } = null!;

        public Brand Brand { get; set; } = null!;


        // Product Images
        public ICollection<ProductImage> Images { get; set; }
            = new List<ProductImage>();


        // Product Variants
        public ICollection<ProductVariant> Variants { get; set; }
            = new List<ProductVariant>();


        // Product Reviews
        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();
    }
}