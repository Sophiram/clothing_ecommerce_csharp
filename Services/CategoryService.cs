using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetCategoriesAsync(string? search = null)
        {
            var query = _context.Categories
                .AsNoTracking()
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(c => c.Name.Contains(s));
            }

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(Guid id)
        {
            return await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ServiceResult> CreateAsync(Category category)
        {
            category.Id = Guid.NewGuid();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok($"Category '{category.Name}' was created successfully.");
        }

        public async Task<ServiceResult> UpdateAsync(Guid id, Category category)
        {
            try
            {
                var existing = await _context.Categories.FindAsync(id);
                if (existing == null)
                {
                    return ServiceResult.Fail("Category not found.");
                }

                existing.Name = category.Name;
                existing.Description = category.Description;

                await _context.SaveChangesAsync();
                return ServiceResult.Ok($"Category '{existing.Name}' was updated successfully.");
            }
            catch (DbUpdateException ex)
            {
                return ServiceResult.Fail(ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return ServiceResult.Fail("Category not found.");
                }

                var name = category.Name;
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return ServiceResult.Ok($"Category '{name}' was deleted successfully.");
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Cannot delete this category because it has products assigned to it.");
            }
        }
    }
}
