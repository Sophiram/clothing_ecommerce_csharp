using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            string? userId,
            string? userEmail,
            string action,
            string entityName,
            string? entityId = null,
            string? description = null,
            string? ipAddress = null)
        {
            try
            {
                var log = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserEmail = userEmail,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Description = description,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Never crash the calling application if logging fails
            }
        }

        public async Task<AuditLogsResult> GetAuditLogsAsync(string? search, string? actionFilter, string? entityFilter, int page, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(l =>
                    (l.UserEmail != null && l.UserEmail.Contains(search)) ||
                    l.Action.Contains(search) ||
                    l.EntityName.Contains(search) ||
                    (l.Description != null && l.Description.Contains(search)) ||
                    (l.IpAddress != null && l.IpAddress.Contains(search))
                );
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(l => l.Action == actionFilter);
            }

            if (!string.IsNullOrWhiteSpace(entityFilter))
            {
                query = query.Where(l => l.EntityName == entityFilter);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var distinctActions = await _context.AuditLogs
                .AsNoTracking()
                .Select(l => l.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            var distinctEntities = await _context.AuditLogs
                .AsNoTracking()
                .Select(l => l.EntityName)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            var todayUtc = DateTime.UtcNow.Date;
            var todayCount = await _context.AuditLogs
                .AsNoTracking()
                .CountAsync(l => l.CreatedAt >= todayUtc);

            return new AuditLogsResult
            {
                Logs = logs,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                TodayCount = todayCount,
                DistinctActions = distinctActions,
                DistinctEntities = distinctEntities
            };
        }

        public async Task<int> ClearOlderThanAsync(int days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            var logsToDelete = await _context.AuditLogs
                .Where(l => l.CreatedAt < cutoff)
                .ToListAsync();

            if (logsToDelete.Count > 0)
            {
                _context.AuditLogs.RemoveRange(logsToDelete);
                await _context.SaveChangesAsync();
                return logsToDelete.Count;
            }

            return 0;
        }
    }
}
