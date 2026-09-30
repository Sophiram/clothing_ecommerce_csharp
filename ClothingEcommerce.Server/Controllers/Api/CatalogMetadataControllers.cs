using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = c.Products.Count
                })
                .ToListAsync();

            return Ok(ApiResponse<List<CategoryDto>>.Ok(categories));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ProductCount = c.Products.Count
                })
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(ApiResponse<CategoryDto>.Fail("Category not found.", 404));
            }

            return Ok(ApiResponse<CategoryDto>.Ok(category));
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class BrandsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BrandsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetBrands()
        {
            var brands = await _context.Brands
                .AsNoTracking()
                .Select(b => new BrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    ProductCount = b.Products.Count
                })
                .ToListAsync();

            return Ok(ApiResponse<List<BrandDto>>.Ok(brands));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBrandById(Guid id)
        {
            var brand = await _context.Brands
                .AsNoTracking()
                .Select(b => new BrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    ProductCount = b.Products.Count
                })
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                return NotFound(ApiResponse<BrandDto>.Fail("Brand not found.", 404));
            }

            return Ok(ApiResponse<BrandDto>.Ok(brand));
        }
    }
}
