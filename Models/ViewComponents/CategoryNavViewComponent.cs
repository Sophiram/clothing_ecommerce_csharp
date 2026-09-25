using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;

namespace WebApplication_ClothingEcommerce.ViewComponents
{
    public class CategoryNavViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public CategoryNavViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }
    }
}
