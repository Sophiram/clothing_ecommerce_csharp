using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AuditLogsController : Controller
    {
        private readonly IAuditService _auditService;
        private readonly WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService _avatarService;

        public AuditLogsController(
            IAuditService auditService,
            WebApplication_ClothingEcommerce.Services.Interfaces.IAvatarService avatarService)
        {
            _auditService = auditService;
            _avatarService = avatarService;
        }

        // =====================================================
        // GET: /Admin/AuditLogs
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search = null,
            string? actionFilter = null,
            string? entityFilter = null,
            int page = 1)
        {
            var result = await _auditService.GetAuditLogsAsync(search, actionFilter, entityFilter, page);

            var userAvatars = new Dictionary<string, string?>();
            foreach (var log in result.Logs)
            {
                if (!string.IsNullOrEmpty(log.UserId) && !userAvatars.ContainsKey(log.UserId))
                {
                    userAvatars[log.UserId] = _avatarService.GetAvatarUrl(log.UserId);
                }
            }

            ViewBag.Search = search;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.EntityFilter = entityFilter;
            ViewBag.CurrentPage = result.CurrentPage;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.TodayCount = result.TodayCount;
            ViewBag.DistinctActions = result.DistinctActions;
            ViewBag.DistinctEntities = result.DistinctEntities;
            ViewBag.UserAvatars = userAvatars;

            return View(result.Logs);
        }

        // =====================================================
        // POST: /Admin/AuditLogs/ClearOlderThan
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearOlderThan(int days = 30)
        {
            var deletedCount = await _auditService.ClearOlderThanAsync(days);

            if (deletedCount > 0)
            {
                TempData["Success"] = $"Cleared {deletedCount} audit log entries older than {days} days.";
            }
            else
            {
                TempData["Info"] = $"No audit log entries older than {days} days were found.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
