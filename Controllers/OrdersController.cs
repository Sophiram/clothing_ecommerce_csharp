using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

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
        public async Task<IActionResult> Index()
        {
            var customer = await GetCustomer();

            if (customer == null)
                return RedirectToAction("Index", "Home");

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.Id)

                .Include(o => o.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)

                .Include(o => o.Payment)

                .Include(o => o.Shipment)

                .OrderByDescending(o => o.OrderDate)

                .ToListAsync();

            return View(orders);
        }

        // =========================================================
        // ORDER DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var customer = await GetCustomer();

            if (customer == null)
                return RedirectToAction("Index", "Home");

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
                return NotFound();

            return View(order);
        }
    }
}