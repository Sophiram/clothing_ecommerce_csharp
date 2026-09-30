using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClothingEcommerce.Client.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IApiClient _apiClient;

        public OrdersController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _apiClient.GetAsync<List<OrderDto>>("api/orders");
            return View(response?.Data ?? new List<OrderDto>());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var response = await _apiClient.GetAsync<OrderDto>($"api/orders/{id}");
            if (response == null || !response.Success || response.Data == null)
            {
                return NotFound();
            }

            return View(response.Data);
        }
    }
}
