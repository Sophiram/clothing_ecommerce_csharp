
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ShipmentsController : Controller
{
    private readonly AppDbContext _context;

    public ShipmentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: SHIPMENTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Shipments.ToListAsync());
    }

    // GET: SHIPMENTS/Details/5
    public async Task<IActionResult> Details(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(m => m.Id == id);
        if (shipment == null)
        {
            return NotFound();
        }

        return View(shipment);
    }

    // GET: SHIPMENTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SHIPMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,OrderId,TrackingNumber,ShippingCompany,ShipmentStatus,ShippedAt,DeliveredAt,Order")] Shipment shipment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(shipment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(shipment);
    }

    // GET: SHIPMENTS/Edit/5
    public async Task<IActionResult> Edit(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shipment = await _context.Shipments.FindAsync(id);
        if (shipment == null)
        {
            return NotFound();
        }
        return View(shipment);
    }

    // POST: SHIPMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(System.Guid? id, [Bind("Id,OrderId,TrackingNumber,ShippingCompany,ShipmentStatus,ShippedAt,DeliveredAt,Order")] Shipment shipment)
    {
        if (id != shipment.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(shipment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShipmentExists(shipment.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(shipment);
    }

    // GET: SHIPMENTS/Delete/5
    public async Task<IActionResult> Delete(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(m => m.Id == id);
        if (shipment == null)
        {
            return NotFound();
        }

        return View(shipment);
    }

    // POST: SHIPMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(System.Guid? id)
    {
        var shipment = await _context.Shipments.FindAsync(id);
        if (shipment != null)
        {
            _context.Shipments.Remove(shipment);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ShipmentExists(System.Guid? id)
    {
        return _context.Shipments.Any(e => e.Id == id);
    }
}
