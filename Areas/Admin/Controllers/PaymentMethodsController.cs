using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PaymentMethodsController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentMethodsController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.PaymentMethods
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.Name.Contains(search) ||
                    (x.Description != null &&
                     x.Description.Contains(search)));
            }

            var methods = await query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            ViewBag.Search = search;

            return View(methods);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var method = await _context.PaymentMethods
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (method == null)
                return NotFound();

            return View(method);
        }

        // =========================================================
        // CREATE GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PaymentMethod
            {
                IsActive = true,
                DisplayOrder = 0
            });
        }

        // =========================================================
        // CREATE POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentMethod model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Payment method name is required.");
            }

            var exists = await _context.PaymentMethods
                .AnyAsync(x => x.Name.ToLower() == model.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "This payment method already exists.");
            }

            if (!ModelState.IsValid)
                return View(model);

            model.Id = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;

            _context.PaymentMethods.Add(model);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment method created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var method = await _context.PaymentMethods
                .FirstOrDefaultAsync(x => x.Id == id);

            if (method == null)
                return NotFound();

            return View(method);
        }

        // =========================================================
        // EDIT POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            PaymentMethod model)
        {
            if (id != model.Id)
                return NotFound();

            model.Name = model.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Payment method name is required.");
            }

            var exists = await _context.PaymentMethods
                .AnyAsync(x =>
                    x.Id != id &&
                    x.Name.ToLower() == model.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Another payment method with this name already exists.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.PaymentMethods
                .FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null)
                return NotFound();

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Icon = model.Icon;
            existing.IsActive = model.IsActive;
            existing.DisplayOrder = model.DisplayOrder;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment method updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TOGGLE ACTIVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            var method = await _context.PaymentMethods
                .FirstOrDefaultAsync(x => x.Id == id);

            if (method == null)
                return NotFound();

            method.IsActive = !method.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = method.IsActive
                ? $"{method.Name} has been activated."
                : $"{method.Name} has been disabled.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var method = await _context.PaymentMethods
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (method == null)
                return NotFound();

            return View(method);
        }

        // =========================================================
        // DELETE POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var method = await _context.PaymentMethods
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (method == null)
                return NotFound();

            if (method.Payments.Any())
            {
                TempData["Error"] =
                    "This payment method cannot be deleted because it is already used by payments. Disable it instead.";

                return RedirectToAction(nameof(Index));
            }

            _context.PaymentMethods.Remove(method);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment method deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}