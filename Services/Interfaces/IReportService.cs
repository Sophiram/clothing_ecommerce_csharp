using System;
using System.Threading.Tasks;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services.Interfaces
{
    public interface IReportService
    {
        Task<AdminReportsViewModel> GetFinancialReportAsync(string range, DateTime? customStart = null, DateTime? customEnd = null);
        Task<byte[]> GenerateCsvReportAsync(string range, DateTime? customStart = null, DateTime? customEnd = null);
    }
}
