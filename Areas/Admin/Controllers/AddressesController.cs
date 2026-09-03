
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AddressesController : Controller
{
    private readonly AppDbContext _context;

    public AddressesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: ADDRESSS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Addresses.ToListAsync());
    }

    // GET: ADDRESSS/Details/5
    public async Task<IActionResult> Details(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var address = await _context.Addresses
            .FirstOrDefaultAsync(m => m.Id == id);
        if (address == null)
        {
            return NotFound();
        }

        return View(address);
    }

    // GET: ADDRESSS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ADDRESSS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,CustomerId,Province,City,Street,PostalCode,IsDefault,Customer")] Address address)
    {
        if (ModelState.IsValid)
        {
            _context.Add(address);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(address);
    }

    // GET: ADDRESSS/Edit/5
    public async Task<IActionResult> Edit(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var address = await _context.Addresses.FindAsync(id);
        if (address == null)
        {
            return NotFound();
        }
        return View(address);
    }

    // POST: ADDRESSS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(System.Guid? id, [Bind("Id,CustomerId,Province,City,Street,PostalCode,IsDefault,Customer")] Address address)
    {
        if (id != address.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(address);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AddressExists(address.Id))
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
        return View(address);
    }

    // GET: ADDRESSS/Delete/5
    public async Task<IActionResult> Delete(System.Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var address = await _context.Addresses
            .FirstOrDefaultAsync(m => m.Id == id);
        if (address == null)
        {
            return NotFound();
        }

        return View(address);
    }

    // POST: ADDRESSS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(System.Guid? id)
    {
        var address = await _context.Addresses.FindAsync(id);
        if (address != null)
        {
            _context.Addresses.Remove(address);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AddressExists(System.Guid? id)
    {
        return _context.Addresses.Any(e => e.Id == id);
    }
}
