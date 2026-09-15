using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SizesController : Controller
    {
        private readonly ISizeService _sizeService;

        public SizesController(ISizeService sizeService)
        {
            _sizeService = sizeService;
        }

        // GET: Admin/Sizes
        public async Task<IActionResult> Index()
        {
            return View(await _sizeService.GetAllAsync());
        }

        // GET: Admin/Sizes/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var size = await _sizeService.GetByIdAsync(id.Value);
            if (size == null) return NotFound();

            return View(size);
        }

        // POST: Admin/Sizes/Create (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Size size)
        {
            ModelState.Remove("Variants");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _sizeService.CreateAsync(size);
            return Ok(new { success = true, message = result.Message });
        }

        // POST: Admin/Sizes/Edit/5 (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name")] Size size)
        {
            if (id != size.Id)
            {
                return BadRequest(new { success = false, errors = new[] { "Invalid size ID." } });
            }

            ModelState.Remove("Variants");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, errors });
            }

            var result = await _sizeService.UpdateAsync(id, size);
            if (!result.Success)
            {
                return NotFound(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.Message });
        }

        // POST: Admin/Sizes/Delete/5 (AJAX)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _sizeService.DeleteAsync(id);
            if (!result.Success)
            {
                return BadRequest(new { success = false, errors = result.Errors });
            }

            return Ok(new { success = true, message = result.Message });
        }
    }
}