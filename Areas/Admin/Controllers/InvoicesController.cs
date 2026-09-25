using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService _avatarService;
        private const int PageSize = 15;

        public InvoicesController(
            AppDbContext context,
            WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService avatarService)
        {
            _context = context;
            _avatarService = avatarService;
        }

        // =========================================================
        // GET: /Admin/Invoices
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            PaymentStatus? paymentStatus = null,
            OrderStatus? orderStatus = null,
            int page = 1)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Payment)
                    .ThenInclude(p => p!.PaymentMethod)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o =>
                    o.Id.ToString().ToLower().Contains(term) ||
                    (o.Customer != null && (
                        o.Customer.FirstName.ToLower().Contains(term) ||
                        o.Customer.LastName.ToLower().Contains(term) ||
                        o.Customer.Email.ToLower().Contains(term) ||
                        (o.Customer.Phone != null && o.Customer.Phone.Contains(term))
                    ))
                );
            }

            // Payment status filter
            if (paymentStatus.HasValue)
            {
                query = query.Where(o => o.Payment != null && o.Payment.PaymentStatus == paymentStatus.Value);
            }

            // Order status filter
            if (orderStatus.HasValue)
            {
                query = query.Where(o => o.Status == orderStatus.Value);
            }

            // Overall stats (unfiltered for KPI cards)
            var allOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Payment)
                .ToListAsync();

            ViewBag.TotalInvoiced = allOrders.Sum(o => o.TotalAmount);
            ViewBag.TotalCollected = allOrders
                .Where(o => o.Payment != null && (o.Payment.PaymentStatus == PaymentStatus.Paid || o.Payment.PaymentStatus == PaymentStatus.Completed))
                .Sum(o => o.TotalAmount);
            ViewBag.TotalPending = allOrders
                .Where(o => o.Payment == null || o.Payment.PaymentStatus == PaymentStatus.Pending)
                .Sum(o => o.TotalAmount);
            ViewBag.TotalCount = allOrders.Count;
            ViewBag.PaidCount = allOrders.Count(o => o.Payment != null && (o.Payment.PaymentStatus == PaymentStatus.Paid || o.Payment.PaymentStatus == PaymentStatus.Completed));
            ViewBag.PendingCount = allOrders.Count(o => o.Payment == null || o.Payment.PaymentStatus == PaymentStatus.Pending);

            // Filtered pagination
            var totalFiltered = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalFiltered / (double)PageSize);
            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var invoices = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.OrderStatus = orderStatus;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalFiltered = totalFiltered;

            return View(invoices);
        }

        // =========================================================
        // POST: /Admin/Invoices/UpdateStatus
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid orderId, PaymentStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order invoice was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (order.Payment == null)
            {
                var defaultMethod = await _context.PaymentMethods.FirstOrDefaultAsync(m => m.IsActive);
                order.Payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentMethodId = defaultMethod?.Id ?? Guid.Empty,
                    Amount = order.TotalAmount,
                    PaymentStatus = status,
                    PaidAt = (status == PaymentStatus.Paid || status == PaymentStatus.Completed) ? DateTime.UtcNow : null
                };
                _context.Payments.Add(order.Payment);
            }
            else
            {
                order.Payment.PaymentStatus = status;
                if (status == PaymentStatus.Paid || status == PaymentStatus.Completed)
                {
                    order.Payment.PaidAt ??= DateTime.UtcNow;
                }
                else
                {
                    order.Payment.PaidAt = null;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Invoice #{order.Id.ToString()[..8].ToUpper()} payment status updated to {status}.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // GET: /Admin/Invoices/Print/{id}
        // =========================================================
        [HttpGet]
        public IActionResult Print(Guid id)
        {
            return RedirectToAction("Receipt", "Orders", new { area = "Admin", id });
        }
    }
}
