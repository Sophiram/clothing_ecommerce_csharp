using ClothingEcommerce.Client.Services.ApiClient;
using ClothingEcommerce.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.ViewComponents
{
    public class CategoryNavViewComponent : ViewComponent
    {
        private readonly IApiClient _apiClient;

        public CategoryNavViewComponent(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var response = await _apiClient.GetAsync<List<CategoryDto>>("api/categories");
            var categories = response?.Data?.Select(c => new Category
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            }).ToList() ?? new List<Category>();

            return View(categories);
        }
    }
}
