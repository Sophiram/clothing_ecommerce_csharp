using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ColorsController : Controller
    {
        private readonly AppDbContext _context;

        public ColorsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Colors.Include(c => c.Variants).AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var color = await _context.Colors
                .Include(c => c.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

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

            color.Id = Guid.NewGuid();
            _context.Add(color);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Color '{color.Name}' was created successfully." });
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

            try
            {
                _context.Update(color);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ColorExists(color.Id))
                    return NotFound(new { success = false, errors = new[] { "Color not found." } });
                throw;
            }

            return Ok(new { success = true, message = $"Color '{color.Name}' was updated successfully." });
        }

        // POST: Admin/Colors/Delete/5 (AJAX)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var color = await _context.Colors.FindAsync(id);

            if (color == null)
            {
                return NotFound(new { success = false, errors = new[] { "Color not found." } });
            }

            try
            {
                var name = color.Name;
                _context.Colors.Remove(color);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Color '{name}' was deleted successfully." });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { success = false, errors = new[] { "Cannot delete this color because it has variants assigned to it." } });
            }
        }

        private bool ColorExists(Guid id) => _context.Colors.Any(e => e.Id == id);
    }
}