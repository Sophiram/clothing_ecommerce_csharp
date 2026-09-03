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

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,HexCode")] Color color)
        {
            ModelState.Remove("Variants");
            if (ModelState.IsValid)
            {
                color.Id = Guid.NewGuid();
                _context.Add(color);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(color);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var color = await _context.Colors.FindAsync(id);
            if (color == null) return NotFound();
            return View(color);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,HexCode")] Color color)
        {
            if (id != color.Id) return NotFound();

            ModelState.Remove("Variants");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(color);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ColorExists(color.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(color);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var color = await _context.Colors
                .Include(c => c.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (color == null) return NotFound();
            return View(color);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var color = await _context.Colors.FindAsync(id);
            if (color != null) _context.Colors.Remove(color);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ColorExists(Guid id) => _context.Colors.Any(e => e.Id == id);
    }
}