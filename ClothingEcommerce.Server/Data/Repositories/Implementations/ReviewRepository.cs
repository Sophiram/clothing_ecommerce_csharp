using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        public ReviewRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Review>> GetReviewsByProductIdAsync(Guid productId)
        {
            return await _dbSet
                .Include(r => r.Customer)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetCustomerReviewAsync(Guid customerId, Guid productId)
        {
            return await _dbSet
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.ProductId == productId);
        }

        public async Task<IReadOnlyList<Review>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(r => r.Customer)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
