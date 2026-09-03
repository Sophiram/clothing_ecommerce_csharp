using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class ShopViewModel
    {
        public IEnumerable<Product> Products { get; set; }
            = new List<Product>();

        public IEnumerable<Category> Categories { get; set; }
            = new List<Category>();

        public IEnumerable<Brand> Brands { get; set; }
            = new List<Brand>();

        public Guid? CategoryId { get; set; }

        public Guid? BrandId { get; set; }

        public string? Search { get; set; }

        public string? Sort { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 12;

        public int TotalProducts { get; set; }

        public int TotalPages =>
            PageSize <= 0
                ? 0
                : (int)Math.Ceiling(
                    TotalProducts / (double)PageSize
                );
    }
}