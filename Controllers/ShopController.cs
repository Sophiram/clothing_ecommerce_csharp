using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;

        public ShopController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // SHOP
        // GET: /Shop
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            Guid? categoryId,
            Guid? brandId,
            string? search,
            string? sort,
            bool? onSale)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)
                .Where(p =>
                    p.Status == Data.Enums.ProductStatus.Active)
                .AsQueryable();


            // ==========================================
            // CATEGORY
            // ==========================================

            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryId == categoryId.Value);
            }


            // ==========================================
            // BRAND
            // ==========================================

            if (brandId.HasValue)
            {
                query = query.Where(p =>
                    p.BrandId == brandId.Value);
            }


            // ==========================================
            // ON SALE
            // ==========================================

            if (onSale == true)
            {
                query = query.Where(p =>
                    p.Variants.Any(v =>
                        v.CompareAtPrice != null &&
                        v.CompareAtPrice > v.Price));
            }


            // ==========================================
            // SEARCH
            // ==========================================

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null &&
                     p.Description.Contains(search)) ||
                    (p.Brand != null &&
                     p.Brand.Name.Contains(search)) ||
                    (p.Category != null &&
                     p.Category.Name.Contains(search)));
            }


            // ==========================================
            // SORT
            // ==========================================

            query = sort switch
            {
                "price-low" =>
                    query.OrderBy(p =>
                        p.Variants
                            .Select(v => (decimal?)v.Price)
                            .Min() ?? 0),

                "price-high" =>
                    query.OrderByDescending(p =>
                        p.Variants
                            .Select(v => (decimal?)v.Price)
                            .Max() ?? 0),

                "name" =>
                    query.OrderBy(p => p.Name),

                "oldest" =>
                    query.OrderBy(p => p.CreatedAt),

                _ =>
                    query.OrderByDescending(p => p.CreatedAt)
            };


            // ==========================================
            // VIEW MODEL
            // ==========================================

            var model = new ShopViewModel
            {
                Products = await query.ToListAsync(),

                Categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync(),

                Brands = await _context.Brands
                    .AsNoTracking()
                    .OrderBy(b => b.Name)
                    .ToListAsync(),

                CategoryId = categoryId,
                BrandId = brandId,
                Search = search,
                Sort = sort,
                OnSale = onSale
            };


            return View(model);
        }



        // =====================================================
        // PRODUCT DETAILS
        // GET: /Shop/Details/{id}
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (!id.HasValue || id.Value == Guid.Empty)
            {
                return RedirectToAction("Index");
            }

            var product = await _context.Products
                .AsNoTracking()

                .Include(p => p.Brand)

                .Include(p => p.Category)

                .Include(p => p.Images)

                .Include(p => p.Variants)
                    .ThenInclude(v => v.Size)

                .Include(p => p.Variants)
                    .ThenInclude(v => v.Color)

                .Include(p => p.Variants)
                    .ThenInclude(v => v.Inventory)

                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Customer)

                .FirstOrDefaultAsync(p =>
                    p.Id == id.Value &&
                    p.Status == Data.Enums.ProductStatus.Active);

            if (product == null)
            {
                TempData["Error"] = "Product was not found or is no longer available.";
                return RedirectToAction("Index");
            }

            return View(product);
        }
    }
}
