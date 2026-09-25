using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Models.ViewModels;
using WebApplication_ClothingEcommerce.Services;

using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IHomeService homeService,
            AppDbContext context,
            IServiceScopeFactory scopeFactory,
            ILogger<HomeController> logger)
        {
            _homeService = homeService;
            _context = context;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // =====================================================
        // HOME
        // GET: /
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _homeService.GetHomeIndexDataAsync();

            ViewBag.Categories = data.Categories;
            ViewBag.Brands = data.Brands;

            try
            {
                ViewBag.HomePageSettings = await _context.HomePageSettings.FirstOrDefaultAsync() ?? new HomePageSettings();
            }
            catch
            {
                ViewBag.HomePageSettings = new HomePageSettings();
            }

            return View(data.LatestProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return Redirect("/Home/About#contact");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(msg => !string.IsNullOrWhiteSpace(msg));

                TempData["ErrorMessage"] = string.Join(" ", errorMessages);
                return Redirect("/Home/About#contact");
            }

            try
            {
                // 1. Persist message to database so admin can manage it
                var contactMessage = new ContactMessage
                {
                    Name    = model.Name?.Trim() ?? string.Empty,
                    Email   = model.Email?.Trim() ?? string.Empty,
                    Phone   = model.Phone?.Trim(),
                    Subject = model.Subject?.Trim() ?? string.Empty,
                    Message = model.Message?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.ContactMessages.Add(contactMessage);
                await _context.SaveChangesAsync();

                // 2. Dispatch notifications asynchronously (Admin Telegram, Admin Email, User Confirmation Email)
                var name = contactMessage.Name;
                var email = contactMessage.Email;
                var phone = contactMessage.Phone;
                var subject = contactMessage.Subject;
                var msg = contactMessage.Message;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var scopedTelegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();
                        var scopedEmail = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        // A. Admin Telegram notification
                        try
                        {
                            await scopedTelegram.SendContactInquiryNotificationAsync(name, email, phone, subject, msg);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send Telegram contact inquiry notification for {Email}", email);
                        }

                        // B. Admin Email notification
                        try
                        {
                            await scopedEmail.SendContactInquiryAdminNotificationAsync(name, email, phone, subject, msg);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send Admin email contact notification for {Email}", email);
                        }

                        // C. User Confirmation Email
                        try
                        {
                            await scopedEmail.SendContactInquiryCustomerConfirmationAsync(name, email, phone, subject, msg);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send User confirmation email for {Email}", email);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to execute notification scope for contact inquiry from {Email}", email);
                    }
                });

                TempData["SuccessMessage"] = $"Thank you, {contactMessage.Name}! Your message has been received by our support team. We have sent a confirmation email to {contactMessage.Email} and will get back to you shortly.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process contact inquiry submission");
                TempData["ErrorMessage"] = "We encountered a problem saving your message. Please try again or reach out to us directly on Telegram @khmerclothingstore_bot or call 096 914 4183.";
            }

            return Redirect("/Home/About#contact");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new WebApplication_ClothingEcommerce.Models.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}