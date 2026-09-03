namespace WebApplication_ClothingEcommerce.Models
{
    public class Color
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string HexCode { get; set; } = string.Empty;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
