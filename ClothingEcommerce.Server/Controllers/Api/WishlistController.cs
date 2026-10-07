using System.Security.Claims;
using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWishlistService _wishlistService;

        public WishlistController(AppDbContext context, IWishlistService wishlistService)
        {
            _context = context;
            _wishlistService = wishlistService;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return null;

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);
            if (customer == null)
            {
                var user = await _context.Users.FindAsync(userId);
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = userId,
                    FirstName = user?.FirstName ?? "Customer",
                    LastName = user?.LastName ?? "",
                    Email = user?.Email ?? "",
                    Phone = user?.PhoneNumber ?? "",
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();
            }

            return customer;
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(ApiResponse.Fail("Unauthorized", 401));
            }

            var wishlist = await _wishlistService.GetOrCreateCustomerWishlistAsync(customer.Id);
            var dto = new WishlistDto
            {
                Id = wishlist.Id,
                CustomerId = customer.Id,
                Items = wishlist.Items.Where(i => i.Variant != null && i.Variant.Product != null).Select(i => new WishlistItemDto
                {
                    Id = i.Id,
                    VariantId = i.VariantId,
                    ProductId = i.Variant.ProductId,
                    ProductName = i.Variant.Product.Name,
                    ProductImage = i.Variant.Product.Images?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl
                                   ?? i.Variant.Product.Images?.FirstOrDefault()?.ImageUrl,
                    SizeName = i.Variant.Size?.Name,
                    ColorName = i.Variant.Color?.Name,
                    ColorHex = i.Variant.Color?.HexCode,
                    Price = i.Variant.Price,
                    Stock = i.Variant.Inventory?.AvailableQuantity ?? 0,
                    AddedAt = DateTime.UtcNow
                }).ToList()
            };

            return Ok(ApiResponse<WishlistDto>.Ok(dto));
        }

        [HttpGet("ids")]
        public async Task<IActionResult> GetWishlistedVariantIds()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Ok(ApiResponse<List<Guid>>.Ok(new List<Guid>()));
            }

            var wishlist = await _wishlistService.GetOrCreateCustomerWishlistAsync(customer.Id);
            var ids = wishlist.Items.Select(i => i.VariantId).ToList();
            return Ok(ApiResponse<List<Guid>>.Ok(ids));
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Ok(ApiResponse<int>.Ok(0));
            }

            var wishlist = await _wishlistService.GetOrCreateCustomerWishlistAsync(customer.Id);
            return Ok(ApiResponse<int>.Ok(wishlist.Items.Count));
        }

        [HttpPost("toggle")]
        public async Task<IActionResult> Toggle([FromBody] WishlistActionRequestDto req)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(ApiResponse.Fail("Unauthorized", 401));
            }

            var (success, isWishlisted, message) = await _wishlistService.ToggleWishlistAsync(customer.Id, req.VariantId);
            return Ok(ApiResponse<object>.Ok(new { success, isWishlisted, message }));
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] WishlistActionRequestDto req)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(ApiResponse.Fail("Unauthorized", 401));
            }

            var (success, message) = await _wishlistService.AddToWishlistAsync(customer.Id, req.VariantId);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }

            return Ok(ApiResponse.Ok(message));
        }

        [HttpPost("remove")]
        public async Task<IActionResult> Remove([FromBody] WishlistRemoveRequestDto req)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(ApiResponse.Fail("Unauthorized", 401));
            }

            var (success, message) = await _wishlistService.RemoveFromWishlistAsync(customer.Id, req.Id, req.VariantId);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }

            return Ok(ApiResponse.Ok(message));
        }

        [HttpPost("move-to-cart")]
        public async Task<IActionResult> MoveToCart([FromBody] WishlistMoveRequestDto req)
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(ApiResponse.Fail("Unauthorized", 401));
            }

            var (success, message) = await _wishlistService.MoveToCartAsync(customer.Id, req.WishlistItemId);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(message));
            }

            return Ok(ApiResponse.Ok(message));
        }

        [HttpPost("clear")]
        public async Task<IActionResult> Clear()
        {
            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
            {
                return Unauthorized(ApiResponse.Fail("Unauthorized", 401));
            }

            var success = await _wishlistService.ClearWishlistAsync(customer.Id);
            return Ok(ApiResponse.Ok("Wishlist cleared successfully."));
        }
    }
}
