using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    public class ShopController : Controller
    {
        private readonly IShopService _shopService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShopController(
            IShopService shopService,
            UserManager<ApplicationUser> userManager)
        {
            _shopService = shopService;
            _userManager = userManager;
        }

        // =====================================================
        // SHOP
        // GET: /Shop
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            Guid? categoryId,
            Guid? brandId,
            Guid? sizeId,
            Guid? colorId,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStock,
            bool? onSale,
            string? search,
            string? sort,
            int page = 1,
            int pageSize = 12,
            string viewMode = "grid")
        {
            string? userId = null;
            string? userEmail = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    userId = user.Id;
                    userEmail = user.Email;
                }
            }

            var filter = new ShopFilterParameters
            {
                CategoryId = categoryId,
                BrandId = brandId,
                SizeId = sizeId,
                ColorId = colorId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                InStock = inStock,
                OnSale = onSale,
                Search = search,
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                ViewMode = viewMode,
                UserId = userId,
                UserEmail = userEmail
            };

            var (model, wishlistedVariantIds) = await _shopService.GetShopViewModelAsync(filter);

            if (wishlistedVariantIds != null)
            {
                ViewData["WishlistedVariantIds"] = wishlistedVariantIds;
            }

            return View(model);
        }

        // =====================================================
        // PRODUCT DETAILS
        // GET: /Shop/Details/{id}
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            ProductDetailsResult result;
            if (id.HasValue && id.Value != Guid.Empty)
            {
                result = await _shopService.GetProductDetailsAsync(id.Value);
            }
            else
            {
                result = await _shopService.GetFirstProductDetailsAsync();
            }

            if (result.Product == null)
            {
                result = await _shopService.GetFirstProductDetailsAsync();
            }

            if (result.Product == null)
            {
                TempData["Error"] = "Product was not found or is no longer available.";
                return RedirectToAction("Index");
            }

            ViewBag.RelatedProducts = result.RelatedProducts;
            return View(result.Product);
        }
    }
}
