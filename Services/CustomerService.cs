using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _context;
        private readonly IAuditService _auditService;

        public CustomerService(AppDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<(List<Customer> Customers, int TotalCount, int ActiveCount)> GetCustomersAsync(string? search, CustomerStatus? status)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.Reviews)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(c =>
                    c.FirstName.Contains(search) ||
                    c.LastName.Contains(search) ||
                    c.Email.Contains(search) ||
                    (c.Phone != null && c.Phone.Contains(search))
                );
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var totalCount = await _context.Customers.CountAsync();
            var activeCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);

            return (customers, totalCount, activeCount);
        }

        public async Task<Customer?> GetCustomerDetailsAsync(Guid id)
        {
            return await _context.Customers
                .Include(c => c.ApplicationUser)
                .Include(c => c.Addresses)
                .Include(c => c.Orders).ThenInclude(o => o.Items)
                .Include(c => c.Reviews).ThenInclude(r => r.Product)
                .Include(c => c.Wishlist).ThenInclude(w => w!.Items)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Customer?> GetCustomerByIdAsync(Guid id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task<ServiceResult> UpdateCustomerAsync(Guid id, Customer model, string? user = null, string? ip = null)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return ServiceResult.Fail("Customer not found.");
            }

            customer.FirstName = model.FirstName?.Trim() ?? string.Empty;
            customer.LastName = model.LastName?.Trim() ?? string.Empty;
            customer.Phone = model.Phone?.Trim();
            customer.Status = model.Status;

            _context.Update(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                user,
                user,
                "EditCustomer",
                "Customer",
                customer.Id.ToString(),
                $"Updated customer {customer.FirstName} {customer.LastName} ({customer.Email}) - Status: {customer.Status}",
                ip
            );

            return ServiceResult.Ok($"Customer '{customer.FirstName} {customer.LastName}' updated successfully.");
        }

        public async Task<ServiceResult> ToggleCustomerStatusAsync(Guid id, string? user = null, string? ip = null)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return ServiceResult.Fail("Customer not found.");
            }

            customer.Status = (customer.Status == CustomerStatus.Active) ? CustomerStatus.Inactive : CustomerStatus.Active;
            _context.Update(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                user,
                user,
                "ToggleCustomerStatus",
                "Customer",
                customer.Id.ToString(),
                $"Changed customer {customer.Email} status to {customer.Status}",
                ip
            );

            return ServiceResult.Ok($"Customer '{customer.FirstName} {customer.LastName}' status updated to {customer.Status}.");
        }

        public async Task<ServiceResult> DeleteCustomerAsync(Guid id, string? user = null, string? ip = null)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return ServiceResult.Fail("Customer not found.");
            }

            if (customer.Orders.Any())
            {
                return ServiceResult.Fail($"Cannot delete customer '{customer.FirstName} {customer.LastName}' because they have {customer.Orders.Count} associated orders. Deactivate their account instead.");
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                user,
                user,
                "DeleteCustomer",
                "Customer",
                customer.Id.ToString(),
                $"Deleted customer record '{customer.FirstName} {customer.LastName}' ({customer.Email})",
                ip
            );

            return ServiceResult.Ok($"Customer '{customer.FirstName} {customer.LastName}' deleted successfully.");
        }

        public async Task<List<Address>> GetAllAddressesAsync()
        {
            return await _context.Addresses.AsNoTracking().ToListAsync();
        }

        public async Task<Address?> GetAddressByIdAsync(Guid id)
        {
            return await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ServiceResult> CreateAddressAsync(Address address)
        {
            if (address.Id == Guid.Empty)
            {
                address.Id = Guid.NewGuid();
            }

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Address created successfully.");
        }

        public async Task<ServiceResult> UpdateAddressAsync(Guid id, Address address)
        {
            var existing = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null)
            {
                return ServiceResult.Fail("Address not found.");
            }

            existing.CustomerId = address.CustomerId;
            existing.Province = address.Province;
            existing.City = address.City;
            existing.Street = address.Street;
            existing.PostalCode = address.PostalCode;
            existing.IsDefault = address.IsDefault;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Address updated successfully.");
        }

        public async Task<ServiceResult> DeleteAddressAsync(Guid id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
            {
                return ServiceResult.Fail("Address not found.");
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Address deleted successfully.");
        }
    }
}
