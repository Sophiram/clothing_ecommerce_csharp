using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ShopController : Controller
    {
        private readonly IShopService _shopService;

        public ShopController(IShopService shopService)
        {
            _shopService = shopService;
        }

        // GET: /Admin/Shop
        public async Task<IActionResult> Index(Guid? categoryId, Guid? brandId, string? search)
        {
            var (products, categories, brands) = await _shopService.GetAdminShopDataAsync(categoryId, brandId, search);

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            return View(products);
        }

        // GET: /Admin/Shop/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var product = await _shopService.GetAdminProductDetailsAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }
    }
}