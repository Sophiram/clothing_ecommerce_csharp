using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;

namespace WebApplication_ClothingEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // HOME
        // GET: /
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ==========================================
            // CATEGORIES
            // ==========================================

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();


            // ==========================================
            // BRANDS
            // ==========================================

            var brands = await _context.Brands
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();


            // ==========================================
            // LATEST PRODUCTS
            // Reviews (star ratings) and Variants -> Inventory
            // (so quick "Add to Cart" can find an in-stock
            // variant without a second query per card).
            // ==========================================

            var products = await _context.Products
                .AsNoTracking()

                .Include(p => p.Brand)

                .Include(p => p.Category)

                .Include(p => p.Images)

                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)

                .Include(p => p.Reviews)

                .Where(p =>
                    p.Status == Data.Enums.ProductStatus.Active)

                .OrderByDescending(p => p.CreatedAt)

                .Take(8)

                .ToListAsync();


            // ==========================================
            // VIEWBAG
            // ==========================================

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}