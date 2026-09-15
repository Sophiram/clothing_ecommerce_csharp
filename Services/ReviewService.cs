using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetAllReviewsAsync(string? search = null, int? minRating = null)
        {
            var query = _context.Reviews
                .AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Product)
                    .ThenInclude(p => p.Images)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(r =>
                    (r.Comment != null && r.Comment.Contains(search)) ||
                    r.Product.Name.Contains(search) ||
                    r.Customer.FirstName.Contains(search) ||
                    r.Customer.LastName.Contains(search) ||
                    r.Customer.Email.Contains(search)
                );
            }

            if (minRating.HasValue && minRating.Value > 0)
            {
                query = query.Where(r => r.Rating >= minRating.Value);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetReviewByIdAsync(Guid id)
        {
            return await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Product)
                    .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<(bool Success, string Message)> AddOrUpdateReviewAsync(Guid customerId, Guid productId, int rating, string? comment)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return (false, "Product not found.");

            rating = Math.Clamp(rating, 1, 5);

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == customerId);

            if (existingReview != null)
            {
                existingReview.Rating = rating;
                existingReview.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
                existingReview.CreatedAt = DateTime.UtcNow;
                _context.Reviews.Update(existingReview);
                await _context.SaveChangesAsync();
                return (true, "Your review has been updated successfully!");
            }

            var newReview = new Review
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                CustomerId = customerId,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Reviews.Add(newReview);
            await _context.SaveChangesAsync();

            return (true, "Thank you! Your review has been posted.");
        }

        public async Task<bool> DeleteReviewAsync(Guid id, Guid? customerId = null, bool isStaff = false)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return false;

            if (!isStaff && customerId.HasValue && review.CustomerId != customerId.Value)
            {
                return false;
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
