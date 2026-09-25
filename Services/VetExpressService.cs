using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class VetExpressService : IVetExpressService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VetExpressService> _logger;

        public VetExpressService(AppDbContext context, ILogger<VetExpressService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<string>> GetProvincesAsync()
        {
            var provinces = await _context.DeliveryBranches
                .AsNoTracking()
                .Where(b => b.CarrierCode == "VETExpress" && b.IsActive)
                .Select(b => b.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            if (!provinces.Any())
            {
                provinces = new List<string>
                {
                    "រាជធានីភ្នំពេញ",
                    "ខេត្តសៀមរាប",
                    "ខេត្តបាត់ដំបង",
                    "ខេត្តព្រះសីហនុ",
                    "ខេត្តកំពង់ចាម",
                    "ខេត្តកំពត",
                    "ខេត្តកណ្តាល",
                    "ខេត្តស្វាយរៀង",
                    "ខេត្តតាកែវ"
                };
            }

            return provinces;
        }

        public async Task<List<DeliveryBranch>> GetBranchesByProvinceAsync(string province)
        {
            var branches = await _context.DeliveryBranches
                .AsNoTracking()
                .Where(b => b.CarrierCode == "VETExpress" && b.Province == province && b.IsActive)
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            return branches;
        }

        public async Task<decimal> CalculateDeliveryFeeAsync(string province, decimal totalAmount)
        {
            // Standard VET Express rate: $2.00 across Cambodia
            await Task.CompletedTask;
            return 2.00m;
        }

        public async Task<string> CreateWaybillAsync(Order order, string branchName)
        {
            await Task.CompletedTask;
            var waybillNo = $"VET-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            _logger.LogInformation("Generated VET Express Waybill {Waybill} for Order {OrderId} to branch {Branch}", waybillNo, order.Id, branchName);
            return waybillNo;
        }
    }
}
