using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AdminReportsViewModel> GetFinancialReportAsync(string range, DateTime? customStart = null, DateTime? customEnd = null)
        {
            range = string.IsNullOrWhiteSpace(range) ? "1month" : range.ToLowerInvariant();
            var now = DateTime.UtcNow;

            DateTime startDate;
            DateTime endDate = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, DateTimeKind.Utc);

            switch (range)
            {
                case "1week":
                    startDate = now.Date.AddDays(-6);
                    break;
                case "6months":
                    startDate = now.Date.AddMonths(-6);
                    break;
                case "year":
                    startDate = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    break;
                case "custom":
                    startDate = customStart.HasValue 
                        ? DateTime.SpecifyKind(customStart.Value.Date, DateTimeKind.Utc) 
                        : now.Date.AddDays(-29);
                    endDate = customEnd.HasValue 
                        ? DateTime.SpecifyKind(customEnd.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) 
                        : endDate;
                    break;
                case "1month":
                default:
                    range = "1month";
                    startDate = now.Date.AddDays(-29);
                    break;
            }

            // Pure data query from repository
            var orders = await _unitOfWork.Orders.GetOrdersByDateRangeAsync(startDate, endDate);

            var validOrders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
            var paidOrders = orders.Where(o => o.Payment?.PaymentStatus == PaymentStatus.Completed || o.Status == OrderStatus.Delivered).ToList();
            var cancelledOrders = orders.Where(o => o.Status == OrderStatus.Cancelled).ToList();

            decimal grossIncome = validOrders.Sum(o => o.TotalAmount);

            // Business Calculation: Outcome / Operating Expenses
            // - Estimated Cost of Goods Sold (COGS): 50% of items subtotal
            // - Shipping & fulfillment delivery expense: $2.00 per dispatched order
            // - Payment gateway transaction fee: 1.5% of processed transactions
            decimal cogs = validOrders.Sum(o => o.Items.Sum(i => i.Quantity * i.UnitPrice) * 0.50m);
            decimal deliveryExpenses = validOrders.Count * 2.00m;
            decimal processingFees = grossIncome * 0.015m;
            decimal totalOutcome = Math.Round(cogs + deliveryExpenses + processingFees, 2);

            decimal netProfit = Math.Round(grossIncome - totalOutcome, 2);
            decimal profitMargin = grossIncome > 0 ? Math.Round((netProfit / grossIncome) * 100m, 1) : 0m;

            int totalOrdersCount = orders.Count;
            decimal aov = validOrders.Count > 0 ? Math.Round(grossIncome / validOrders.Count, 2) : 0m;
            int totalItemsSold = validOrders.Sum(o => o.Items.Sum(i => i.Quantity));

            // Timeline points
            var timeline = GenerateTimeline(startDate, endDate, validOrders, range);

            // Payment Methods Breakdown
            var paymentGroups = validOrders
                .GroupBy(o => o.Payment?.PaymentMethod?.Name ?? "Cash on Delivery")
                .Select(g => new PaymentMethodReportItem
                {
                    MethodName = g.Key,
                    OrdersCount = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    Percentage = grossIncome > 0 ? Math.Round((g.Sum(o => o.TotalAmount) / grossIncome) * 100m, 1) : 0m
                })
                .OrderByDescending(p => p.TotalAmount)
                .ToList();

            // Status Breakdown
            var statusGroups = orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusReportItem
                {
                    Status = g.Key,
                    StatusName = g.Key.ToString(),
                    Count = g.Count(),
                    Percentage = totalOrdersCount > 0 ? Math.Round(((decimal)g.Count() / totalOrdersCount) * 100m, 1) : 0m
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            // Top Products Sold
            var topProducts = validOrders
                .SelectMany(o => o.Items)
                .Where(i => i.Variant?.Product != null)
                .GroupBy(i => new { i.Variant!.ProductId, i.Variant.Product!.Name })
                .Select(g => new TopSellingProductItem
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(tp => tp.QuantitySold)
                .Take(5)
                .ToList();

            return new AdminReportsViewModel
            {
                Range = range,
                StartDate = startDate,
                EndDate = endDate,
                GrossIncome = grossIncome,
                TotalOutcome = totalOutcome,
                NetProfit = netProfit,
                ProfitMargin = profitMargin,
                TotalOrders = totalOrdersCount,
                PaidOrdersCount = paidOrders.Count,
                PendingOrdersCount = orders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing),
                CancelledOrdersCount = cancelledOrders.Count,
                AverageOrderValue = aov,
                TotalItemsSold = totalItemsSold,
                Timeline = timeline,
                PaymentBreakdown = paymentGroups,
                StatusBreakdown = statusGroups,
                TopProducts = topProducts,
                Orders = orders.ToList()
            };
        }

        private List<DailyFinancialDataPoint> GenerateTimeline(DateTime start, DateTime end, List<Order> orders, string range)
        {
            var list = new List<DailyFinancialDataPoint>();

            if (range == "6months" || range == "year" || (end - start).TotalDays > 60)
            {
                // Monthly grouping
                var cur = new DateTime(start.Year, start.Month, 1);
                var endMonth = new DateTime(end.Year, end.Month, 1);

                while (cur <= endMonth)
                {
                    var next = cur.AddMonths(1);
                    var monthOrders = orders.Where(o => o.OrderDate >= cur && o.OrderDate < next).ToList();
                    decimal inc = monthOrders.Sum(o => o.TotalAmount);
                    decimal cogs = monthOrders.Sum(o => o.Items.Sum(i => i.Quantity * i.UnitPrice) * 0.50m);
                    decimal outc = Math.Round(cogs + (monthOrders.Count * 2.00m) + (inc * 0.015m), 2);

                    list.Add(new DailyFinancialDataPoint
                    {
                        Date = cur,
                        Label = cur.ToString("MMM yyyy"),
                        Income = inc,
                        Outcome = outc,
                        Profit = Math.Round(inc - outc, 2),
                        OrdersCount = monthOrders.Count
                    });

                    cur = next;
                }
            }
            else
            {
                // Daily grouping
                for (var dt = start.Date; dt <= end.Date; dt = dt.AddDays(1))
                {
                    var nextDay = dt.AddDays(1);
                    var dayOrders = orders.Where(o => o.OrderDate >= dt && o.OrderDate < nextDay).ToList();
                    decimal inc = dayOrders.Sum(o => o.TotalAmount);
                    decimal cogs = dayOrders.Sum(o => o.Items.Sum(i => i.Quantity * i.UnitPrice) * 0.50m);
                    decimal outc = Math.Round(cogs + (dayOrders.Count * 2.00m) + (inc * 0.015m), 2);

                    list.Add(new DailyFinancialDataPoint
                    {
                        Date = dt,
                        Label = dt.ToString("dd MMM"),
                        Income = inc,
                        Outcome = outc,
                        Profit = Math.Round(inc - outc, 2),
                        OrdersCount = dayOrders.Count
                    });
                }
            }

            return list;
        }

        public async Task<byte[]> GenerateCsvReportAsync(string range, DateTime? customStart = null, DateTime? customEnd = null)
        {
            var report = await GetFinancialReportAsync(range, customStart, customEnd);
            var sb = new StringBuilder();

            // Header info
            sb.AppendLine($"CLOTHE FINANCIAL & SALES REPORT");
            sb.AppendLine($"Range,{report.Range},Period,{report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Gross Income,${report.GrossIncome:F2},Total Outcome,${report.TotalOutcome:F2},Net Profit,${report.NetProfit:F2},Profit Margin,{report.ProfitMargin}%");
            sb.AppendLine($"Total Orders,{report.TotalOrders},AOV,${report.AverageOrderValue:F2},Items Sold,{report.TotalItemsSold}");
            sb.AppendLine();

            // Daily / Timeline table
            sb.AppendLine("Date / Period,Orders,Gross Income ($),Estimated Outcome ($),Net Profit ($)");
            foreach (var point in report.Timeline)
            {
                sb.AppendLine($"\"{point.Label}\",{point.OrdersCount},{point.Income:F2},{point.Outcome:F2},{point.Profit:F2}");
            }
            sb.AppendLine();

            // Detailed Orders ledger
            sb.AppendLine("Order ID,Date (UTC),Customer,Payment Method,Status,Total Amount ($),Estimated Outcome ($),Net Profit ($)");
            foreach (var ord in report.Orders)
            {
                var custName = ord.Customer != null ? $"{ord.Customer.FirstName} {ord.Customer.LastName}".Trim() : "Guest";
                var method = ord.Payment?.PaymentMethod?.Name ?? "COD";
                decimal ordOutcome = ord.Status != OrderStatus.Cancelled
                    ? Math.Round((ord.Items.Sum(i => i.Quantity * i.UnitPrice) * 0.50m) + 2.00m + (ord.TotalAmount * 0.015m), 2)
                    : 0m;
                decimal ordProfit = ord.Status != OrderStatus.Cancelled
                    ? Math.Round(ord.TotalAmount - ordOutcome, 2)
                    : 0m;

                sb.AppendLine($"\"{ord.Id}\",\"{ord.OrderDate:yyyy-MM-dd HH:mm}\",\"{custName.Replace("\"", "\"\"")}\",\"{method}\",\"{ord.Status}\",{ord.TotalAmount:F2},{ordOutcome:F2},{ordProfit:F2}");
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }
    }
}
