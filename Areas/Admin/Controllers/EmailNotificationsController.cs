using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class EmailNotificationsController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailNotificationsController> _logger;

        public EmailNotificationsController(
            IEmailService emailService, 
            ILogger<EmailNotificationsController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var settings = _emailService.GetSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestEmail(string recipientEmail)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                TempData["Error"] = "Please enter a valid recipient email address.";
                return RedirectToAction(nameof(Index));
            }

            var (success, message) = await _emailService.SendTestEmailAsync(recipientEmail.Trim());
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
