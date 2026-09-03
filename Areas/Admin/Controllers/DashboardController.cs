using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),

                TotalProducts = await _context.Products.CountAsync(),

                TotalCustomers = await _context.Customers.CountAsync(),

                TotalRevenue =
                    await _context.Payments
                        .SumAsync(p => (decimal?)p.Amount) ?? 0,

                RecentOrders =
                    await _context.Orders
                        .Include(o => o.Customer)
                        .OrderByDescending(o => o.OrderDate)
                        .Take(5)
                        .ToListAsync()
            };

            return View(viewModel);
        }
    }
}