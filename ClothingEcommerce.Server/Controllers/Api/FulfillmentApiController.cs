using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Catalog;
using ClothingEcommerce.Shared.DTOs.Fulfillment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager,Staff")]
    public class FulfillmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FulfillmentController> _logger;

        public FulfillmentController(AppDbContext context, ILogger<FulfillmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // GET: /api/fulfillment/orders
        // Queue for warehouse packing and dispatch
        // ==========================================
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? tab = "to_pack", [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
        {
            try
            {
                var query = _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Customer)
                    .Include(o => o.Address)
                    .Include(o => o.Payment)
                        .ThenInclude(p => p.PaymentMethod)
                    .Include(o => o.Shipment)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Product)
                                .ThenInclude(p => p.Images)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Size)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Variant)
                            .ThenInclude(v => v.Color)
                    .AsQueryable();

                // Apply tab filter
                tab = (tab ?? "to_pack").ToLower();
                if (tab == "to_pack")
                {
                    query = query.Where(o => o.Status == OrderStatus.Pending || (o.Status == OrderStatus.Processing && o.Shipment == null));
                }
                else if (tab == "packed")
                {
                    query = query.Where(o => o.Status == OrderStatus.Processing && o.Shipment != null && o.Shipment.ShipmentStatus == ShipmentStatus.Pending);
                }
                else if (tab == "shipped")
                {
                    query = query.Where(o => o.Status == OrderStatus.Shipped || (o.Shipment != null && o.Shipment.ShipmentStatus == ShipmentStatus.InTransit));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(o => 
                        o.Id.ToString().ToLower().Contains(term) ||
                        (o.Customer != null && (o.Customer.FirstName.ToLower().Contains(term) || o.Customer.LastName.ToLower().Contains(term) || (o.Customer.Phone != null && o.Customer.Phone.Contains(term)))) ||
                        (o.Shipment != null && o.Shipment.TrackingNumber.ToLower().Contains(term)));
                }

                var totalCount = await query.CountAsync();
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 15 : pageSize;

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new FulfillmentOrderDto
                    {
                        OrderId = o.Id,
                        OrderNumber = o.Id.ToString(),
                        OrderDate = o.OrderDate,
                        CustomerName = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}" : "Customer",
                        CustomerPhone = o.Customer != null ? o.Customer.Phone ?? "" : "",
                        CustomerEmail = o.Customer != null ? o.Customer.Email : "",
                        ShippingAddress = o.Address != null ? $"{o.Address.Street}, {o.Address.City}" : "",
                        City = o.Address != null ? o.Address.City : "",
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        PaymentStatus = o.Payment != null ? o.Payment.PaymentStatus.ToString() : "Pending",
                        PaymentMethod = o.Payment != null && o.Payment.PaymentMethod != null ? o.Payment.PaymentMethod.Name : "KHQR",
                        CarrierName = o.Shipment != null ? o.Shipment.ShippingCompany : null,
                        TrackingNumber = o.Shipment != null ? o.Shipment.TrackingNumber : null,
                        TotalItems = o.Items.Sum(i => i.Quantity),
                        Items = o.Items.Select(oi => new FulfillmentItemDto
                        {
                            OrderItemId = oi.Id,
                            VariantId = oi.VariantId,
                            ProductName = oi.Variant != null && oi.Variant.Product != null ? oi.Variant.Product.Name : "Product",
                            VariantSku = oi.Variant != null ? oi.Variant.SKU : "",
                            SizeName = oi.Variant != null && oi.Variant.Size != null ? oi.Variant.Size.Name : "",
                            ColorName = oi.Variant != null && oi.Variant.Color != null ? oi.Variant.Color.Name : "",
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            ProductImageUrl = oi.Variant != null && oi.Variant.Product != null && oi.Variant.Product.Images.Any()
                                ? (oi.Variant.Product.Images.FirstOrDefault(img => img.IsPrimary) ?? oi.Variant.Product.Images.First()).ImageUrl
                                : null
                        }).ToList()
                    })
                    .ToListAsync();

                var result = new PagedResult<FulfillmentOrderDto>
                {
                    Items = orders,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return Ok(ApiResponse<PagedResult<FulfillmentOrderDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fulfillment orders");
                return StatusCode(500, ApiResponse.Fail("Failed to load fulfillment orders: " + ex.Message));
            }
        }

        // ==========================================
        // POST: /api/fulfillment/update-status
        // Mark as Packed or Shipped
        // ==========================================
        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateFulfillmentStatusDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse.Fail(errors));
            }

            var order = await _context.Orders
                .Include(o => o.Shipment)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return NotFound(ApiResponse.Fail("Order not found."));
            }

            var target = request.NewStatus.Trim().ToLower();

            if (target == "packed")
            {
                order.Status = OrderStatus.Processing;

                if (order.Shipment == null)
                {
                    order.Shipment = new Shipment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ShippingCompany = request.CarrierName ?? "VET Express (Virak Buntham)",
                        TrackingNumber = request.TrackingNumber ?? ("TRK-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + new Random().Next(1000, 9999)),
                        ShipmentStatus = ShipmentStatus.Pending
                    };
                    _context.Shipments.Add(order.Shipment);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.CarrierName))
                        order.Shipment.ShippingCompany = request.CarrierName;
                    if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
                        order.Shipment.TrackingNumber = request.TrackingNumber;
                }
            }
            else if (target == "shipped")
            {
                order.Status = OrderStatus.Shipped;

                if (order.Shipment == null)
                {
                    order.Shipment = new Shipment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ShippingCompany = request.CarrierName ?? "VET Express (Virak Buntham)",
                        TrackingNumber = request.TrackingNumber ?? ("TRK-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + new Random().Next(1000, 9999)),
                        ShipmentStatus = ShipmentStatus.InTransit,
                        ShippedAt = DateTime.UtcNow
                    };
                    _context.Shipments.Add(order.Shipment);
                }
                else
                {
                    order.Shipment.ShipmentStatus = ShipmentStatus.InTransit;
                    order.Shipment.ShippedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.CarrierName))
                        order.Shipment.ShippingCompany = request.CarrierName;
                    if (!string.IsNullOrWhiteSpace(request.TrackingNumber))
                        order.Shipment.TrackingNumber = request.TrackingNumber;
                }
            }
            else if (target == "delivered")
            {
                order.Status = OrderStatus.Delivered;
                if (order.Shipment != null)
                {
                    order.Shipment.ShipmentStatus = ShipmentStatus.Delivered;
                    order.Shipment.DeliveredAt = DateTime.UtcNow;
                }
            }
            else if (target == "cancelled")
            {
                order.Status = OrderStatus.Cancelled;
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.Ok("Order fulfillment status updated successfully."));
        }
    }
}
