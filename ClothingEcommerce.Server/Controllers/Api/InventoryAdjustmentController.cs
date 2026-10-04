using System.Security.Claims;
using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Catalog;
using ClothingEcommerce.Shared.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryAdjustmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InventoryAdjustmentController> _logger;

        public InventoryAdjustmentController(AppDbContext context, ILogger<InventoryAdjustmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string CurrentUserName => User.FindFirstValue(ClaimTypes.Name) 
            ?? User.FindFirstValue(ClaimTypes.Email) 
            ?? "Manager";

        // ==========================================
        // POST: /api/inventoryadjustment/adjust
        // Stock IN / OUT / Damaged / Return
        // ==========================================
        [Authorize(Roles = "SuperAdmin,Admin,Manager")]
        [HttpPost("adjust")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<StockMovementDto>.Fail(errors));
            }

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.Size)
                .Include(v => v.Color)
                .Include(v => v.Inventory)
                .FirstOrDefaultAsync(v => v.Id == request.ProductVariantId);

            if (variant == null)
            {
                return NotFound(ApiResponse<StockMovementDto>.Fail("Product variant not found."));
            }

            if (variant.Inventory == null)
            {
                variant.Inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variant.Id,
                    Quantity = 0,
                    ReservedQuantity = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Inventories.Add(variant.Inventory);
            }

            var prevQty = variant.Inventory.Quantity;
            int newQty = prevQty;
            var type = request.MovementType.Trim();

            if (type.Equals("StockIn", StringComparison.OrdinalIgnoreCase) || 
                type.Equals("Return", StringComparison.OrdinalIgnoreCase))
            {
                newQty = prevQty + request.Quantity;
            }
            else if (type.Equals("StockOut", StringComparison.OrdinalIgnoreCase) || 
                     type.Equals("Damaged", StringComparison.OrdinalIgnoreCase))
            {
                if (prevQty < request.Quantity)
                {
                    return BadRequest(ApiResponse<StockMovementDto>.Fail(
                        $"Cannot deduct {request.Quantity} units. Current stock is {prevQty}."));
                }
                newQty = prevQty - request.Quantity;
            }
            else if (type.Equals("AuditCorrection", StringComparison.OrdinalIgnoreCase))
            {
                newQty = request.Quantity; // For audit correction, request.Quantity is the counted physical quantity
            }
            else
            {
                return BadRequest(ApiResponse<StockMovementDto>.Fail($"Unsupported movement type: {request.MovementType}"));
            }

            variant.Inventory.Quantity = newQty;
            variant.Inventory.UpdatedAt = DateTime.UtcNow;

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductVariantId = variant.Id,
                MovementType = type,
                Quantity = request.Quantity,
                PreviousQuantity = prevQty,
                NewQuantity = newQty,
                Reason = request.Reason,
                Reference = request.Reference,
                CreatedBy = CurrentUserName,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();

            var dto = new StockMovementDto
            {
                Id = movement.Id,
                ProductVariantId = variant.Id,
                ProductName = variant.Product?.Name ?? "Product",
                VariantSku = variant.SKU,
                SizeName = variant.Size?.Name ?? "",
                ColorName = variant.Color?.Name ?? "",
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                PreviousQuantity = movement.PreviousQuantity,
                NewQuantity = movement.NewQuantity,
                Reason = movement.Reason,
                Reference = movement.Reference,
                CreatedBy = movement.CreatedBy,
                CreatedAt = movement.CreatedAt
            };

            return Ok(ApiResponse<StockMovementDto>.Ok(dto, "Stock adjusted successfully."));
        }

        // ==========================================
        // GET: /api/inventoryadjustment/movements
        // Stock movement audit history
        // ==========================================
        [Authorize(Roles = "SuperAdmin,Admin,Manager")]
        [HttpGet("movements")]
        public async Task<IActionResult> GetMovements([FromQuery] Guid? variantId = null, [FromQuery] string? type = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.StockMovements
                .AsNoTracking()
                .Include(sm => sm.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(sm => sm.ProductVariant)
                    .ThenInclude(v => v.Size)
                .Include(sm => sm.ProductVariant)
                    .ThenInclude(v => v.Color)
                .AsQueryable();

            if (variantId.HasValue && variantId.Value != Guid.Empty)
            {
                query = query.Where(sm => sm.ProductVariantId == variantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(sm => sm.MovementType == type);
            }

            var totalCount = await query.CountAsync();
            var movements = await query
                .OrderByDescending(sm => sm.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sm => new StockMovementDto
                {
                    Id = sm.Id,
                    ProductVariantId = sm.ProductVariantId,
                    ProductName = sm.ProductVariant.Product != null ? sm.ProductVariant.Product.Name : "Product",
                    VariantSku = sm.ProductVariant.SKU,
                    SizeName = sm.ProductVariant.Size != null ? sm.ProductVariant.Size.Name : "",
                    ColorName = sm.ProductVariant.Color != null ? sm.ProductVariant.Color.Name : "",
                    MovementType = sm.MovementType,
                    Quantity = sm.Quantity,
                    PreviousQuantity = sm.PreviousQuantity,
                    NewQuantity = sm.NewQuantity,
                    Reason = sm.Reason,
                    Reference = sm.Reference,
                    CreatedBy = sm.CreatedBy,
                    CreatedAt = sm.CreatedAt
                })
                .ToListAsync();

            var result = new PagedResult<StockMovementDto>
            {
                Items = movements,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<StockMovementDto>>.Ok(result));
        }

        // ==========================================
        // GET: /api/inventoryadjustment/stock-check
        // Warehouse & staff stock lookup tool
        // Accessible by SuperAdmin, Admin, Manager, Cashier, Staff
        // ==========================================
        [Authorize(Roles = "SuperAdmin,Admin,Manager,Cashier,Staff")]
        [HttpGet("stock-check")]
        public async Task<IActionResult> StockCheck([FromQuery] StockFilterDto filter)
        {
            var query = _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Brand)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                .Include(v => v.Size)
                .Include(v => v.Color)
                .Include(v => v.Inventory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(v => 
                    v.SKU.ToLower().Contains(term) ||
                    (v.Product != null && v.Product.Name.ToLower().Contains(term)));
            }

            if (filter.CategoryId.HasValue && filter.CategoryId.Value != Guid.Empty)
            {
                query = query.Where(v => v.Product != null && v.Product.CategoryId == filter.CategoryId.Value);
            }

            if (filter.LowStockOnly == true)
            {
                query = query.Where(v => v.Inventory == null || 
                    (v.Inventory.Quantity - v.Inventory.ReservedQuantity) <= filter.LowStockThreshold);
            }

            var totalCount = await query.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

            var items = await query
                .OrderBy(v => v.Product != null ? v.Product.Name : "")
                .ThenBy(v => v.SKU)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new StockCheckItemDto
                {
                    ProductVariantId = v.Id,
                    ProductId = v.ProductId,
                    ProductName = v.Product != null ? v.Product.Name : "Product",
                    CategoryName = v.Product != null && v.Product.Category != null ? v.Product.Category.Name : "",
                    BrandName = v.Product != null && v.Product.Brand != null ? v.Product.Brand.Name : "",
                    VariantSku = v.SKU,
                    SizeName = v.Size != null ? v.Size.Name : "",
                    ColorName = v.Color != null ? v.Color.Name : "",
                    ColorHex = v.Color != null ? v.Color.HexCode : null,
                    Price = v.Price,
                    Quantity = v.Inventory != null ? v.Inventory.Quantity : 0,
                    ReservedQuantity = v.Inventory != null ? v.Inventory.ReservedQuantity : 0,
                    AvailableQuantity = v.Inventory != null ? Math.Max(0, v.Inventory.Quantity - v.Inventory.ReservedQuantity) : 0,
                    ImageUrl = v.Product != null && v.Product.Images.Any() 
                        ? (v.Product.Images.FirstOrDefault(i => i.IsPrimary) ?? v.Product.Images.First()).ImageUrl 
                        : null,
                    LastUpdated = v.Inventory != null ? v.Inventory.UpdatedAt : DateTime.UtcNow
                })
                .ToListAsync();

            var result = new PagedResult<StockCheckItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<StockCheckItemDto>>.Ok(result));
        }
    }
}
