using System.Security.Claims;
using ClothingEcommerce.Shared.Common;
using ClothingEcommerce.Shared.DTOs.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace ClothingEcommerce.Server.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager,Cashier")]
    public class PosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IKhqrService _khqrService;
        private readonly ILogger<PosController> _logger;

        public PosController(AppDbContext context, IKhqrService khqrService, ILogger<PosController> logger)
        {
            _context = context;
            _khqrService = khqrService;
            _logger = logger;
        }

        private string CurrentUserName => User.FindFirstValue(ClaimTypes.Name) 
            ?? User.FindFirstValue(ClaimTypes.Email) 
            ?? "Cashier";

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ==========================================
        // GET: /api/pos/products
        // Fast search by barcode/SKU/name for POS
        // ==========================================
        [HttpGet("products")]
        public async Task<IActionResult> GetPosProducts([FromQuery] string? search = null, [FromQuery] Guid? categoryId = null)
        {
            try
            {
                var query = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.Size)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.Color)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.Inventory)
                    .Where(p => p.Status == ProductStatus.Active);

                if (categoryId.HasValue && categoryId.Value != Guid.Empty)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(p => 
                        p.Name.ToLower().Contains(term) ||
                        p.Variants.Any(v => v.SKU.ToLower().Contains(term)));
                }

                var products = await query
                    .Take(100)
                    .Select(p => new PosProductDto
                    {
                        ProductId = p.Id,
                        Name = p.Name,
                        CategoryName = p.Category != null ? p.Category.Name : null,
                        BrandName = p.Brand != null ? p.Brand.Name : null,
                        ImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                                   ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault(),
                        BasePrice = p.Variants.Select(v => v.Price).FirstOrDefault(),
                        TotalAvailableStock = p.Variants.Where(v => v.Inventory != null).Sum(v => v.Inventory!.Quantity - v.Inventory!.ReservedQuantity),
                        Variants = p.Variants.Select(v => new PosProductVariantDto
                        {
                            VariantId = v.Id,
                            Sku = v.SKU,
                            SizeName = v.Size != null ? v.Size.Name : "Standard",
                            ColorName = v.Color != null ? v.Color.Name : "Standard",
                            ColorHex = v.Color != null ? v.Color.HexCode : null,
                            Price = v.Price,
                            CompareAtPrice = v.CompareAtPrice,
                            StockQuantity = v.Inventory != null ? v.Inventory.Quantity : 0,
                            AvailableQuantity = v.Inventory != null ? Math.Max(0, v.Inventory.Quantity - v.Inventory.ReservedQuantity) : 0
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<PosProductDto>>.Ok(products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching POS products");
                return StatusCode(500, ApiResponse.Fail("Failed to retrieve products."));
            }
        }

        // ==========================================
        // POST: /api/pos/checkout
        // Process POS walk-in sale
        // ==========================================
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] PosCheckoutRequestDto request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return BadRequest(ApiResponse<PosCheckoutResponseDto>.Fail("No items in cart to checkout."));
            }

            var variantIds = request.Items.Select(i => i.VariantId).Distinct().ToList();
            var variants = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.Size)
                .Include(v => v.Color)
                .Include(v => v.Inventory)
                .Where(v => variantIds.Contains(v.Id))
                .ToListAsync();

            if (variants.Count != variantIds.Count)
            {
                return BadRequest(ApiResponse<PosCheckoutResponseDto>.Fail("One or more selected products are invalid."));
            }

            // Validate inventory availability
            foreach (var item in request.Items)
            {
                var variant = variants.First(v => v.Id == item.VariantId);
                var availableStock = variant.Inventory != null 
                    ? Math.Max(0, variant.Inventory.Quantity - variant.Inventory.ReservedQuantity) 
                    : 0;

                if (availableStock < item.Quantity)
                {
                    return BadRequest(ApiResponse<PosCheckoutResponseDto>.Fail(
                        $"Insufficient stock for '{variant.Product?.Name} ({variant.Size?.Name}/{variant.Color?.Name})'. Available: {availableStock}, Requested: {item.Quantity}"));
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Ensure a POS Walk-in Customer
                var customerEmail = !string.IsNullOrWhiteSpace(request.CustomerEmail) 
                    ? request.CustomerEmail.Trim().ToLower() 
                    : "walkin.pos@clothe.local";

                var customer = await _context.Customers
                    .Include(c => c.Addresses)
                    .FirstOrDefaultAsync(c => c.Email == customerEmail);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        Id = Guid.NewGuid(),
                        FirstName = string.IsNullOrWhiteSpace(request.CustomerName) ? "Walk-in" : request.CustomerName.Trim(),
                        LastName = "Customer",
                        Email = customerEmail,
                        Phone = request.CustomerPhone ?? "012345678",
                        Status = CustomerStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // Ensure an address for the order
                var address = customer.Addresses.FirstOrDefault();
                if (address == null)
                {
                    address = new Address
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        Street = "Store POS Checkout",
                        City = "Phnom Penh",
                        Province = "រាជធានីភ្នំពេញ",
                        PostalCode = "12000",
                        IsDefault = true
                    };
                    _context.Addresses.Add(address);
                    await _context.SaveChangesAsync();
                }

                // Calculate totals
                decimal subtotalUsd = 0;
                var receiptItems = new List<PosReceiptItemDto>();
                var orderItems = new List<OrderItem>();

                var orderId = Guid.NewGuid();
                var shortReceiptCode = "REC-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + new Random().Next(1000, 9999);

                foreach (var item in request.Items)
                {
                    var variant = variants.First(v => v.Id == item.VariantId);
                    var price = item.UnitPrice > 0 ? item.UnitPrice : variant.Price;
                    var itemSubtotal = price * item.Quantity;
                    subtotalUsd += itemSubtotal;

                    orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        VariantId = variant.Id,
                        Quantity = item.Quantity,
                        UnitPrice = price
                    });

                    receiptItems.Add(new PosReceiptItemDto
                    {
                        ProductName = variant.Product?.Name ?? "Product",
                        VariantSku = variant.SKU,
                        SizeName = variant.Size?.Name ?? "Standard",
                        ColorName = variant.Color?.Name ?? "Standard",
                        Quantity = item.Quantity,
                        UnitPrice = price
                    });

                    // Deduct inventory and log StockMovement
                    if (variant.Inventory != null)
                    {
                        var prevQty = variant.Inventory.Quantity;
                        variant.Inventory.Quantity = Math.Max(0, variant.Inventory.Quantity - item.Quantity);
                        variant.Inventory.UpdatedAt = DateTime.UtcNow;

                        _context.StockMovements.Add(new StockMovement
                        {
                            Id = Guid.NewGuid(),
                            ProductVariantId = variant.Id,
                            MovementType = "PosSale",
                            Quantity = item.Quantity,
                            PreviousQuantity = prevQty,
                            NewQuantity = variant.Inventory.Quantity,
                            Reason = $"POS Sale: {shortReceiptCode}",
                            Reference = shortReceiptCode,
                            CreatedBy = CurrentUserName,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                decimal totalUsd = Math.Max(0, subtotalUsd - request.DiscountAmount);
                decimal exchangeRate = request.ExchangeRate > 0 ? request.ExchangeRate : 4100;
                decimal totalKhr = Math.Round(totalUsd * exchangeRate, 0);

                // Create Order
                var order = new Order
                {
                    Id = orderId,
                    CustomerId = customer.Id,
                    AddressId = address.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Delivered, // In-store POS is fulfilled on checkout
                    TotalAmount = totalUsd,
                    Items = orderItems
                };
                _context.Orders.Add(order);

                // Ensure PaymentMethod exists
                var isKhqr = request.PaymentMethod.Equals("KHQR", StringComparison.OrdinalIgnoreCase);
                var paymentMethodName = isKhqr ? "NBC Bakong KHQR" : "Cash (សាច់ប្រាក់)";
                var paymentMethod = await _context.PaymentMethods
                    .FirstOrDefaultAsync(pm => pm.Name.Contains(isKhqr ? "KHQR" : "Cash") || pm.Name == paymentMethodName);

                if (paymentMethod == null)
                {
                    paymentMethod = new PaymentMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = paymentMethodName,
                        IsActive = true
                    };
                    _context.PaymentMethods.Add(paymentMethod);
                    await _context.SaveChangesAsync();
                }

                // Change calculation
                decimal totalTenderedUsd = request.CashTenderedUsd + (request.CashTenderedKhr > 0 ? request.CashTenderedKhr / exchangeRate : 0);
                decimal changeUsd = 0;
                decimal changeKhr = 0;

                if (!isKhqr)
                {
                    if (totalTenderedUsd < totalUsd)
                    {
                        totalTenderedUsd = totalUsd; // Exact cash assumed if not entered
                    }
                    changeUsd = Math.Max(0, totalTenderedUsd - totalUsd);
                    changeKhr = Math.Round(changeUsd * exchangeRate, 0);
                }

                string? khqrPayload = null;
                string? khqrMd5 = null;

                if (isKhqr)
                {
                    try
                    {
                        var qrRes = _khqrService.GenerateKhqr(totalUsd, "USD", shortReceiptCode);
                        khqrPayload = qrRes.QrString;
                        khqrMd5 = qrRes.Md5;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate dynamic KHQR in POS, using fallback");
                    }
                }

                // Payment record
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentMethodId = paymentMethod.Id,
                    PaymentStatus = PaymentStatus.Completed,
                    Amount = totalUsd,
                    Currency = "USD",
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow,
                    QRCode = khqrPayload,
                    Md5Hash = khqrMd5
                };
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var responseDto = new PosCheckoutResponseDto
                {
                    Success = true,
                    OrderId = order.Id,
                    ReceiptNumber = shortReceiptCode,
                    TransactionDate = DateTime.UtcNow,
                    CashierName = CurrentUserName,
                    SubtotalUsd = subtotalUsd,
                    DiscountUsd = request.DiscountAmount,
                    TotalAmountUsd = totalUsd,
                    TotalAmountKhr = totalKhr,
                    ExchangeRate = exchangeRate,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = "Paid",
                    CashTenderedUsd = request.CashTenderedUsd,
                    CashTenderedKhr = request.CashTenderedKhr,
                    ChangeUsd = Math.Round(changeUsd, 2),
                    ChangeKhr = changeKhr,
                    KhqrPayload = khqrPayload,
                    KhqrMd5 = khqrMd5,
                    Items = receiptItems
                };

                return Ok(ApiResponse<PosCheckoutResponseDto>.Ok(responseDto, "POS sale completed successfully."));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction failed during POS checkout");
                return StatusCode(500, ApiResponse<PosCheckoutResponseDto>.Fail("POS checkout failed: " + ex.Message));
            }
        }

        // ==========================================
        // GET: /api/pos/daily-summary
        // Cashier shift / daily summary
        // ==========================================
        [HttpGet("daily-summary")]
        public async Task<IActionResult> GetDailySummary()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var ordersToday = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Payment)
                    .ThenInclude(p => p.PaymentMethod)
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow && o.Status == OrderStatus.Delivered)
                .ToListAsync();

            decimal totalSalesUsd = ordersToday.Sum(o => o.TotalAmount);
            decimal exchangeRate = 4100;
            decimal totalSalesKhr = Math.Round(totalSalesUsd * exchangeRate, 0);

            decimal cashSales = ordersToday
                .Where(o => o.Payment?.PaymentMethod?.Name.Contains("Cash") == true)
                .Sum(o => o.TotalAmount);

            decimal khqrSales = ordersToday
                .Where(o => o.Payment?.PaymentMethod?.Name.Contains("KHQR") == true)
                .Sum(o => o.TotalAmount);

            int totalItemsSold = ordersToday.Sum(o => o.Items.Sum(i => i.Quantity));

            var summary = new PosDailySummaryDto
            {
                Date = today,
                CashierId = CurrentUserId ?? "",
                CashierName = CurrentUserName,
                TotalTransactions = ordersToday.Count,
                TotalSalesUsd = totalSalesUsd,
                TotalSalesKhr = totalSalesKhr,
                CashSalesUsd = cashSales,
                KhqrSalesUsd = khqrSales,
                TotalItemsSold = totalItemsSold,
                ExchangeRate = exchangeRate
            };

            return Ok(ApiResponse<PosDailySummaryDto>.Ok(summary));
        }
    }
}
