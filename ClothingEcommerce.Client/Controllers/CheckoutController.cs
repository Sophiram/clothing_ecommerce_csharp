using System.Security.Claims;
using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Orders;
using ClothingEcommerce.Shared.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace ClothingEcommerce.Client.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IApiClient _apiClient;

        public CheckoutController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        private async Task LoadPaymentMethodsAsync()
        {
            var response = await _apiClient.GetAsync<List<PaymentMethod>>("api/payments/methods");
            var methods = response?.Data ?? new List<PaymentMethod>();

            if (!methods.Any())
            {
                // Fallback default Cambodian payment options if none seeded
                methods = new List<PaymentMethod>
                {
                    new() { Id = Guid.NewGuid(), Name = "Bakong KHQR (All Banks)", IsActive = true, DisplayOrder = 1 },
                    new() { Id = Guid.NewGuid(), Name = "ABA Pay", IsActive = true, DisplayOrder = 2 },
                    new() { Id = Guid.NewGuid(), Name = "Cash on Delivery (COD)", IsActive = true, DisplayOrder = 3 }
                };
            }

            ViewBag.PaymentMethods = methods;
            ViewBag.DeliveryMethods = new List<object>();
            ViewBag.DeliveryBranches = new List<object>();
            ViewBag.Provinces = new List<string> { "រាជធានីភ្នំពេញ", "ខេត្តសៀមរាប", "ខេត្តបាត់ដំបង", "ខេត្តព្រះសីហនុ", "ខេត្តកំពង់ចាម" };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
            if (cartResponse?.Data == null || !cartResponse.Data.Items.Any())
            {
                TempData["Error"] = "Your shopping bag is empty.";
                return RedirectToAction("Index", "Cart");
            }

            var cartDto = cartResponse.Data;
            var subTotal = cartDto.Subtotal;
            var deliveryFee = subTotal >= 50.00m ? 0.00m : 5.00m;

            var fullName = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var firstName = fullName.Split(' ').FirstOrDefault() ?? "";
            var lastName = fullName.Contains(' ') ? fullName.Substring(fullName.IndexOf(' ') + 1) : "";

            var model = new CheckoutViewModel
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = "",
                Street = "",
                City = "Phnom Penh",
                Province = "រាជធានីភ្នំពេញ",
                SelectedProvince = "រាជធានីភ្នំពេញ",
                DeliveryType = "ExpressDelivery",
                CarrierCode = "VETExpress",
                SubTotal = subTotal,
                DeliveryFee = deliveryFee,
                Total = subTotal + deliveryFee,
                Items = cartDto.Items.Select(i => new CheckoutItem
                {
                    VariantId = i.VariantId ?? Guid.Empty,
                    ProductName = i.ProductName,
                    SKU = i.VariantSku ?? "",
                    Size = i.SizeName ?? "",
                    Color = i.ColorName ?? "",
                    ImageUrl = i.ProductImage ?? "",
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                }).ToList()
            };

            await LoadPaymentMethodsAsync();
            return View("Index", model);
        }

        [HttpGet]
        [ActionName("Checkout")]
        public async Task<IActionResult> CheckoutGet()
        {
            return await Index();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            await LoadPaymentMethodsAsync();

            var paymentMethods = ViewBag.PaymentMethods as List<PaymentMethod> ?? new List<PaymentMethod>();
            var selectedMethod = paymentMethods.FirstOrDefault(p => p.Id == model.PaymentMethodId);
            var methodName = selectedMethod?.Name ?? "KHQR";

            var request = new CheckoutRequestDto
            {
                FullName = $"{model.FirstName} {model.LastName}".Trim(),
                Email = model.Email,
                Phone = model.Phone,
                Address = !string.IsNullOrWhiteSpace(model.Street) ? model.Street : (model.SelectedProvince ?? "Phnom Penh"),
                City = !string.IsNullOrWhiteSpace(model.City) ? model.City : "Phnom Penh",
                PostalCode = model.PostalCode,
                Notes = model.DeliveryNote,
                PaymentMethod = methodName
            };

            var response = await _apiClient.PostAsync<CheckoutRequestDto, OrderDto>("api/orders/checkout", request);

            if (response == null || !response.Success || response.Data == null)
            {
                TempData["Error"] = response?.Message ?? "Failed to place your order. Please check your details and try again.";

                // Reload cart items for model
                var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
                if (cartResponse?.Data != null)
                {
                    model.Items = cartResponse.Data.Items.Select(i => new CheckoutItem
                    {
                        VariantId = i.VariantId ?? Guid.Empty,
                        ProductName = i.ProductName,
                        SKU = i.VariantSku ?? "",
                        Size = i.SizeName ?? "",
                        Color = i.ColorName ?? "",
                        ImageUrl = i.ProductImage ?? "",
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity
                    }).ToList();
                    model.SubTotal = cartResponse.Data.Subtotal;
                    model.DeliveryFee = model.SubTotal >= 50.00m ? 0.00m : 5.00m;
                    model.Total = model.SubTotal + model.DeliveryFee;
                }

                return View("Index", model);
            }

            TempData["Success"] = "Your order has been placed successfully!";
            return RedirectToAction("Details", "Orders", new { id = response.Data.Id });
        }

        [HttpPost]
        public async Task<IActionResult> GenerateKhqr([FromBody] KhqrGenerateRequestDto request)
        {
            var response = await _apiClient.PostAsync<KhqrGenerateRequestDto, KhqrGenerateResponseDto>("api/payments/khqr/generate", request);
            return Json(response?.Data ?? new KhqrGenerateResponseDto());
        }

        [HttpGet]
        public async Task<IActionResult> CheckPayment(string md5, bool simulate = false)
        {
            var endpoint = $"api/payments/khqr/status/{md5}?simulate={simulate}";
            var response = await _apiClient.GetAsync<PaymentStatusResponseDto>(endpoint);
            return Json(response?.Data ?? new PaymentStatusResponseDto());
        }
    }
}
