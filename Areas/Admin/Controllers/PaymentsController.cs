using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PaymentsController : Controller
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            Guid? paymentMethodId)
        {
            var payments = _context.Payments
                .Include(p => p.Order)
                .Include(p => p.PaymentMethod)
                .AsNoTracking()
                .AsQueryable();

            // Search Order ID

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                payments = payments.Where(p =>
                    p.OrderId.ToString().Contains(search));
            }

            // Payment Status

            PaymentStatus? selectedStatus = null;

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<PaymentStatus>(
                    status,
                    true,
                    out var parsedStatus))
            {
                selectedStatus = parsedStatus;

                payments = payments.Where(p =>
                    p.PaymentStatus == parsedStatus);
            }

            // Payment Method

            if (paymentMethodId.HasValue)
            {
                payments = payments.Where(p =>
                    p.PaymentMethodId == paymentMethodId.Value);
            }

            ViewBag.Search = search;
            ViewBag.Status = selectedStatus;
            ViewBag.PaymentMethodId = paymentMethodId;

            // Status dropdown

            ViewBag.PaymentStatuses =
                Enum.GetValues<PaymentStatus>()
                    .Select(s => new SelectListItem
                    {
                        Text = s.ToString(),
                        Value = s.ToString(),
                        Selected = selectedStatus == s
                    })
                    .ToList();

            // Payment methods

            var paymentMethods = await _context.PaymentMethods
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            ViewBag.PaymentMethods =
                paymentMethods.Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                    Selected = paymentMethodId == x.Id
                }).ToList();

            var result = await payments
                .OrderByDescending(p => p.PaidAt)
                .ThenByDescending(p => p.Amount)
                .ToListAsync();

            return View(result);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        // =========================================================
        // CREATE GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadOrdersAsync();
            await LoadPaymentMethodsAsync();

            return View(new Payment
            {
                PaymentStatus = PaymentStatus.Pending
            });
        }

        // =========================================================
        // CREATE POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            var orderExists = await _context.Orders
                .AnyAsync(o => o.Id == payment.OrderId);

            if (!orderExists)
            {
                ModelState.AddModelError(
                    nameof(payment.OrderId),
                    "Selected order does not exist.");
            }

            var methodExists = await _context.PaymentMethods
                .AnyAsync(m =>
                    m.Id == payment.PaymentMethodId &&
                    m.IsActive);

            if (!methodExists)
            {
                ModelState.AddModelError(
                    nameof(payment.PaymentMethodId),
                    "Selected payment method is invalid or inactive.");
            }

            if (!ModelState.IsValid)
            {
                await LoadOrdersAsync(payment.OrderId);
                await LoadPaymentMethodsAsync(payment.PaymentMethodId);

                return View(payment);
            }

            payment.Id = Guid.NewGuid();

            if (payment.PaymentStatus == PaymentStatus.Paid ||
                payment.PaymentStatus == PaymentStatus.Completed)
            {
                payment.PaidAt ??= DateTime.UtcNow;
            }
            else
            {
                payment.PaidAt = null;
            }

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            await LoadOrdersAsync(payment.OrderId);
            await LoadPaymentMethodsAsync(payment.PaymentMethodId);

            return View(payment);
        }

        // =========================================================
        // EDIT POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            Payment payment)
        {
            if (id != payment.Id)
                return NotFound();

            var orderExists = await _context.Orders
                .AnyAsync(o => o.Id == payment.OrderId);

            if (!orderExists)
            {
                ModelState.AddModelError(
                    nameof(payment.OrderId),
                    "Selected order does not exist.");
            }

            var methodExists = await _context.PaymentMethods
                .AnyAsync(m =>
                    m.Id == payment.PaymentMethodId);

            if (!methodExists)
            {
                ModelState.AddModelError(
                    nameof(payment.PaymentMethodId),
                    "Selected payment method does not exist.");
            }

            if (!ModelState.IsValid)
            {
                await LoadOrdersAsync(payment.OrderId);
                await LoadPaymentMethodsAsync(payment.PaymentMethodId);

                return View(payment);
            }

            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingPayment == null)
                return NotFound();

            existingPayment.OrderId =
                payment.OrderId;

            existingPayment.PaymentMethodId =
                payment.PaymentMethodId;

            existingPayment.PaymentStatus =
                payment.PaymentStatus;

            existingPayment.Amount =
                payment.Amount;

            existingPayment.PaidAt =
                payment.PaidAt;

            if (existingPayment.PaymentStatus == PaymentStatus.Paid ||
                existingPayment.PaymentStatus == PaymentStatus.Completed)
            {
                existingPayment.PaidAt ??= DateTime.UtcNow;
            }
            else
            {
                existingPayment.PaidAt = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            return View(payment);
        }

        // =========================================================
        // DELETE POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            _context.Payments.Remove(payment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Payment deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // LOAD ORDERS
        // =========================================================

        private async Task LoadOrdersAsync(
            Guid? selectedOrderId = null)
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = new SelectList(
                orders,
                "Id",
                "Id",
                selectedOrderId);
        }

        // =========================================================
        // LOAD PAYMENT METHODS
        // =========================================================

        private async Task LoadPaymentMethodsAsync(
            Guid? selectedPaymentMethodId = null)
        {
            var methods = await _context.PaymentMethods
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            ViewBag.PaymentMethods = new SelectList(
                methods,
                "Id",
                "Name",
                selectedPaymentMethodId);
        }
    }
}