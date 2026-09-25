using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace WebApplication_ClothingEcommerce.Services
{
    public class StoreSettingsService : IStoreSettingsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StoreSettingsService> _logger;

        public StoreSettingsService(AppDbContext context, ILogger<StoreSettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StoreReceiptSettings> GetSettingsAsync()
        {
            var settings = await _context.StoreReceiptSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new StoreReceiptSettings
                {
                    StoreName = "CLOTHÉ",
                    StoreNameKh = "ហាងសម្លៀកបំពាក់ CLOTHÉ",
                    Tagline = "Atelier & Luxury Fashion House",
                    TaglineKh = "ម៉ូដទាន់សម័យ និងប្រណីតភាព",
                    Address = "#88 Preah Norodom Blvd, BKK1, Phnom Penh, Cambodia",
                    AddressKh = "អគារលេខ ៨៨ មហាវិថីព្រះនរោត្តម សង្កាត់បឹងកេងកង១ រាជធានីភ្នំពេញ",
                    Phone = "+855 (0) 23 999 888",
                    Email = "info@clothe-atelier.com",
                    Website = "https://clothe-store.com",
                    Telegram = "@clothe_support",
                    VatNumber = "K005-902201889",
                    ReceiptPrefix = "REC-",
                    DefaultReceiptTheme = "ModernLuxury",
                    ReturnPolicyEn = "Items may be exchanged within 7 days of purchase with original receipt and tags attached. No cash refunds.",
                    ReturnPolicyKh = "ទំនិញដែលបានទិញរួចអាចប្តូរបានក្នុងរយៈពេល ៧ ថ្ងៃ ដោយមានវិក្កយបត្រ និងស្លាកសញ្ញាដើម។ មិនមានការបង្វិលប្រាក់វិញទេ។",
                    ThankYouNoteEn = "Thank you for shopping at CLOTHÉ! We appreciate your patronage.",
                    ThankYouNoteKh = "សូមអរគុណសម្រាប់ការគាំទ្រហាងយើងខ្ញុំ! សូមអញ្ជើញមកម្តងទៀត។",
                    ShowDualCurrency = true,
                    ShowKhqr = true,
                    ExchangeRate = 4100m,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                try
                {
                    _context.StoreReceiptSettings.Add(settings);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not persist initial StoreReceiptSettings row.");
                }
            }
            return settings;
        }

        public async Task<bool> UpdateStoreProfileAsync(StoreReceiptSettings model, string updatedBy)
        {
            try
            {
                var settings = await GetSettingsAsync();
                settings.StoreName = model.StoreName?.Trim() ?? "CLOTHÉ";
                settings.StoreNameKh = model.StoreNameKh?.Trim();
                settings.Tagline = model.Tagline?.Trim();
                settings.TaglineKh = model.TaglineKh?.Trim();
                settings.Address = model.Address?.Trim();
                settings.AddressKh = model.AddressKh?.Trim();
                settings.Phone = model.Phone?.Trim();
                settings.Email = model.Email?.Trim();
                settings.Website = model.Website?.Trim();
                settings.Telegram = model.Telegram?.Trim();
                settings.UpdatedAt = DateTime.UtcNow;
                settings.UpdatedBy = updatedBy;

                _context.StoreReceiptSettings.Update(settings);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Store Profile settings.");
                return false;
            }
        }

        public async Task<bool> UpdateReceiptCustomizerAsync(StoreReceiptSettings model, string updatedBy)
        {
            try
            {
                var settings = await GetSettingsAsync();
                settings.VatNumber = model.VatNumber?.Trim();
                settings.ReceiptPrefix = model.ReceiptPrefix?.Trim() ?? "REC-";
                settings.DefaultReceiptTheme = string.IsNullOrWhiteSpace(model.DefaultReceiptTheme) ? "ModernLuxury" : model.DefaultReceiptTheme.Trim();
                settings.ReturnPolicyEn = model.ReturnPolicyEn?.Trim();
                settings.ReturnPolicyKh = model.ReturnPolicyKh?.Trim();
                settings.ThankYouNoteEn = model.ThankYouNoteEn?.Trim();
                settings.ThankYouNoteKh = model.ThankYouNoteKh?.Trim();
                settings.ShowDualCurrency = model.ShowDualCurrency;
                settings.ShowKhqr = model.ShowKhqr;
                if (model.ExchangeRate > 0)
                {
                    settings.ExchangeRate = model.ExchangeRate;
                }
                settings.UpdatedAt = DateTime.UtcNow;
                settings.UpdatedBy = updatedBy;

                _context.StoreReceiptSettings.Update(settings);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Receipt Customizer settings.");
                return false;
            }
        }
    }
}
