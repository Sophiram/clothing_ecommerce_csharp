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
        public IEnumerable<Size> Sizes { get; set; } = new List<Size>();
        public IEnumerable<Color> Colors { get; set; } = new List<Color>();

        // Category & Brand counts
        public Dictionary<Guid, int> CategoryCounts { get; set; } = new();
        public Dictionary<Guid, int> BrandCounts { get; set; } = new();

        // ==========================================
        // ACTIVE FILTER STATE
        // ==========================================
        public Guid? CategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? SizeId { get; set; }
        public Guid? ColorId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStock { get; set; }
        public bool? OnSale { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; }

        // Price range boundaries for range controls
        public decimal PriceMinBound { get; set; } = 0;
        public decimal PriceMaxBound { get; set; } = 500;

        // ==========================================
        // PAGINATION & VIEW MODE
        // ==========================================
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalProducts { get; set; } = 0;
        public string ViewMode { get; set; } = "grid"; // "grid" or "list"
    }
}
