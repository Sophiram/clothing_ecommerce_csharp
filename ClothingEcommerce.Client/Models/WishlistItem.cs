namespace WebApplication_ClothingEcommerce.Models
{
    public class WishlistItem
    {
        public Guid Id { get; set; }

        public Guid WishlistId { get; set; }

        public Guid VariantId { get; set; }

        public Wishlist Wishlist { get; set; } = null!;

        public ProductVariant Variant { get; set; } = null!;
    }
}
