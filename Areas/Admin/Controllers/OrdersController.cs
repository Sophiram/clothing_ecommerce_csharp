using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService _avatarService;

        public OrdersController(
            IOrderService orderService,
            WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService avatarService)
        {
            _orderService = orderService;
            _avatarService = avatarService;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index(string? search = null, OrderStatus? status = null)
        {
            var orders = await _orderService.GetAdminOrdersAsync(search, status);
            var allOrders = (string.IsNullOrWhiteSpace(search) && !status.HasValue) 
                ? orders 
                : await _orderService.GetAdminOrdersAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.TotalOrders = orders.Count;
            ViewBag.Statuses = Enum.GetValues<OrderStatus>();
            ViewBag.TotalCount = allOrders.Count;
            ViewBag.PendingCount = allOrders.Count(o => o.Status == OrderStatus.Pending);
            ViewBag.ProcessingCount = allOrders.Count(o => o.Status == OrderStatus.Processing);
            ViewBag.ShippedCount = allOrders.Count(o => o.Status == OrderStatus.Shipped);
            ViewBag.DeliveredCount = allOrders.Count(o => o.Status == OrderStatus.Delivered);
            ViewBag.CancelledCount = allOrders.Count(o => o.Status == OrderStatus.Cancelled);
            ViewBag.TotalRevenue = allOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var order = await _orderService.GetAdminOrderByIdAsync(id.Value);
            if (order == null) return NotFound();

            if (order.Customer != null && !string.IsNullOrEmpty(order.Customer.ApplicationUserId))
            {
                ViewBag.CustomerAvatarUrl = _avatarService.GetAvatarUrl(order.Customer.ApplicationUserId);
            }

            return View(order);
        }

        // GET: Admin/Orders/Receipt/5
        public async Task<IActionResult> Receipt(Guid? id)
        {
            if (id == null) return NotFound();

            var order = await _orderService.GetAdminOrderByIdAsync(id.Value);
            if (order == null) return NotFound();

            if (order.Customer != null && !string.IsNullOrEmpty(order.Customer.ApplicationUserId))
            {
                ViewBag.CustomerAvatarUrl = _avatarService.GetAvatarUrl(order.Customer.ApplicationUserId);
            }

            return View(order);
        }

        // GET: Admin/Orders/Create
        public async Task<IActionResult> Create()
        {
            await LoadOrderDropdownsAsync();
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
                var result = await _orderService.CreateAdminOrderAsync(order);
                if (result.Success)
                {
                    TempData["Success"] = result.SuccessMessage ?? "Order created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create order.");
            }

            await LoadOrderDropdownsAsync(order.CustomerId, order.AddressId);
            return View(order);
        }

        private async Task LoadOrderDropdownsAsync(Guid? customerId = null, Guid? addressId = null)
        {
            var (customers, addresses) = await _orderService.GetOrderCreateDropdownDataAsync();

            ViewBag.CustomerId = new SelectList(
                customers.Select(c => new { Id = c.Id, Name = $"{c.FirstName} {c.LastName}".Trim() }),
                "Id",
                "Name",
                customerId
            );

            ViewBag.AddressId = new SelectList(
                addresses.Select(a => new { Id = a.Id, Text = $"{a.Street}, {a.City}" }),
                "Id",
                "Text",
                addressId
            );
        }

        // GET: Admin/Orders/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var order = await _orderService.GetAdminOrderByIdAsync(id.Value);
            if (order == null) return NotFound();

            ViewData["StatusList"] = new SelectList(Enum.GetValues<OrderStatus>(), order.Status);
            return View(order);
        }

        // POST: Admin/Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, OrderStatus status)
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, status);
            if (!success) return NotFound();

            TempData["Success"] = "Order status updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Orders/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var order = await _orderService.GetAdminOrderByIdAsync(id.Value);
            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var success = await _orderService.DeleteOrderAsync(id);
            if (success)
            {
                TempData["Success"] = "Order deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}