using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        // =====================================================
        // HOME
        // GET: /
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _homeService.GetHomeIndexDataAsync();

            ViewBag.Categories = data.Categories;
            ViewBag.Brands = data.Brands;

            return View(data.LatestProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}