using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly AppDbContext _context;

        public DeliveryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DeliveryMethod>> GetActiveDeliveryMethodsAsync()
        {
            return await _context.DeliveryMethods
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<DeliveryMethod>> GetAllDeliveryMethodsAsync()
        {
            return await _context.DeliveryMethods
                .AsNoTracking()
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();
        }

        public async Task<DeliveryMethod?> GetDeliveryMethodByCodeAsync(string code)
        {
            return await _context.DeliveryMethods
                .FirstOrDefaultAsync(d => d.Code == code);
        }

        public async Task<bool> UpdateDeliveryMethodAsync(DeliveryMethod method)
        {
            var existing = await _context.DeliveryMethods.FindAsync(method.Id);
            if (existing == null) return false;

            existing.Name = method.Name;
            existing.KhmerName = method.KhmerName;
            existing.BaseFee = method.BaseFee;
            existing.EstimatedDeliveryTime = method.EstimatedDeliveryTime;
            existing.IsActive = method.IsActive;
            existing.DisplayOrder = method.DisplayOrder;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DeliveryBranch>> GetBranchesByCarrierAsync(string carrierCode = "VETExpress")
        {
            return await _context.DeliveryBranches
                .AsNoTracking()
                .Where(b => b.CarrierCode == carrierCode && b.IsActive)
                .OrderBy(b => b.Province)
                .ThenBy(b => b.BranchName)
                .ToListAsync();
        }

        public async Task<List<string>> GetProvincesAsync(string carrierCode = "VETExpress")
        {
            return await _context.DeliveryBranches
                .AsNoTracking()
                .Where(b => b.CarrierCode == carrierCode && b.IsActive)
                .Select(b => b.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }

        public async Task<List<DeliveryBranch>> GetBranchesByProvinceAsync(string province, string carrierCode = "VETExpress")
        {
            return await _context.DeliveryBranches
                .AsNoTracking()
                .Where(b => b.CarrierCode == carrierCode && b.Province == province && b.IsActive)
                .OrderBy(b => b.BranchName)
                .ToListAsync();
        }

        public async Task<bool> AddBranchAsync(DeliveryBranch branch)
        {
            if (branch.Id == Guid.Empty) branch.Id = Guid.NewGuid();
            _context.DeliveryBranches.Add(branch);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateBranchAsync(DeliveryBranch branch)
        {
            var existing = await _context.DeliveryBranches.FindAsync(branch.Id);
            if (existing == null) return false;

            existing.Province = branch.Province;
            existing.BranchName = branch.BranchName;
            existing.Address = branch.Address;
            existing.Phone = branch.Phone;
            existing.IsActive = branch.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBranchAsync(Guid id)
        {
            var branch = await _context.DeliveryBranches.FindAsync(id);
            if (branch == null) return false;

            _context.DeliveryBranches.Remove(branch);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
