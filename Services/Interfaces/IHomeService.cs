using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class HomeIndexData
    {
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public List<Product> LatestProducts { get; set; } = new();
    }

    public interface IHomeService
    {
        Task<HomeIndexData> GetHomeIndexDataAsync();
    }
}
