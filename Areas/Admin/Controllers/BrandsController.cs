using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private readonly string[] _allowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".svg"
        };

        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public BrandsController(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // =====================================================
        // INDEX
        // =====================================================

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(b =>
                    b.Name.Contains(search) ||
                    (b.Description != null &&
                     b.Description.Contains(search)));
            }

            ViewBag.Search = search;

            var brands = await query
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View(brands);
        }


        // =====================================================
        // DETAILS
        // =====================================================

        public async Task<IActionResult> Details(Guid? id)
                {
                    if (id == null)
                        return NotFound();

                    var brand = await _context.Brands
                        .Include(b => b.Products)
                            .ThenInclude(p => p.Category)
                        .Include(b => b.Products)
                            .ThenInclude(p => p.Variants)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Id == id);

                    if (brand == null)
                        return NotFound();

                    return View(brand);
                }



        // =====================================================
        // CREATE - GET
        // =====================================================

        public IActionResult Create()
        {
            return View();
        }


        // =====================================================
        // CREATE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description")] Brand brand,
            IFormFile? logo)
        {
            ModelState.Remove("Products");
            ModelState.Remove("Logo");

            ValidateLogo(logo);

            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            brand.Id = Guid.NewGuid();

            if (logo != null && logo.Length > 0)
            {
                brand.Logo = await SaveLogoAsync(logo);
            }

            _context.Brands.Add(brand);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Brand created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // EDIT - GET
        // =====================================================

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var brand = await _context.Brands
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
                return NotFound();

            return View(brand);
        }


        // =====================================================
        // EDIT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind("Id,Name,Description,Logo")] Brand brand,
            IFormFile? logo)
        {
            if (id != brand.Id)
                return NotFound();

            ModelState.Remove("Products");
            ModelState.Remove("Logo");

            ValidateLogo(logo);

            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            var existingBrand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id);

            if (existingBrand == null)
                return NotFound();


            // Update normal fields
            existingBrand.Name = brand.Name;
            existingBrand.Description = brand.Description;


            // Replace logo
            if (logo != null && logo.Length > 0)
            {
                var oldLogo = existingBrand.Logo;

                existingBrand.Logo = await SaveLogoAsync(logo);

                DeleteLogo(oldLogo);
            }


            await _context.SaveChangesAsync();

            TempData["Success"] = "Brand updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // DELETE - GET
        // =====================================================

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var brand = await _context.Brands
                .Include(b => b.Products)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
                return NotFound();

            return View(brand);
        }


        // =====================================================
        // DELETE - POST
        // =====================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
                return NotFound();


            // Prevent deleting brand if products exist
            var hasProducts = await _context.Products
                .AnyAsync(p => p.BrandId == id);

            if (hasProducts)
            {
                TempData["Error"] =
                    "This brand cannot be deleted because it has products.";

                return RedirectToAction(nameof(Index));
            }


            // Delete logo from wwwroot
            DeleteLogo(brand.Logo);

            _context.Brands.Remove(brand);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Brand deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // VALIDATE LOGO
        // =====================================================

        private void ValidateLogo(IFormFile? logo)
        {
            if (logo == null || logo.Length == 0)
                return;


            if (logo.Length > MaxFileSize)
            {
                ModelState.AddModelError(
                    "Logo",
                    "Logo image must be smaller than 5 MB.");

                return;
            }


            var extension =
                Path.GetExtension(logo.FileName)
                    .ToLowerInvariant();


            if (!_allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "Logo",
                    "Only JPG, JPEG, PNG, and WEBP images are allowed.");
            }
        }


        // =====================================================
        // SAVE LOGO
        // =====================================================

        private async Task<string> SaveLogoAsync(IFormFile logo)
        {
            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "brands");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }


            var extension =
                Path.GetExtension(logo.FileName)
                    .ToLowerInvariant();


            var fileName =
                $"{Guid.NewGuid():N}{extension}";


            var filePath =
                Path.Combine(folder, fileName);


            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.Create);


            await logo.CopyToAsync(stream);


            return $"/images/brands/{fileName}";
        }


        // =====================================================
        // DELETE LOGO
        // =====================================================

        private void DeleteLogo(string? logoPath)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
                return;


            var fileName =
                Path.GetFileName(logoPath);


            var filePath =
                Path.Combine(
                    _environment.WebRootPath,
                    "images",
                    "brands",
                    fileName);


            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }


        // =====================================================
        // EXISTS
        // =====================================================

        private bool BrandExists(Guid id)
        {
            return _context.Brands
                .Any(e => e.Id == id);
        }
    }
}