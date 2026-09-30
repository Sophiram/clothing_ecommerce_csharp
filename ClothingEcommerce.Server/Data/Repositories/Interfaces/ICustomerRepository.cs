using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetByUserIdAsync(string userId);
        Task<Customer?> GetByEmailAsync(string email);
        Task<Customer?> GetByUserIdOrEmailAsync(string? userId, string? email);
        Task<Customer?> GetCustomerWithDetailsAsync(Guid customerId);
    }
}
