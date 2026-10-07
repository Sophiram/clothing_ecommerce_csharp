
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AddressesController : Controller
{
    private readonly ICustomerService _customerService;

    public AddressesController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // GET: ADDRESSES
    public async Task<IActionResult> Index()    
    {
        return View(await _customerService.GetAllAddressesAsync());
    }

    // GET: ADDRESSES/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var address = await _customerService.GetAddressByIdAsync(id.Value);
        if (address == null)
        {
            return NotFound();
        }

        return View(address);
    }

    // GET: ADDRESSES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ADDRESSES/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,CustomerId,Province,City,Street,PostalCode,IsDefault,Customer")] Address address)
    {
        if (ModelState.IsValid)
        {
            await _customerService.CreateAddressAsync(address);
            return RedirectToAction(nameof(Index));
        }
        return View(address);
    }

    // GET: ADDRESSES/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var address = await _customerService.GetAddressByIdAsync(id.Value);
        if (address == null)
        {
            return NotFound();
        }
        return View(address);
    }

    // POST: ADDRESSES/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,CustomerId,Province,City,Street,PostalCode,IsDefault,Customer")] Address address)
    {
        if (id != address.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var result = await _customerService.UpdateAddressAsync(id, address);
            if (!result.Success)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
        return View(address);
    }

    // GET: ADDRESSES/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var address = await _customerService.GetAddressByIdAsync(id.Value);
        if (address == null)
        {
            return NotFound();
        }

        return View(address);
    }

    // POST: ADDRESSES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _customerService.DeleteAddressAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
