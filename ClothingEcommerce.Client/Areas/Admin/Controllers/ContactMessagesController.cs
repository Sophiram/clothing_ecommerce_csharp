using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ContactMessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public ContactMessagesController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =========================================================
        // GET: /Admin/ContactMessages
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null, bool? unreadOnly = null)
        {
            var query = _context.ContactMessages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(s) ||
                    m.Email.ToLower().Contains(s) ||
                    m.Subject.ToLower().Contains(s));
            }

            if (unreadOnly == true)
                query = query.Where(m => !m.IsRead);

            var messages = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

            ViewBag.Search = search;
            ViewBag.UnreadOnly = unreadOnly;
            ViewBag.TotalCount = await _context.ContactMessages.CountAsync();
            ViewBag.UnreadCount = await _context.ContactMessages.CountAsync(m => !m.IsRead);

            return View(messages);
        }

        // =========================================================
        // GET: /Admin/ContactMessages/Details/{id}
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            // Mark as read when opened
            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // =========================================================
        // POST: /Admin/ContactMessages/MarkRead/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();
            message.IsRead = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // POST: /Admin/ContactMessages/MarkUnread/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnread(Guid id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();
            message.IsRead = false;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // POST: /Admin/ContactMessages/Delete/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Message deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // POST: /Admin/ContactMessages/Reply/{id}
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(Guid id, string replyBody)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            if (string.IsNullOrWhiteSpace(replyBody))
            {
                TempData["ErrorMessage"] = "Reply message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; background: #f8fafc; color: #1e293b; margin: 0; padding: 24px;'>
    <div style='background: #ffffff; max-width: 600px; margin: 0 auto; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);'>
        <div style='background: #0f172a; color: white; padding: 24px; text-align: center;'>
            <h1 style='margin: 0; font-size: 22px; font-weight: 800;'>CLOTHÉ</h1>
            <p style='margin: 4px 0 0 0; color: #94a3b8; font-size: 13px;'>Customer Support Reply</p>
        </div>
        <div style='padding: 24px; font-size: 14px; line-height: 1.6; color: #334155;'>
            <p>Dear <strong>{message.Name}</strong>,</p>
            <div style='margin: 16px 0; padding: 16px; background: #f8fafc; border-radius: 12px; border-left: 4px solid #2563eb; color: #0f172a; white-space: pre-wrap;'>{replyBody}</div>
            <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;'/>
            <p style='font-size: 12px; color: #64748b;'><strong>Your original message:</strong><br/><em>""{message.Message}""</em></p>
        </div>
        <div style='text-align: center; padding: 16px; font-size: 12px; color: #94a3b8; border-top: 1px solid #f1f5f9;'>
            &copy; {DateTime.UtcNow.Year} CLOTHÉ Clothing Store. Need more help? Telegram: @@khmerclothingstore_bot
        </div>
    </div>
</body>
</html>";

            var sent = await _emailService.SendEmailAsync(message.Email, $"[CLOTHÉ Support] Re: {message.Subject}", html);
            if (sent)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Reply successfully sent to {message.Email}.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Could not deliver email to {message.Email}. Please verify SMTP credentials.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
