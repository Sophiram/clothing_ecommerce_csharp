using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class AuditLogsResult
    {
        public List<AuditLog> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int TodayCount { get; set; }
        public List<string> DistinctActions { get; set; } = new();
        public List<string> DistinctEntities { get; set; } = new();
    }

    public interface IAuditService
    {
        Task LogAsync(string? userId, string? userEmail, string action, string entityName, string? entityId = null, string? description = null, string? ipAddress = null);
        Task<AuditLogsResult> GetAuditLogsAsync(string? search, string? actionFilter, string? entityFilter, int page, int pageSize = 20);
        Task<int> ClearOlderThanAsync(int days);
    }
}
