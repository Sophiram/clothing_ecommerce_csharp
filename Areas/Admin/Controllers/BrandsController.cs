using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class BrandsController : Controller
    {
        private readonly IBrandService _brandService;
        private readonly IWebHostEnvironment _environment;

        public BrandsController(IBrandService brandService, IWebHostEnvironment environment)
        {
            _brandService = brandService;
            _environment = environment;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index(string? search)
        {
            ViewBag.Search = search;
            var brands = await _brandService.GetBrandsAsync(search);
            return View(brands);
        }

        // =====================================================
        // DETAILS
        // =====================================================
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var brand = await _brandService.GetBrandDetailsAsync(id.Value);
            if (brand == null) return NotFound();

            return View(brand);
        }

        // =====================================================
        // CREATE - POST (AJAX)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Brand brand, IFormFile? logo, string? logoUrl)
        {
            ModelState.Remove("Products");
            ModelState.Remove("Logo");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _brandService.CreateBrandAsync(brand, logo, logoUrl, _environment.WebRootPath);
            if (!result.Success)
            {
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                brand = new
                {
                    id = result.Brand!.Id,
                    name = result.Brand.Name,
                    description = result.Brand.Description,
                    logo = result.Brand.Logo,
                    productCount = 0
                }
            });
        }

        // =====================================================
        // EDIT - POST (AJAX)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Description")] Brand brand, IFormFile? logo, string? logoUrl, bool removeLogo = false)
        {
            if (id != brand.Id)
                return BadRequest(new { success = false, errors = new[] { "Invalid brand ID." } });

            ModelState.Remove("Products");
            ModelState.Remove("Logo");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _brandService.UpdateBrandAsync(id, brand, logo, logoUrl, removeLogo, _environment.WebRootPath);
            if (!result.Success)
            {
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                brand = new
                {
                    id = result.Brand!.Id,
                    name = result.Brand.Name,
                    description = result.Brand.Description,
                    logo = result.Brand.Logo
                }
            });
        }

        // =====================================================
        // DELETE - POST (AJAX)
        // =====================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _brandService.DeleteBrandAsync(id, _environment.WebRootPath);
            if (!result.Success)
            {
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.Message });
        }
    }
}