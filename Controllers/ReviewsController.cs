using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                TempData["Error"] = "Product was not found.";
                return RedirectToAction("Index", "Shop");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return Challenge();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == user.Email || c.ApplicationUserId == user.Id);

            if (customer == null)
            {
                // Create customer record if missing
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = user.Id,
                    FirstName = user.FirstName ?? user.Email.Split('@')[0],
                    LastName = user.LastName ?? "",
                    Email = user.Email,
                    Phone = user.PhoneNumber ?? "",
                    Status = Data.Enums.CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Clamp rating between 1 and 5
            rating = Math.Clamp(rating, 1, 5);

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == customer.Id);

            if (existingReview != null)
            {
                existingReview.Rating = rating;
                existingReview.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
                existingReview.CreatedAt = DateTime.UtcNow;
                _context.Reviews.Update(existingReview);
                TempData["Success"] = "Your review has been updated successfully!";
            }
            else
            {
                var newReview = new Review
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    CustomerId = customer.Id,
                    Rating = rating,
                    Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Reviews.Add(newReview);
                TempData["Success"] = "Thank you! Your review has been posted.";
            }

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true, message = TempData["Success"] });
            }

            return RedirectToAction("Details", "Shop", new { id = productId });
        }
    }
}
