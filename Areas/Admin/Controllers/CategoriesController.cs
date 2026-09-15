using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            ViewBag.Search = search;
            var result = await _categoryService.GetCategoriesAsync(search);
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            ModelState.Remove(nameof(Category.Products));
            ModelState.Remove(nameof(Category.Id));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _categoryService.CreateAsync(category);
            return Ok(new { success = true, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Category category)
        {
            if (id != category.Id)
            {
                return BadRequest(new { success = false, errors = new[] { "Invalid category ID." } });
            }

            ModelState.Remove(nameof(Category.Products));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _categoryService.UpdateAsync(id, category);
            if (!result.Success)
            {
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.Message });
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _categoryService.DeleteAsync(id);
            if (!result.Success)
            {
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.Message });
        }
    }
}