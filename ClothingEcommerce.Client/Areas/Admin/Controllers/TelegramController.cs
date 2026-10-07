using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class TelegramController : Controller
    {
        private readonly ITelegramService _telegramService;

        public TelegramController(ITelegramService telegramService)
        {
            _telegramService = telegramService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _telegramService.GetSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TelegramSettings model)
        {
            var success = await _telegramService.SaveSettingsAsync(model);
            if (success)
            {
                TempData["Success"] = "Telegram Bot settings updated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to update Telegram settings.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestConnection(string botToken, string chatId)
        {
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            {
                TempData["Error"] = "Please provide both Bot Token and Chat ID to send a test message.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _telegramService.SendTestNotificationAsync(botToken, chatId);
            if (success)
            {
                TempData["Success"] = "Test message sent to Telegram successfully! Check your Telegram chat/channel.";
            }
            else
            {
                TempData["Error"] = "Failed to send test message to Telegram. Please check your Bot Token and Chat ID.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBroadcast(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Announcement message cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _telegramService.SendBroadcastAsync(message);
            if (success)
            {
                TempData["Success"] = "Broadcast announcement dispatched to Telegram successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to dispatch broadcast. Ensure bot credentials are configured and bot is active.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
