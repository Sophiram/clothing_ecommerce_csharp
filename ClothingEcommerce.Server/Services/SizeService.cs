using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class SizeService : ISizeService
    {
        private readonly AppDbContext _context;

        public SizeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Size>> GetAllAsync()
        {
            return await _context.Sizes
                .Include(s => s.Variants)
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Size?> GetByIdAsync(Guid id)
        {
            return await _context.Sizes
                .Include(s => s.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ServiceResult> CreateAsync(Size size)
        {
            size.Id = Guid.NewGuid();
            _context.Sizes.Add(size);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Size '{size.Name}' was created successfully.");
        }

        public async Task<ServiceResult> UpdateAsync(Guid id, Size size)
        {
            var existing = await _context.Sizes.FindAsync(id);
            if (existing == null)
            {
                return ServiceResult.Fail("Size not found.");
            }

            existing.Name = size.Name;
            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Size '{size.Name}' was updated successfully.");
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return ServiceResult.Fail("Size not found.");
            }

            var hasVariants = await _context.ProductVariants.AnyAsync(v => v.SizeId == id);
            if (hasVariants)
            {
                return ServiceResult.Fail("Cannot delete this size because it is used by product variants.");
            }

            var name = size.Name;
            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Size '{name}' was deleted successfully.");
        }
    }
}
