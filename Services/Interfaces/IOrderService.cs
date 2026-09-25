using WebApplication_ClothingEcommerce.Data.Enums;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class OrderPlacementResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? OrderId { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class CustomerOrdersResult
    {
        public List<Order> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public OrderStatus? CurrentStatus { get; set; }
        public int AllCount { get; set; }
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int ShippedCount { get; set; }
        public int DeliveredCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public interface IOrderService
    {
        Task<CheckoutViewModel?> PrepareCheckoutAsync(Guid customerId);
        Task<OrderPlacementResult> PlaceOrderAsync(Guid customerId, CheckoutViewModel model);
        Task<CustomerOrdersResult> GetCustomerOrdersAsync(Guid customerId, OrderStatus? status, int page = 1, int pageSize = 10);
        Task<Order?> GetCustomerOrderByIdAsync(Guid customerId, Guid orderId);
        Task<(bool Success, string Message)> CancelCustomerOrderAsync(Guid customerId, Guid orderId);

        // Admin operations
        Task<List<Order>> GetAdminOrdersAsync(string? search = null, OrderStatus? status = null);
        Task<Order?> GetAdminOrderByIdAsync(Guid orderId);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
        Task<bool> DeleteOrderAsync(Guid orderId);
        Task<(List<Customer> Customers, List<Address> Addresses)> GetOrderCreateDropdownDataAsync();
        Task<ServiceResult> CreateAdminOrderAsync(Order order);

        // Order Items operations
        Task<List<OrderItem>> GetAllOrderItemsAsync();
        Task<OrderItem?> GetOrderItemByIdAsync(Guid id);
        Task<ServiceResult> CreateOrderItemAsync(OrderItem item);
        Task<ServiceResult> UpdateOrderItemAsync(Guid id, OrderItem item);
        Task<ServiceResult> DeleteOrderItemAsync(Guid id);
    }
}
