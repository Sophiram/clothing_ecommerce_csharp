using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SizesController : Controller
    {
        private readonly AppDbContext _context;

        public SizesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Sizes
        public async Task<IActionResult> Index()
        {
            return View(await _context.Sizes.Include(s => s.Variants).AsNoTracking().ToListAsync());
        }

        // GET: Admin/Sizes/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var size = await _context.Sizes
                .Include(s => s.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

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

            size.Id = Guid.NewGuid();
            _context.Add(size);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Size '{size.Name}' was created successfully." });
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

            try
            {
                _context.Update(size);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SizeExists(size.Id))
                    return NotFound(new { success = false, errors = new[] { "Size not found." } });
                throw;
            }

            return Ok(new { success = true, message = $"Size '{size.Name}' was updated successfully." });
        }

        // POST: Admin/Sizes/Delete/5 (AJAX)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var size = await _context.Sizes.FindAsync(id);

            if (size == null)
            {
                return NotFound(new { success = false, errors = new[] { "Size not found." } });
            }

            try
            {
                var name = size.Name;
                _context.Sizes.Remove(size);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Size '{name}' was deleted successfully." });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { success = false, errors = new[] { "Cannot delete this size because it has variants assigned to it." } });
            }
        }

        private bool SizeExists(Guid id) => _context.Sizes.Any(e => e.Id == id);
    }
}