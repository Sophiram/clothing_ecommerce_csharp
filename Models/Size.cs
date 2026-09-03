namespace WebApplication_ClothingEcommerce.Models
{
    public class Size
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
