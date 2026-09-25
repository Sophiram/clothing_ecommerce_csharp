using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class BrandRepository : Repository<Brand>, IBrandRepository
    {
        public BrandRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Brand>> GetAllWithProductsAsync()
        {
            return await _dbSet
                .Include(b => b.Products)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Brand?> GetBrandWithProductsAsync(Guid id)
        {
            return await _dbSet
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
        {
            var query = _dbSet.Where(b => b.Name.ToLower() == name.ToLower());
            if (excludeId.HasValue)
            {
                query = query.Where(b => b.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }
    }
}
