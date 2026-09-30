using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Orders;
using Microsoft.AspNetCore.Mvc;

namespace ClothingEcommerce.Client.Controllers
{
    public class CartController : Controller
    {
        private readonly IApiClient _apiClient;

        public CartController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetAsync<CartDto>("api/cart");
            return View(response?.Data ?? new CartDto());
        }

        [HttpGet]
        public async Task<IActionResult> GetDrawerCart()
        {
            var response = await _apiClient.GetAsync<CartDto>("api/cart");
            return Json(response?.Data ?? new CartDto());
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequestDto request)
        {
            var response = await _apiClient.PostAsync<AddToCartRequestDto, object>("api/cart/items", request);
            return Json(response);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateQuantity(Guid id, [FromBody] UpdateCartItemRequestDto request)
        {
            var response = await _apiClient.PutAsync<UpdateCartItemRequestDto, object>($"api/cart/items/{id}", request);
            return Json(response);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveItem(Guid id)
        {
            var response = await _apiClient.DeleteAsync<object>($"api/cart/items/{id}");
            return Json(response);
        }
    }
}
