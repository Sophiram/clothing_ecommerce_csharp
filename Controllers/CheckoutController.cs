using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly IPaymentService _paymentService;
        private readonly IDeliveryService _deliveryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(
            IOrderService orderService,
            ICartService cartService,
            IPaymentService paymentService,
            IDeliveryService deliveryService,
            UserManager<ApplicationUser> userManager)
        {
            _orderService = orderService;
            _cartService = cartService;
            _paymentService = paymentService;
            _deliveryService = deliveryService;
            _userManager = userManager;
        }

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _cartService.GetCustomerByUserIdOrEmailAsync(user.Id, user.Email);
        }

        private async Task LoadPaymentMethodsAsync()
        {
            ViewBag.PaymentMethods = await _paymentService.GetActivePaymentMethodsAsync();
            ViewBag.DeliveryMethods = await _deliveryService.GetActiveDeliveryMethodsAsync();
            ViewBag.DeliveryBranches = await _deliveryService.GetBranchesByCarrierAsync("VETExpress");
            ViewBag.Provinces = await _deliveryService.GetProvincesAsync("VETExpress");
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches(string province)
        {
            var branches = await _deliveryService.GetBranchesByProvinceAsync(province, "VETExpress");
            return Json(branches.Select(b => new { id = b.Id, name = b.BranchName, address = b.Address, phone = b.Phone }));
        }

        // =========================================================
        // GET: /Checkout, /Checkout/Index, /Checkout/Checkout
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var model = await _orderService.PrepareCheckoutAsync(customer.Id);
            if (model == null || !model.Items.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            await LoadPaymentMethodsAsync();
            return View("Index", model);
        }

        [HttpGet]
        [ActionName("Checkout")]
        public async Task<IActionResult> CheckoutGet()
        {
            return await Index();
        }

        // =========================================================
        // POST: /Checkout, /Checkout/Index, /Checkout/Checkout
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                var prep = await _orderService.PrepareCheckoutAsync(customer.Id);
                if (prep != null)
                {
                    model.Items = prep.Items;
                    model.SubTotal = prep.SubTotal;
                    model.DeliveryFee = prep.DeliveryFee;
                    model.Total = prep.Total;
                }
                await LoadPaymentMethodsAsync();
                return View("Index", model);
            }

            var result = await _orderService.PlaceOrderAsync(customer.Id, model);

            if (!result.Success)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }

                var prep = await _orderService.PrepareCheckoutAsync(customer.Id);
                if (prep != null)
                {
                    model.Items = prep.Items;
                    model.SubTotal = prep.SubTotal;
                    model.DeliveryFee = prep.DeliveryFee;
                    model.Total = prep.Total;
                }
                await LoadPaymentMethodsAsync();
                return View("Index", model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Success", new { id = result.OrderId });
        }

        /// <summary>
        /// POST: /Checkout/CreateOrder
        /// Creates the initial order from checkout form via AJAX and returns the new orderId.
        /// Used by the dynamic Bakong KHQR checkout flow.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(CheckoutViewModel model)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Json(new { success = false, message = "Customer profile was not found." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
                return Json(new { success = false, message = string.Join(" ", errors), errors });
            }

            var result = await _orderService.PlaceOrderAsync(customer.Id, model);
            if (!result.Success)
            {
                return Json(new { success = false, message = string.Join(" ", result.Errors), errors = result.Errors });
            }

            return Json(new { success = true, orderId = result.OrderId, message = result.Message });
        }

        // =========================================================
        // GET: /Checkout/Success/{id}
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Success(Guid id)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = await _orderService.GetCustomerOrderByIdAsync(customer.Id, id);
            if (order == null)
            {
                return RedirectToAction("Index", "Orders");
            }

            return View("Success", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Checkout")]
        public async Task<IActionResult> CheckoutPost(CheckoutViewModel model)
        {
            return await Index(model);
        }
    }
}
