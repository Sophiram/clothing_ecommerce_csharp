using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // =========================================================
        // FINANCIAL & SALES REPORT
        // GET: /Admin/Reports?range=1week|1month|6months|year|custom
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string range = "1month", 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            var model = await _reportService.GetFinancialReportAsync(range, startDate, endDate);
            return View(model);
        }

        // =========================================================
        // EXPORT CSV
        // GET: /Admin/Reports/ExportCsv?range=...&startDate=...&endDate=...
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            string range = "1month", 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            var csvBytes = await _reportService.GenerateCsvReportAsync(range, startDate, endDate);
            var fileName = $"Financial_Report_{range}_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";

            return File(csvBytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
