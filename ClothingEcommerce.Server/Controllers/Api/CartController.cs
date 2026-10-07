using System.Security.Claims;
using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Ok(ApiResponse<CartDto>.Ok(new CartDto()));
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Size)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Color)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                        .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(c => c.Customer != null && c.Customer.ApplicationUserId == userId);

            if (cart == null)
            {
                return Ok(ApiResponse<CartDto>.Ok(new CartDto()));
            }

            var dto = new CartDto
            {
                Id = cart.Id,
                UserId = userId,
                ShippingFee = 3.00m,
                Items = cart.Items.Where(i => i.Variant != null && i.Variant.Product != null).Select(item => new CartItemDto
                {
                    Id = item.Id,
                    ProductId = item.Variant.ProductId,
                    ProductName = item.Variant.Product.Name,
                    ProductImage = item.Variant.Product.Images.Where(img => img.IsPrimary).Select(img => img.ImageUrl).FirstOrDefault()
                                   ?? item.Variant.Product.Images.Select(img => img.ImageUrl).FirstOrDefault(),
                    VariantId = item.VariantId,
                    VariantSku = item.Variant.SKU,
                    SizeName = item.Variant.Size?.Name,
                    ColorName = item.Variant.Color?.Name,
                    UnitPrice = item.Variant.Price,
                    Quantity = item.Quantity,
                    MaxStock = item.Variant.Inventory != null ? item.Variant.Inventory.Quantity - item.Variant.Inventory.ReservedQuantity : 10
                }).ToList()
            };

            return Ok(ApiResponse<CartDto>.Ok(dto));
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequestDto request)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail("Please login to add items to your cart.", 401));
            }

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

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id
                };
                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            // Find variant
            Guid variantId = Guid.Empty;
            if (request.VariantId.HasValue && request.VariantId != Guid.Empty)
            {
                var variantExists = await _context.ProductVariants.AnyAsync(v => v.Id == request.VariantId.Value);
                if (variantExists)
                {
                    variantId = request.VariantId.Value;
                }
            }

            if (variantId == Guid.Empty && request.ProductId.HasValue && request.ProductId != Guid.Empty)
            {
                var defaultVariant = await _context.ProductVariants
                    .Where(v => v.ProductId == request.ProductId.Value && v.Status == VariantStatus.Available)
                    .OrderByDescending(v => v.Inventory != null ? v.Inventory.Quantity - v.Inventory.ReservedQuantity : 0)
                    .FirstOrDefaultAsync()
                    ?? await _context.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == request.ProductId.Value);

                if (defaultVariant != null)
                {
                    variantId = defaultVariant.Id;
                }
            }

            if (variantId == Guid.Empty)
            {
                return NotFound(ApiResponse.Fail("No variant found for this product.", 404));
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == variantId);
            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    VariantId = variantId,
                    Quantity = request.Quantity
                };
                await _context.CartItems.AddAsync(newItem);
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Ok("Item added to cart successfully."));
        }

        [HttpPut("items/{itemId:guid}")]
        public async Task<IActionResult> UpdateItemQuantity(Guid itemId, [FromBody] UpdateCartItemRequestDto request)
        {
            var item = await _context.CartItems.FindAsync(itemId);
            if (item == null)
            {
                return NotFound(ApiResponse.Fail("Cart item not found.", 404));
            }

            if (request.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = request.Quantity;
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Ok("Cart updated successfully."));
        }

        [HttpDelete("items/{itemId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid itemId)
        {
            var item = await _context.CartItems.FindAsync(itemId);
            if (item == null)
            {
                return NotFound(ApiResponse.Fail("Cart item not found.", 404));
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Ok("Item removed from cart."));
        }
    }
}
