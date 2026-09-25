using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public interface ICustomerService
    {
        // Customer Admin Operations
        Task<(List<Customer> Customers, int TotalCount, int ActiveCount)> GetCustomersAsync(string? search, CustomerStatus? status);
        Task<Customer?> GetCustomerDetailsAsync(Guid id);
        Task<Customer?> GetCustomerByIdAsync(Guid id);
        Task<ServiceResult> UpdateCustomerAsync(Guid id, Customer model, string? user = null, string? ip = null);
        Task<ServiceResult> ToggleCustomerStatusAsync(Guid id, string? user = null, string? ip = null);
        Task<ServiceResult> DeleteCustomerAsync(Guid id, string? user = null, string? ip = null);

        // Address Admin Operations
        Task<List<Address>> GetAllAddressesAsync();
        Task<Address?> GetAddressByIdAsync(Guid id);
        Task<ServiceResult> CreateAddressAsync(Address address);
        Task<ServiceResult> UpdateAddressAsync(Guid id, Address address);
        Task<ServiceResult> DeleteAddressAsync(Guid id);
    }
}
