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
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(
            IOrderService orderService,
            ICartService cartService,
            IPaymentService paymentService,
            UserManager<ApplicationUser> userManager)
        {
            _orderService = orderService;
            _cartService = cartService;
            _paymentService = paymentService;
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
            return RedirectToAction("Details", "Orders", new { id = result.OrderId });
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
