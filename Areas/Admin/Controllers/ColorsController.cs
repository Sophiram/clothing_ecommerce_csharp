using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ColorsController : Controller
    {
        private readonly IColorService _colorService;

        public ColorsController(IColorService colorService)
        {
            _colorService = colorService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _colorService.GetAllAsync());
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var color = await _colorService.GetByIdAsync(id.Value);
            if (color == null) return NotFound();

            return View(color);
        }

        // POST: Admin/Colors/Create (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,HexCode")] Color color)
        {
            ModelState.Remove("Variants");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _colorService.CreateAsync(color);
            return Ok(new { success = true, message = result.Message });
        }

        // POST: Admin/Colors/Edit/5 (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,HexCode")] Color color)
        {
            if (id != color.Id)
            {
                return BadRequest(new { success = false, errors = new[] { "Invalid color ID." } });
            }

            ModelState.Remove("Variants");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _colorService.UpdateAsync(id, color);
            if (!result.Success)
            {
                return NotFound(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.Message });
        }

        // POST: Admin/Colors/Delete/5 (AJAX)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _colorService.DeleteAsync(id);
            if (!result.Success)
            {
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.Message });
        }
    }
}