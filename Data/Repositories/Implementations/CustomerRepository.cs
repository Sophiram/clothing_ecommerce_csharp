using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Customer?> GetByUserIdAsync(string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<Customer?> GetByUserIdOrEmailAsync(string? userId, string? email)
        {
            if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(email))
                return null;

            return await _dbSet.FirstOrDefaultAsync(c =>
                (!string.IsNullOrEmpty(userId) && c.ApplicationUserId == userId) ||
                (!string.IsNullOrEmpty(email) && c.Email == email));
        }

        public async Task<Customer?> GetCustomerWithDetailsAsync(Guid customerId)
        {
            return await _dbSet
                .Include(c => c.Addresses)
                .Include(c => c.Orders)
                .Include(c => c.Cart)
                    .ThenInclude(cart => cart.Items)
                .Include(c => c.Wishlist)
                    .ThenInclude(w => w.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == customerId);
        }
    }
}
