using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Categories
                .AsNoTracking()
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(c => c.Name.Contains(search));
            }

            ViewBag.Search = search;

            var result = await query.OrderBy(c => c.Name).ToListAsync();
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

            category.Id = Guid.NewGuid();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Category '{category.Name}' was created successfully." });
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

            try
            {
                var existing = await _context.Categories.FindAsync(id);

                if (existing == null)
                {
                    return NotFound(new { success = false, errors = new[] { "Category not found." } });
                }

                existing.Name = category.Name;
                existing.Description = category.Description;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Category '{existing.Name}' was updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { success = false, errors = new[] { ex.InnerException?.Message ?? ex.Message } });
            }
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound(new { success = false, errors = new[] { "Category not found." } });
                }

                var name = category.Name;
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Category '{name}' was deleted successfully." });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { success = false, errors = new[] { "Cannot delete this category because it has products assigned to it." } });
            }
        }
    }
}