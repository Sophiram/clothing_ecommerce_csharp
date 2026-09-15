using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
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
            var (payments, stats) = await _paymentService.GetPaymentsAsync(search, status, paymentMethodId);

            PaymentStatus? selectedStatus = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            {
                selectedStatus = parsedStatus;
            }

            ViewBag.Search = search;
            ViewBag.Status = selectedStatus;
            ViewBag.PaymentMethodId = paymentMethodId;
            ViewBag.Stats = stats;

            ViewBag.PaymentStatuses = Enum.GetValues<PaymentStatus>()
                .Select(s => new SelectListItem
                {
                    Text = s.ToString(),
                    Value = s.ToString(),
                    Selected = selectedStatus == s
                })
                .ToList();

            var paymentMethods = await _paymentService.GetPaymentMethodsSelectListAsync();
            ViewBag.PaymentMethods = paymentMethods.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString(),
                Selected = paymentMethodId == x.Id
            }).ToList();

            return View(payments);
        }

        // =========================================================
        // DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var payment = await _paymentService.GetPaymentByIdAsync(id.Value);
            if (payment == null) return NotFound();

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
            if (!ModelState.IsValid)
            {
                await LoadOrdersAsync(payment.OrderId);
                await LoadPaymentMethodsAsync(payment.PaymentMethodId);
                return View(payment);
            }

            var result = await _paymentService.CreatePaymentAsync(payment);
            if (!result.Success)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }
                await LoadOrdersAsync(payment.OrderId);
                await LoadPaymentMethodsAsync(payment.PaymentMethodId);
                return View(payment);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var payment = await _paymentService.GetPaymentByIdAsync(id.Value);
            if (payment == null) return NotFound();

            await LoadOrdersAsync(payment.OrderId);
            await LoadPaymentMethodsAsync(payment.PaymentMethodId);

            return View(payment);
        }

        // =========================================================
        // EDIT POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Payment payment)
        {
            if (id != payment.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadOrdersAsync(payment.OrderId);
                await LoadPaymentMethodsAsync(payment.PaymentMethodId);
                return View(payment);
            }

            var result = await _paymentService.UpdatePaymentAsync(payment);
            if (!result.Success)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }
                await LoadOrdersAsync(payment.OrderId);
                await LoadPaymentMethodsAsync(payment.PaymentMethodId);
                return View(payment);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var payment = await _paymentService.GetPaymentByIdAsync(id.Value);
            if (payment == null) return NotFound();

            return View(payment);
        }

        // =========================================================
        // DELETE POST
        // =========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _paymentService.DeletePaymentAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadOrdersAsync(Guid? selectedOrderId = null)
        {
            var orders = await _paymentService.GetAllOrdersAsync();
            ViewBag.Orders = new SelectList(orders, "Id", "Id", selectedOrderId);
        }

        private async Task LoadPaymentMethodsAsync(Guid? selectedPaymentMethodId = null)
        {
            var methods = await _paymentService.GetActivePaymentMethodsAsync();
            ViewBag.PaymentMethods = new SelectList(methods, "Id", "Name", selectedPaymentMethodId);
        }
    }
}