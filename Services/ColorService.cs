using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ColorService : IColorService
    {
        private readonly AppDbContext _context;

        public ColorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Color>> GetAllAsync()
        {
            return await _context.Colors
                .Include(c => c.Variants)
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Color?> GetByIdAsync(Guid id)
        {
            return await _context.Colors
                .Include(c => c.Variants)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ServiceResult> CreateAsync(Color color)
        {
            color.Id = Guid.NewGuid();
            _context.Colors.Add(color);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Color '{color.Name}' was created successfully.");
        }

        public async Task<ServiceResult> UpdateAsync(Guid id, Color color)
        {
            var existing = await _context.Colors.FindAsync(id);
            if (existing == null)
            {
                return ServiceResult.Fail("Color not found.");
            }

            existing.Name = color.Name;
            existing.HexCode = color.HexCode;
            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Color '{color.Name}' was updated successfully.");
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            var color = await _context.Colors.FindAsync(id);
            if (color == null)
            {
                return ServiceResult.Fail("Color not found.");
            }

            var hasVariants = await _context.ProductVariants.AnyAsync(v => v.ColorId == id);
            if (hasVariants)
            {
                return ServiceResult.Fail("Cannot delete this color because it is used by product variants.");
            }

            var name = color.Name;
            _context.Colors.Remove(color);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Color '{name}' was deleted successfully.");
        }
    }
}
