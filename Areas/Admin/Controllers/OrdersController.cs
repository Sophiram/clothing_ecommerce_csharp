using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    // GET: Admin/Orders
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();

        return View(orders);
    }

    // GET: Admin/Orders/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Address)
            .Include(o => o.Payment)
            .Include(o => o.Shipment)
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Color)
            .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Size)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    // GET: Admin/Orders/Create
    public async Task<IActionResult> Create()
    {
        var customers = await _context.Customers.AsNoTracking().ToListAsync();
        var addresses = await _context.Addresses.AsNoTracking().ToListAsync();

        // បង្កើត SelectList ដោយមាន Safety Check
        ViewBag.CustomerId = new SelectList(
            customers.Select(c => new { Id = c.Id, Name = $"{c.FirstName} {c.LastName}".Trim() }),
            "Id",
            "Name"
        );

        ViewBag.AddressId = new SelectList(
            addresses.Select(a => new { Id = a.Id, Text = a.Id.ToString() }),
            "Id",
            "Text"
        );

        return View();
    }

    // POST: Admin/Orders/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerId,AddressId,Status,TotalAmount")] Order order)
    {
        ModelState.Remove("Customer");
        ModelState.Remove("Address");
        ModelState.Remove("Items");

        if (ModelState.IsValid)
        {
            order.Id = Guid.NewGuid();
            order.OrderDate = DateTime.UtcNow;
            _context.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 💡 ត្រូវតែ reload ViewBag ឡើងវិញពេល Submit Form មិនឆ្លង (Validation Fail)
        var customers = await _context.Customers.AsNoTracking().ToListAsync();
        var addresses = await _context.Addresses.AsNoTracking().ToListAsync();

        ViewBag.CustomerId = new SelectList(
            customers.Select(c => new { Id = c.Id, Name = $"{c.FirstName} {c.LastName}".Trim() }),
            "Id",
            "Name",
            order.CustomerId
        );

        ViewBag.AddressId = new SelectList(
            addresses.Select(a => new { Id = a.Id, Text = a.Id.ToString() }),
            "Id",
            "Text",
            order.AddressId
        );

        return View(order);
    }

    // GET: Admin/Orders/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        ViewData["StatusList"] = new SelectList(Enum.GetValues<OrderStatus>(), order.Status);
        return View(order);
    }

    // POST: Admin/Orders/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        try
        {
            _context.Update(order);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrderExists(order.Id)) return NotFound();
            else throw;
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Orders/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    // POST: Admin/Orders/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null) _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool OrderExists(Guid id) => _context.Orders.Any(e => e.Id == id);
}