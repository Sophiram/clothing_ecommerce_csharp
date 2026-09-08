using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int PageSize = 10;

        public OrdersController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Customer?> GetCustomer()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.Email == null)
                return null;

            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == user.Email);
        }

        // =========================================================
        // MY ORDERS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var customer = await GetCustomer();

            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            if (page < 1)
                page = 1;

            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.Id)
                .OrderByDescending(o => o.OrderDate);

            var totalOrders = await query.CountAsync();

            var orders = await query
                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)

                .Include(o => o.Payment)

                .Include(o => o.Shipment)

                .Skip((page - 1) * PageSize)
                .Take(PageSize)

                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = PageSize;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)PageSize);

            return View(orders);
        }

        // =========================================================
        // ORDER DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Invalid order.";
                return RedirectToAction("Index");
            }

            var customer = await GetCustomer();

            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders

                .AsNoTracking()

                .Include(o => o.Address)

                .Include(o => o.Payment)

                .Include(o => o.Shipment)

                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)

                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)

                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)

                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.CustomerId == customer.Id);

            if (order == null)
            {
                TempData["Error"] = "Order was not found.";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // =========================================================
        // CANCEL ORDER
        // Only orders that are still Pending can be cancelled by
        // the customer. NOTE: this assumes OrderStatus has a
        // "Cancelled" member — adjust the enum name below if yours
        // differs (e.g. Canceled, Voided).
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Invalid order.";
                return RedirectToAction("Index");
            }

            var customer = await GetCustomer();

            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.CustomerId == customer.Id);

            if (order == null)
            {
                TempData["Error"] = "Order was not found.";
                return RedirectToAction("Index");
            }

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] =
                    "Only pending orders can be cancelled.";

                return RedirectToAction("Details", new { id });
            }

            order.Status = OrderStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your order has been cancelled.";

            return RedirectToAction("Details", new { id });
        }
    }
}