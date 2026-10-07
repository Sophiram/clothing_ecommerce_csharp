using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<IReadOnlyList<Review>> GetReviewsByProductIdAsync(Guid productId);
        Task<Review?> GetCustomerReviewAsync(Guid customerId, Guid productId);
        Task<IReadOnlyList<Review>> GetAllWithDetailsAsync();
    }
}
