using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface IReviewService
    {
        Task<List<Review>> GetAllReviewsAsync(string? search = null, int? minRating = null);
        Task<Review?> GetReviewByIdAsync(Guid id);
        Task<(bool Success, string Message)> AddOrUpdateReviewAsync(Guid customerId, Guid productId, int rating, string? comment);
        Task<bool> DeleteReviewAsync(Guid id, Guid? customerId = null, bool isStaff = false);
    }
}
