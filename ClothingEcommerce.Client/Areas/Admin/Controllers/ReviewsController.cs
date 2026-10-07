using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IAuditService _auditService;

        public ReviewsController(IReviewService reviewService, IAuditService auditService)
        {
            _reviewService = reviewService;
            _auditService = auditService;
        }

        // =========================================================
        // GET: /Admin/Reviews
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, int? rating = null)
        {
            var reviews = await _reviewService.GetAllReviewsAsync(search, rating);

            ViewBag.Search = search;
            ViewBag.Rating = rating;
            ViewBag.TotalCount = reviews.Count;
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            ViewBag.FiveStarCount = reviews.Count(r => r.Rating == 5);
            ViewBag.CriticalCount = reviews.Count(r => r.Rating <= 2);

            return View(reviews);
        }

        // =========================================================
        // GET: /Admin/Reviews/Details/{id}
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var review = await _reviewService.GetReviewByIdAsync(id.Value);
            if (review == null) return NotFound();

            return View(review);
        }

        // =========================================================
        // POST: /Admin/Reviews/Delete/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null) return NotFound();

            var success = await _reviewService.DeleteReviewAsync(id, null, isStaff: true);
            if (success)
            {
                await _auditService.LogAsync(
                    User.Identity?.Name,
                    User.Identity?.Name,
                    "DeleteReview",
                    "Review",
                    id.ToString(),
                    $"Deleted review by {review.Customer?.Email} on product {review.Product?.Name}",
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["Success"] = "Review removed successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
