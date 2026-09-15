using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(
            IReviewService reviewService,
            ICartService cartService,
            UserManager<ApplicationUser> userManager)
        {
            _reviewService = reviewService;
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
        // ADD OR UPDATE CUSTOMER REVIEW
        // POST: /Reviews/Add
        // =========================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid productId, int rating, string? comment)
        {
            if (productId == Guid.Empty)
            {
                return BadRequest("Invalid product id.");
            }

            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Challenge();
            }

            var (success, message) = await _reviewService.AddOrUpdateReviewAsync(customer.Id, productId, rating, comment);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success, message });
            }

            return RedirectToAction("Details", "Shop", new { id = productId });
        }

        // =========================================================
        // DELETE CUSTOMER REVIEW
        // POST: /Reviews/Delete
        // =========================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid productId)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid review id.");
            }

            var customer = await GetCurrentCustomerAsync();
            var isStaff = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            if (customer == null && !isStaff)
            {
                return Challenge();
            }

            var deleted = await _reviewService.DeleteReviewAsync(id, customer?.Id, isStaff);

            if (deleted)
            {
                TempData["Success"] = "Your review has been removed successfully.";
            }
            else
            {
                TempData["Error"] = "Review not found or you are not authorized to delete it.";
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = deleted, message = TempData["Success"] ?? TempData["Error"] });
            }

            return RedirectToAction("Details", "Shop", new { id = productId });
        }
    }
}
