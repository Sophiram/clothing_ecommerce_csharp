using System;
using System.Collections.Generic;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class AdminReportsViewModel
    {
        public string Range { get; set; } = "1month";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Top Financial Metrics
        public decimal GrossIncome { get; set; }
        public decimal TotalOutcome { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMargin { get; set; }

        // Order Analytics
        public int TotalOrders { get; set; }
        public int PaidOrdersCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int CancelledOrdersCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItemsSold { get; set; }

        // Breakdowns
        public List<DailyFinancialDataPoint> Timeline { get; set; } = new();
        public List<PaymentMethodReportItem> PaymentBreakdown { get; set; } = new();
        public List<OrderStatusReportItem> StatusBreakdown { get; set; } = new();
        public List<TopSellingProductItem> TopProducts { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
    }

    public class DailyFinancialDataPoint
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Outcome { get; set; }
        public decimal Profit { get; set; }
        public int OrdersCount { get; set; }
    }

    public class PaymentMethodReportItem
    {
        public string MethodName { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class OrderStatusReportItem
    {
        public OrderStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TopSellingProductItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
