using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class HomeService : IHomeService
    {
        private readonly AppDbContext _context;

        public HomeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HomeIndexData> GetHomeIndexDataAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            var brands = await _context.Brands
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .Include(p => p.Reviews)
                .Where(p => p.Status == ProductStatus.Active)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            return new HomeIndexData
            {
                Categories = categories,
                Brands = brands,
                LatestProducts = products
            };
        }
    }
}
