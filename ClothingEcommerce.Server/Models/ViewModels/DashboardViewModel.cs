using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    // =========================================================
    // MAIN ADMIN DASHBOARD WRAPPER
    // =========================================================
    public class AdminDashboardViewModel
    {
        public bool IsSuperAdmin { get; set; }
        public string ActiveView { get; set; } = "store"; // "superadmin" or "store"

        public SuperAdminDashboardViewModel SuperAdmin { get; set; } = new();
        public StoreAdminDashboardViewModel StoreAdmin { get; set; } = new();
    }

    // =========================================================
    // SUPER ADMIN DASHBOARD
    // =========================================================
    public class SuperAdminDashboardViewModel
    {
        // Financial & Platform Metrics
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }

        // User & Security Metrics
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalAdmins { get; set; }
        public int SuperAdminCount { get; set; }
        public int TotalRoles { get; set; }

        // Order & Platform Counts
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBrands { get; set; }
        public int TotalAuditLogs { get; set; }
        public int TodayAuditLogs { get; set; }

        // Order Status Distribution
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Professional Platform Visual Analytics & Donut Data
        public List<DailySalesTrendDto> SalesTrend { get; set; } = new();
        public List<CategorySalesDistributionDto> CategoryDistributions { get; set; } = new();
        public InventoryHealthDto InventoryHealth { get; set; } = new();
        public OrderFulfillmentStatsDto FulfillmentStats { get; set; } = new();
        public PaymentDistributionDto PaymentStats { get; set; } = new();
        public decimal RevenueGrowthPercentage { get; set; }

        // Activity Feeds
        public List<AuditLog> RecentAuditLogs { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<ApplicationUser> RecentUsers { get; set; } = new();

        // System Health
        public string ServerTimeUtc { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        public string EnvironmentName { get; set; } = "Development";
        public string FrameworkVersion { get; set; } = Environment.Version.ToString();
    }

    // =========================================================
    // STORE ADMIN DASHBOARD
    // =========================================================
    public class StoreAdminDashboardViewModel
    {
        // Store Operations KPIs
        public decimal StoreRevenue { get; set; }
        public decimal TodaySales { get; set; }
        public decimal MonthSales { get; set; }
        public int TotalOrders { get; set; }
        public int OrdersToday { get; set; }

        // Fulfillment Pipeline
        public int PendingOrdersCount { get; set; }
        public int ProcessingOrdersCount { get; set; }
        public int ShippedOrdersCount { get; set; }
        public int DeliveredOrdersCount { get; set; }
        public int CancelledOrdersCount { get; set; }
        public double FulfillmentRate { get; set; }

        // Sales Target & Rating Benchmarks
        public decimal MonthlySalesTarget { get; set; } = 5000m;
        public double MonthlyTargetProgress { get; set; }
        public double CustomerSatisfactionRating { get; set; }
        public int TotalReviewsCount { get; set; }

        // Professional Platform Visual Analytics & Donut Data
        public List<DailySalesTrendDto> SalesTrend { get; set; } = new();
        public List<CategorySalesDistributionDto> CategoryDistributions { get; set; } = new();
        public InventoryHealthDto InventoryHealth { get; set; } = new();
        public OrderFulfillmentStatsDto FulfillmentStats { get; set; } = new();

        // Inventory Alerts
        public int TotalProducts { get; set; }
        public int ActiveProductsCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<LowStockAlertDto> LowStockAlerts { get; set; } = new();

        // Top Selling Products
        public List<TopProductDto> TopSellingProducts { get; set; } = new();

        // Recent Orders & Shipments
        public List<Order> RecentOrders { get; set; } = new();
        public List<Shipment> PendingShipments { get; set; } = new();
        public List<Review> RecentReviews { get; set; } = new();
    }

    // =========================================================
    // HELPER DTOs
    // =========================================================
    public class DailySalesTrendDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
    }

    public class CategorySalesDistributionDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int ItemsSold { get; set; }
        public decimal Revenue { get; set; }
        public double Percentage { get; set; }
        public string ColorHex { get; set; } = string.Empty;
    }

    public class InventoryHealthDto
    {
        public int TotalVariants { get; set; }
        public int InStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public double HealthScorePercentage { get; set; }
    }

    public class OrderFulfillmentStatsDto
    {
        public int TotalOrders { get; set; }
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int ShippedCount { get; set; }
        public int DeliveredCount { get; set; }
        public int CancelledCount { get; set; }
        public double FulfillmentRate { get; set; }
    }

    public class PaymentDistributionDto
    {
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedOrCancelledCount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
    }

    public class LowStockAlertDto
    {
        public Guid VariantId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class TopProductDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public string? ImageUrl { get; set; }
        public int CurrentStock { get; set; }
    }

    // Legacy support to ensure backward compatibility
    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }
}