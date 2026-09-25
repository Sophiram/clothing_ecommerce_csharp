using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int PageSize = 10;

        public OrdersController(
            IOrderService orderService,
            ICartService cartService,
            UserManager<ApplicationUser> userManager)
        {
            _orderService = orderService;
            _cartService = cartService;
            _userManager = userManager;
        }

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _cartService.GetCustomerByUserIdOrEmailAsync(user.Id, user.Email);
        }

        // =========================================================
        // MY ORDERS
        // GET: /Orders
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(OrderStatus? status, int page = 1)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _orderService.GetCustomerOrdersAsync(customer.Id, status, page, PageSize);

            ViewBag.Page = result.CurrentPage;
            ViewBag.PageSize = result.PageSize;
            ViewBag.TotalOrders = result.TotalCount;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.CurrentStatus = result.CurrentStatus;

            ViewBag.AllCount = result.AllCount;
            ViewBag.PendingCount = result.PendingCount;
            ViewBag.ProcessingCount = result.ProcessingCount;
            ViewBag.ShippedCount = result.ShippedCount;
            ViewBag.DeliveredCount = result.DeliveredCount;
            ViewBag.CancelledCount = result.CancelledCount;

            return View(result.Orders);
        }

        // =========================================================
        // ORDER DETAILS
        // GET: /Orders/Details/{id}
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Invalid order.";
                return RedirectToAction(nameof(Index));
            }

            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _orderService.GetCustomerOrderByIdAsync(customer.Id, id);
            if (order == null)
            {
                TempData["Error"] = "Order was not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        // =========================================================
        // ORDER RECEIPT
        // GET: /Orders/Receipt/{id}
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Receipt(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Invalid order.";
                return RedirectToAction(nameof(Index));
            }

            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _orderService.GetCustomerOrderByIdAsync(customer.Id, id);
            if (order == null)
            {
                TempData["Error"] = "Order was not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        // =========================================================
        // CANCEL ORDER
        // POST: /Orders/Cancel/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Invalid order.";
                return RedirectToAction(nameof(Index));
            }

            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                TempData["Error"] = "Customer profile was not found.";
                return RedirectToAction("Index", "Home");
            }

            var (success, message) = await _orderService.CancelCustomerOrderAsync(customer.Id, id);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}