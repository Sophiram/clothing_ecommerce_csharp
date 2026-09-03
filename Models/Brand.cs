namespace WebApplication_ClothingEcommerce.Models
{
    public class Brand
    {
        public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Brand logo image path / URL
        public string? Logo { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

}
