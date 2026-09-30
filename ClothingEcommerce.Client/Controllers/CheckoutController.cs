using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Orders;
using ClothingEcommerce.Shared.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cartResponse = await _apiClient.GetAsync<CartDto>("api/cart");
            if (cartResponse?.Data == null || !cartResponse.Data.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Cart = cartResponse.Data;
            return View(new CheckoutRequestDto());
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] CheckoutRequestDto request)
        {
            var response = await _apiClient.PostAsync<CheckoutRequestDto, OrderDto>("api/orders/checkout", request);
            return Json(response);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateKhqr([FromBody] KhqrGenerateRequestDto request)
        {
            var response = await _apiClient.PostAsync<KhqrGenerateRequestDto, KhqrGenerateResponseDto>("api/payments/khqr/generate", request);
            return Json(response);
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
