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

        // GET: Admin/Sizes/Create
        public IActionResult Create() => View();

        // POST: Admin/Sizes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Size size)
        {
            ModelState.Remove("Variants");
            if (ModelState.IsValid)
            {
                size.Id = Guid.NewGuid();
                _context.Add(size);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(size);
        }

        // GET: Admin/Sizes/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var size = await _context.Sizes.FindAsync(id);
            if (size == null) return NotFound();
            return View(size);
        }

        // POST: Admin/Sizes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name")] Size size)
        {
            if (id != size.Id) return NotFound();

            ModelState.Remove("Variants");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(size);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SizeExists(size.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(size);
        }

        // GET: Admin/Sizes/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var size = await _context.Sizes
                .Include(s => s.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (size == null) return NotFound();
            return View(size);
        }

        // POST: Admin/Sizes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size != null) _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SizeExists(Guid id) => _context.Sizes.Any(e => e.Id == id);
    }
}