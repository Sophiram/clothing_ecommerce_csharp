
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class OrderItemsController : Controller
{
    private readonly IOrderService _orderService;

    public OrderItemsController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET: ORDERITEMS
    public async Task<IActionResult> Index()    
    {
        return View(await _orderService.GetAllOrderItemsAsync());
    }

    // GET: ORDERITEMS/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var orderitem = await _orderService.GetOrderItemByIdAsync(id.Value);
        if (orderitem == null)
        {
            return NotFound();
        }

        return View(orderitem);
    }

    // GET: ORDERITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ORDERITEMS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,OrderId,VariantId,Quantity,UnitPrice,Order,Variant")] OrderItem orderitem)
    {
        if (ModelState.IsValid)
        {
            await _orderService.CreateOrderItemAsync(orderitem);
            return RedirectToAction(nameof(Index));
        }
        return View(orderitem);
    }

    // GET: ORDERITEMS/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var orderitem = await _orderService.GetOrderItemByIdAsync(id.Value);
        if (orderitem == null)
        {
            return NotFound();
        }
        return View(orderitem);
    }

    // POST: ORDERITEMS/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,OrderId,VariantId,Quantity,UnitPrice,Order,Variant")] OrderItem orderitem)
    {
        if (id != orderitem.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var result = await _orderService.UpdateOrderItemAsync(id, orderitem);
            if (!result.Success)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
        return View(orderitem);
    }

    // GET: ORDERITEMS/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var orderitem = await _orderService.GetOrderItemByIdAsync(id.Value);
        if (orderitem == null)
        {
            return NotFound();
        }

        return View(orderitem);
    }

    // POST: ORDERITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _orderService.DeleteOrderItemAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
