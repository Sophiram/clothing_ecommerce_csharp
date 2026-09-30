namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;

        public IEnumerable<ProductVariant> Variants { get; set; }
            = new List<ProductVariant>();

        public IEnumerable<Review> Reviews { get; set; }
            = new List<Review>();

        public IEnumerable<ProductImage> Images { get; set; }
            = new List<ProductImage>();

        public ProductVariant? SelectedVariant { get; set; }
    }
}