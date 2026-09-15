using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public DashboardService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync(bool isSuperAdmin, string? requestedView = null)
        {
            var vm = new AdminDashboardViewModel
            {
                IsSuperAdmin = isSuperAdmin
            };

            // Determine active view: SuperAdmin defaults to "superadmin", StoreAdmin is locked to "store"
            if (!isSuperAdmin)
            {
                vm.ActiveView = "store";
                vm.StoreAdmin = await GetStoreAdminDashboardAsync();
            }
            else
            {
                vm.ActiveView = string.Equals(requestedView, "store", StringComparison.OrdinalIgnoreCase)
                    ? "store"
                    : "superadmin";

                // Load both for SuperAdmin so they can seamlessly switch tabs
                vm.SuperAdmin = await GetSuperAdminDashboardAsync();
                vm.StoreAdmin = await GetStoreAdminDashboardAsync();
            }

            return vm;
        }

        public async Task<SuperAdminDashboardViewModel> GetSuperAdminDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var todayUtc = now.Date;
            var monthStartUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Revenue queries
            var totalRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var todayRevenue = await _context.Payments
                .Where(p => (p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed) &&
                            p.PaidAt != null && p.PaidAt >= todayUtc)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var monthRevenue = await _context.Payments
                .Where(p => (p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed) &&
                            p.PaidAt != null && p.PaidAt >= monthStartUtc)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var totalOrders = await _context.Orders.CountAsync();
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // Security & Admin accounts
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdminUsers = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var totalUsers = await _userManager.Users.CountAsync();
            var totalRoles = await _roleManager.Roles.CountAsync();

            // Status distribution
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            var processingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing);
            var shippedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipped);
            var deliveredOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
            var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);

            // Catalog counts
            var totalProducts = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var totalBrands = await _context.Brands.CountAsync();
            var totalCustomers = await _context.Customers.CountAsync();

            // Audit logs
            var totalAuditLogs = await _context.AuditLogs.CountAsync();
            var todayAuditLogs = await _context.AuditLogs.CountAsync(l => l.CreatedAt >= todayUtc);

            var recentAuditLogs = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(l => l.CreatedAt)
                .Take(6)
                .ToListAsync();

            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .ToListAsync();

            var recentUsers = await _userManager.Users
                .AsNoTracking()
                .OrderByDescending(u => u.Id)
                .Take(5)
                .ToListAsync();

            var yesterdayUtc = todayUtc.AddDays(-1);
            var yesterdayRevenue = await _context.Payments
                .Where(p => (p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed) &&
                            p.PaidAt != null && p.PaidAt >= yesterdayUtc && p.PaidAt < todayUtc)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var revenueGrowth = yesterdayRevenue > 0
                ? Math.Round(((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100, 1)
                : (todayRevenue > 0 ? 100m : 0m);

            var salesTrend = await GetSalesTrendAsync(7);
            var categoryDistributions = await GetCategoryDistributionsAsync();
            var inventoryHealth = await GetInventoryHealthAsync();
            var paymentStats = await GetPaymentDistributionAsync();
            var fulfillmentStats = BuildFulfillmentStats(pendingOrders, processingOrders, shippedOrders, deliveredOrders, cancelledOrders, totalOrders);

            return new SuperAdminDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                AverageOrderValue = averageOrderValue,
                RevenueGrowthPercentage = revenueGrowth,

                TotalUsers = totalUsers,
                TotalCustomers = totalCustomers,
                TotalAdmins = adminUsers.Count + superAdminUsers.Count,
                SuperAdminCount = superAdminUsers.Count,
                TotalRoles = totalRoles,

                TotalOrders = totalOrders,
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                TotalBrands = totalBrands,
                TotalAuditLogs = totalAuditLogs,
                TodayAuditLogs = todayAuditLogs,

                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,

                SalesTrend = salesTrend,
                CategoryDistributions = categoryDistributions,
                InventoryHealth = inventoryHealth,
                FulfillmentStats = fulfillmentStats,
                PaymentStats = paymentStats,

                RecentAuditLogs = recentAuditLogs,
                RecentOrders = recentOrders,
                RecentUsers = recentUsers,

                EnvironmentName = _environment.EnvironmentName,
                FrameworkVersion = Environment.Version.ToString(),
                ServerTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            };
        }

        public async Task<StoreAdminDashboardViewModel> GetStoreAdminDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var todayUtc = now.Date;
            var monthStartUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Sales KPIs
            var storeRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var todaySales = await _context.Payments
                .Where(p => (p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed) &&
                            p.PaidAt != null && p.PaidAt >= todayUtc)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var monthSales = await _context.Payments
                .Where(p => (p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed) &&
                            p.PaidAt != null && p.PaidAt >= monthStartUtc)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var totalOrders = await _context.Orders.CountAsync();
            var ordersToday = await _context.Orders.CountAsync(o => o.OrderDate >= todayUtc);

            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
            var processingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing);
            var shippedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Shipped);
            var deliveredOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);

            var totalProducts = await _context.Products.CountAsync();
            var activeProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.Active);

            // Inventory Low Stock Alerts (available <= 5 units)
            var lowStockQuery = _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                .Include(v => v.Size)
                .Include(v => v.Color)
                .Include(v => v.Inventory)
                .Where(v => v.Inventory != null && (v.Inventory.Quantity - v.Inventory.ReservedQuantity) <= 5);

            var lowStockCount = await lowStockQuery.CountAsync();
            var outOfStockCount = await lowStockQuery.CountAsync(v => (v.Inventory!.Quantity - v.Inventory.ReservedQuantity) <= 0);

            var lowStockVariants = await lowStockQuery
                .OrderBy(v => v.Inventory!.Quantity - v.Inventory.ReservedQuantity)
                .Take(6)
                .Select(v => new LowStockAlertDto
                {
                    VariantId = v.Id,
                    ProductId = v.ProductId,
                    ProductName = v.Product.Name,
                    SKU = v.SKU,
                    SizeName = v.Size.Name,
                    ColorName = v.Color.Name,
                    AvailableQuantity = v.Inventory!.Quantity - v.Inventory.ReservedQuantity,
                    Price = v.Price,
                    ImageUrl = v.Product.Images.FirstOrDefault(i => i.IsPrimary) != null
                        ? v.Product.Images.FirstOrDefault(i => i.IsPrimary)!.ImageUrl
                        : v.Product.Images.FirstOrDefault() != null ? v.Product.Images.FirstOrDefault()!.ImageUrl : null
                })
                .ToListAsync();

            // Top Selling Products (calculated from OrderItems)
            var topSellingGroup = await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Category)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Brand)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.Inventory)
                .Where(oi => oi.Variant != null && oi.Variant.Product != null)
                .GroupBy(oi => oi.Variant.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToListAsync();

            var topSellingProducts = new List<TopProductDto>();
            foreach (var item in topSellingGroup)
            {
                var prod = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.Inventory)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (prod != null)
                {
                    var img = prod.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? prod.Images.FirstOrDefault()?.ImageUrl;
                    var totalStock = prod.Variants.Sum(v => v.Inventory?.AvailableQuantity ?? 0);
                    var minPrice = prod.Variants.Any() ? prod.Variants.Min(v => v.Price) : 0;

                    topSellingProducts.Add(new TopProductDto
                    {
                        ProductId = prod.Id,
                        Name = prod.Name,
                        CategoryName = prod.Category?.Name ?? "General",
                        BrandName = prod.Brand?.Name ?? "No Brand",
                        MinPrice = minPrice,
                        TotalQuantitySold = item.TotalQuantity,
                        TotalRevenue = item.TotalRevenue,
                        ImageUrl = img,
                        CurrentStock = totalStock
                    });
                }
            }

            // Recent Orders for store fulfillment (Prioritize orders ready for shipment processing)
            var readyOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Processing)
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .ToListAsync();

            var otherOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                .Include(o => o.Shipment)
                .Include(o => o.Items)
                .Where(o => o.Status != OrderStatus.Pending && o.Status != OrderStatus.Confirmed && o.Status != OrderStatus.Processing)
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .ToListAsync();

            var recentOrders = readyOrders.Concat(otherOrders).ToList();

            // Pending Shipments
            var pendingShipments = await _context.Shipments
                .AsNoTracking()
                .Include(s => s.Order)
                    .ThenInclude(o => o.Customer)
                .Where(s => s.ShipmentStatus == ShipmentStatus.Pending || s.ShipmentStatus == ShipmentStatus.InTransit)
                .OrderByDescending(s => s.Order.OrderDate)
                .Take(5)
                .ToListAsync();

            // Recent Reviews
            var recentReviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.Product)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CreatedAt)
                .Take(4)
                .ToListAsync();

            var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);
            var nonCancelled = totalOrders - cancelledOrders;
            var fulfillmentRate = nonCancelled > 0 ? Math.Round((double)deliveredOrders / nonCancelled * 100, 1) : 0;

            var avgRating = await _context.Reviews.AnyAsync()
                ? Math.Round(await _context.Reviews.AverageAsync(r => (double)r.Rating), 1)
                : 5.0;
            var totalReviewsCount = await _context.Reviews.CountAsync();

            var monthlySalesTarget = 5000m;
            var monthlyTargetProgress = monthlySalesTarget > 0 ? Math.Min(100.0, Math.Round((double)(monthSales / monthlySalesTarget) * 100, 1)) : 0;

            var salesTrend = await GetSalesTrendAsync(7);
            var categoryDistributions = await GetCategoryDistributionsAsync();
            var inventoryHealth = await GetInventoryHealthAsync();
            var fulfillmentStats = BuildFulfillmentStats(pendingOrders, processingOrders, shippedOrders, deliveredOrders, cancelledOrders, totalOrders);

            return new StoreAdminDashboardViewModel
            {
                StoreRevenue = storeRevenue,
                TodaySales = todaySales,
                MonthSales = monthSales,
                TotalOrders = totalOrders,
                OrdersToday = ordersToday,

                PendingOrdersCount = pendingOrders,
                ProcessingOrdersCount = processingOrders,
                ShippedOrdersCount = shippedOrders,
                DeliveredOrdersCount = deliveredOrders,
                CancelledOrdersCount = cancelledOrders,
                FulfillmentRate = fulfillmentRate,

                MonthlySalesTarget = monthlySalesTarget,
                MonthlyTargetProgress = monthlyTargetProgress,
                CustomerSatisfactionRating = avgRating,
                TotalReviewsCount = totalReviewsCount,

                SalesTrend = salesTrend,
                CategoryDistributions = categoryDistributions,
                InventoryHealth = inventoryHealth,
                FulfillmentStats = fulfillmentStats,

                TotalProducts = totalProducts,
                ActiveProductsCount = activeProducts,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,
                LowStockAlerts = lowStockVariants,

                TopSellingProducts = topSellingProducts,
                RecentOrders = recentOrders,
                PendingShipments = pendingShipments,
                RecentReviews = recentReviews
            };
        }

        // =========================================================
        // PRIVATE ANALYTICS HELPER METHODS
        // =========================================================

        private async Task<List<DailySalesTrendDto>> GetSalesTrendAsync(int days = 7)
        {
            var list = new List<DailySalesTrendDto>();
            var todayUtc = DateTime.UtcNow.Date;
            var startDate = todayUtc.AddDays(-(days - 1));

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => (p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed) &&
                            p.PaidAt != null && p.PaidAt >= startDate)
                .ToListAsync();

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderDate >= startDate)
                .ToListAsync();

            for (int i = 0; i < days; i++)
            {
                var targetDay = startDate.AddDays(i);
                var nextDay = targetDay.AddDays(1);

                var dayRev = payments
                    .Where(p => p.PaidAt >= targetDay && p.PaidAt < nextDay)
                    .Sum(p => p.Amount);

                var dayOrders = orders
                    .Count(o => o.OrderDate >= targetDay && o.OrderDate < nextDay);

                if (dayRev == 0 && dayOrders > 0)
                {
                    dayRev = orders
                        .Where(o => o.OrderDate >= targetDay && o.OrderDate < nextDay)
                        .Sum(o => o.TotalAmount);
                }

                list.Add(new DailySalesTrendDto
                {
                    DateLabel = targetDay.ToString("MMM dd"),
                    Revenue = dayRev,
                    OrdersCount = dayOrders
                });
            }

            return list;
        }

        private async Task<List<CategorySalesDistributionDto>> GetCategoryDistributionsAsync()
        {
            var palette = new[] { "#4285F4", "#34A853", "#FBBC04", "#EA4335", "#8E24AA", "#00ACC1", "#FB8C00", "#5C6BC0" };

            var categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Products)
                .ToListAsync();

            var orderItems = await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                .Where(oi => oi.Variant != null && oi.Variant.Product != null)
                .ToListAsync();

            var totalItemsSold = orderItems.Sum(oi => oi.Quantity);
            var list = new List<CategorySalesDistributionDto>();
            int colorIdx = 0;

            foreach (var cat in categories)
            {
                var catItems = orderItems
                    .Where(oi => oi.Variant?.Product?.CategoryId == cat.Id)
                    .ToList();

                var itemsSold = catItems.Sum(oi => oi.Quantity);
                var rev = catItems.Sum(oi => oi.Quantity * oi.UnitPrice);

                list.Add(new CategorySalesDistributionDto
                {
                    CategoryName = cat.Name,
                    ProductCount = cat.Products.Count,
                    ItemsSold = itemsSold,
                    Revenue = rev,
                    Percentage = 0,
                    ColorHex = palette[colorIdx % palette.Length]
                });

                colorIdx++;
            }

            if (totalItemsSold > 0)
            {
                foreach (var item in list)
                {
                    item.Percentage = Math.Round((double)item.ItemsSold / totalItemsSold * 100, 1);
                }
            }
            else
            {
                var totalProds = list.Sum(x => x.ProductCount);
                if (totalProds > 0)
                {
                    foreach (var item in list)
                    {
                        item.Percentage = Math.Round((double)item.ProductCount / totalProds * 100, 1);
                    }
                }
            }

            return list.OrderByDescending(x => x.ItemsSold).ThenByDescending(x => x.ProductCount).Take(6).ToList();
        }

        private async Task<InventoryHealthDto> GetInventoryHealthAsync()
        {
            var variants = await _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Inventory)
                .ToListAsync();

            var total = variants.Count;
            var inStock = variants.Count(v => v.Inventory != null && (v.Inventory.Quantity - v.Inventory.ReservedQuantity) > 5);
            var lowStock = variants.Count(v => v.Inventory != null &&
                                               (v.Inventory.Quantity - v.Inventory.ReservedQuantity) > 0 &&
                                               (v.Inventory.Quantity - v.Inventory.ReservedQuantity) <= 5);
            var outOfStock = variants.Count(v => v.Inventory == null || (v.Inventory.Quantity - v.Inventory.ReservedQuantity) <= 0);

            var score = total > 0 ? Math.Round((double)inStock / total * 100, 1) : 100;

            return new InventoryHealthDto
            {
                TotalVariants = total,
                InStockCount = inStock,
                LowStockCount = lowStock,
                OutOfStockCount = outOfStock,
                HealthScorePercentage = score
            };
        }

        private async Task<PaymentDistributionDto> GetPaymentDistributionAsync()
        {
            var payments = await _context.Payments.AsNoTracking().ToListAsync();

            var paid = payments.Where(p => p.PaymentStatus == PaymentStatus.Paid || p.PaymentStatus == PaymentStatus.Completed).ToList();
            var pending = payments.Where(p => p.PaymentStatus == PaymentStatus.Pending).ToList();
            var failed = payments.Where(p => p.PaymentStatus == PaymentStatus.Failed || p.PaymentStatus == PaymentStatus.Cancelled || p.PaymentStatus == PaymentStatus.Refunded).ToList();

            return new PaymentDistributionDto
            {
                PaidCount = paid.Count,
                PendingCount = pending.Count,
                FailedOrCancelledCount = failed.Count,
                TotalPaidAmount = paid.Sum(p => p.Amount),
                TotalPendingAmount = pending.Sum(p => p.Amount)
            };
        }

        private static OrderFulfillmentStatsDto BuildFulfillmentStats(int pending, int processing, int shipped, int delivered, int cancelled, int total)
        {
            var nonCancelled = total - cancelled;
            var rate = nonCancelled > 0 ? Math.Round((double)delivered / nonCancelled * 100, 1) : 0;

            return new OrderFulfillmentStatsDto
            {
                TotalOrders = total,
                PendingCount = pending,
                ProcessingCount = processing,
                ShippedCount = shipped,
                DeliveredCount = delivered,
                CancelledCount = cancelled,
                FulfillmentRate = rate
            };
        }
    }
}
