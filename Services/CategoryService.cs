using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Category>> GetCategoriesAsync(string? search = null)
        {
            var categories = await _unitOfWork.Categories.GetAllWithProductsAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                return categories.Where(c => c.Name.ToLower().Contains(s)).ToList();
            }
            return categories.ToList();
        }

        public async Task<Category?> GetByIdAsync(Guid id)
        {
            return await _unitOfWork.Categories.GetCategoryWithProductsAsync(id);
        }

        public async Task<ServiceResult> CreateAsync(Category category)
        {
            category.Id = Guid.NewGuid();
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Ok($"Category '{category.Name}' was created successfully.");
        }

        public async Task<ServiceResult> UpdateAsync(Guid id, Category category)
        {
            try
            {
                var existing = await _unitOfWork.Categories.GetByIdAsync(id);
                if (existing == null)
                {
                    return ServiceResult.Fail("Category not found.");
                }

                existing.Name = category.Name;
                existing.Description = category.Description;

                _unitOfWork.Categories.Update(existing);
                await _unitOfWork.SaveChangesAsync();
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
                var category = await _unitOfWork.Categories.GetCategoryWithProductsAsync(id);
                if (category == null)
                {
                    return ServiceResult.Fail("Category not found.");
                }

                if (category.Products.Any())
                {
                    return ServiceResult.Fail("Cannot delete this category because it has products assigned to it.");
                }

                var name = category.Name;
                _unitOfWork.Categories.Remove(category);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult.Ok($"Category '{name}' was deleted successfully.");
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Fail("Cannot delete this category because it has products assigned to it.");
            }
        }
    }
}
