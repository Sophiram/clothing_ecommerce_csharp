using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetOrderWithDetailsAsync(Guid orderId);
        Task<IReadOnlyList<Order>> GetOrdersByCustomerIdAsync(Guid customerId);
        Task<IReadOnlyList<Order>> GetAllOrdersWithDetailsAsync();
        Task<IReadOnlyList<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Payment?> GetPaymentByOrderIdAsync(Guid orderId);
    }
}
