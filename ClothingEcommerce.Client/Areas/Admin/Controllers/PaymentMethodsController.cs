using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class PaymentMethodsController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentMethodsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // =========================================================
        // INDEX
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var methods = await _paymentService.GetAllPaymentMethodsAsync(search);
            ViewBag.Search = search;
            return View(methods);
        }

        // =========================================================
        // DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var method = await _paymentService.GetPaymentMethodByIdAsync(id.Value);
            if (method == null) return NotFound();

            return View(method);
        }

        [HttpGet]
        public async Task<IActionResult> GetMethodJson(Guid id)
        {
            var method = await _paymentService.GetPaymentMethodByIdAsync(id);
            if (method == null) return NotFound();

            return Json(new
            {
                id = method.Id,
                name = method.Name,
                description = method.Description ?? string.Empty,
                icon = method.Icon ?? string.Empty,
                displayOrder = method.DisplayOrder,
                isActive = method.IsActive,
                paymentsCount = method.Payments?.Count ?? 0,
                createdAt = method.CreatedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm")
            });
        }

        // =========================================================
        // CREATE GET
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new PaymentMethod
            {
                IsActive = true,
                DisplayOrder = 0
            });
        }

        // =========================================================
        // CREATE POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentMethod model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Payment method name is required.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = await _paymentService.CreatePaymentMethodAsync(model);
            if (!result.Success)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(nameof(model.Name), err);
                }
                return View(model);
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

            var method = await _paymentService.GetPaymentMethodByIdAsync(id.Value);
            if (method == null) return NotFound();

            return View(method);
        }

        // =========================================================
        // EDIT POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PaymentMethod model)
        {
            if (id != model.Id) return NotFound();

            model.Name = model.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Payment method name is required.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = await _paymentService.UpdatePaymentMethodAsync(model);
            if (!result.Success)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(nameof(model.Name), err);
                }
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // TOGGLE ACTIVE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            var result = await _paymentService.TogglePaymentMethodStatusAsync(id);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var method = await _paymentService.GetPaymentMethodByIdAsync(id.Value);
            if (method == null) return NotFound();

            return View(method);
        }

        // =========================================================
        // DELETE POST
        // =========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var result = await _paymentService.DeletePaymentMethodAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}