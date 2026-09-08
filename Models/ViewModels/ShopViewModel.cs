using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class ShopViewModel
    {
        // ==========================================
        // RESULTS
        // ==========================================
        public IEnumerable<Product> Products { get; set; } = new List<Product>();

        // ==========================================
        // FILTER OPTIONS (for populating the sidebar)
        // ==========================================
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<Brand> Brands { get; set; } = new List<Brand>();

        // ==========================================
        // ACTIVE FILTER STATE
        // ==========================================
        public Guid? CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; }
        public bool? OnSale { get; set; }
    }
}
