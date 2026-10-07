using System.Security.Claims;
using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;
using WebApplication_ClothingEcommerce.Services;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IOrderService _orderService;

        public OrdersController(AppDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail("Unauthorized", 401));
            }

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Payment)
                    .ThenInclude(p => p.PaymentMethod)
                .Where(o => o.Customer != null && o.Customer.ApplicationUserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.Id.ToString(),
                    UserId = userId,
                    CustomerName = $"{o.Customer.FirstName} {o.Customer.LastName}",
                    CustomerEmail = o.Customer.Email,
                    CustomerPhone = o.Customer.Phone,
                    ShippingAddress = o.Address != null ? $"{o.Address.Street}, {o.Address.City}" : "",
                    Subtotal = o.TotalAmount,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString(),
                    PaymentStatus = o.Payment != null ? o.Payment.PaymentStatus.ToString() : "Pending",
                    PaymentMethod = o.Payment != null && o.Payment.PaymentMethod != null ? o.Payment.PaymentMethod.Name : "KHQR",
                    CreatedAt = o.OrderDate,
                    Items = o.Items.Where(i => i.Variant != null && i.Variant.Product != null).Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        ProductId = oi.Variant.ProductId,
                        ProductName = oi.Variant.Product.Name,
                        ProductImage = oi.Variant.Product.Images.Where(img => img.IsPrimary).Select(img => img.ImageUrl).FirstOrDefault()
                                       ?? oi.Variant.Product.Images.Select(img => img.ImageUrl).FirstOrDefault(),
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var userId = CurrentUserId;
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Images)
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Payment)
                    .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(ApiResponse.Fail("Order not found.", 404));
            }

            var isUserAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            if (!isUserAdmin && order.Customer?.ApplicationUserId != userId)
            {
                return Forbid();
            }

            var dto = new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.Id.ToString(),
                UserId = order.Customer?.ApplicationUserId,
                CustomerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}",
                CustomerEmail = order.Customer?.Email ?? "",
                CustomerPhone = order.Customer?.Phone ?? "",
                ShippingAddress = order.Address != null ? $"{order.Address.Street}, {order.Address.City}" : "",
                Subtotal = order.TotalAmount,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                PaymentStatus = order.Payment?.PaymentStatus.ToString() ?? "Pending",
                PaymentMethod = order.Payment?.PaymentMethod?.Name ?? "KHQR",
                CreatedAt = order.OrderDate,
                Items = order.Items.Where(i => i.Variant != null && i.Variant.Product != null).Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.Variant.ProductId,
                    ProductName = oi.Variant.Product.Name,
                    ProductImage = oi.Variant.Product.Images.Where(img => img.IsPrimary).Select(img => img.ImageUrl).FirstOrDefault()
                                   ?? oi.Variant.Product.Images.Select(img => img.ImageUrl).FirstOrDefault(),
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity
                }).ToList()
            };

            return Ok(ApiResponse<OrderDto>.Ok(dto));
        }

        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OrderDto>.Fail(errors));
            }

            var userId = CurrentUserId;
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);
            if (customer == null)
            {
                return BadRequest(ApiResponse<OrderDto>.Fail("Customer account not found for current user."));
            }

            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Name.Contains(request.PaymentMethod) || p.IsActive);

            var checkoutVm = new CheckoutViewModel
            {
                FirstName = request.FullName.Split(' ').FirstOrDefault() ?? request.FullName,
                LastName = request.FullName.Contains(' ') ? request.FullName.Substring(request.FullName.IndexOf(' ') + 1) : "",
                Email = request.Email,
                Phone = request.Phone,
                Street = request.Address,
                City = request.City ?? "Phnom Penh",
                Province = "រាជធានីភ្នំពេញ",
                PostalCode = "12000",
                DeliveryNote = request.Notes,
                PaymentMethodId = paymentMethod?.Id,
                DeliveryType = "ExpressDelivery",
                CarrierCode = "VETExpress",
                SelectedProvince = "រាជធានីភ្នំពេញ"
            };

            var placementResult = await _orderService.PlaceOrderAsync(customer.Id, checkoutVm);
            if (!placementResult.Success || !placementResult.OrderId.HasValue)
            {
                var errors = placementResult.Errors.Any()
                    ? placementResult.Errors
                    : new List<string> { placementResult.Message ?? "Failed to place order." };
                return BadRequest(ApiResponse<OrderDto>.Fail(errors));
            }

            var placedOrder = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == placementResult.OrderId.Value);

            var orderDto = new OrderDto
            {
                Id = placedOrder?.Id ?? placementResult.OrderId.Value,
                OrderNumber = (placedOrder?.Id ?? placementResult.OrderId.Value).ToString(),
                UserId = userId,
                CustomerName = request.FullName,
                CustomerEmail = request.Email,
                CustomerPhone = request.Phone,
                ShippingAddress = $"{request.Address}, {request.City}",
                TotalAmount = placedOrder?.TotalAmount ?? 0,
                Status = placedOrder?.Status.ToString() ?? "Pending",
                PaymentStatus = "Pending",
                PaymentMethod = request.PaymentMethod,
                CreatedAt = placedOrder?.OrderDate ?? DateTime.UtcNow
            };

            return Ok(ApiResponse<OrderDto>.Created(orderDto, "Order placed successfully."));
        }
    }
}
