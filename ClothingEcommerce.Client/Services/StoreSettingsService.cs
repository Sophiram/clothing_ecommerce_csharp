using WebApplication_ClothingEcommerce.Models;
using WebApplication_ClothingEcommerce.Services.Interfaces;

namespace ClothingEcommerce.Client.Services
{
    public class ClientStoreSettingsService : IStoreSettingsService
    {
        private static StoreReceiptSettings? _cachedSettings;

        public Task<StoreReceiptSettings> GetSettingsAsync()
        {
            _cachedSettings ??= new StoreReceiptSettings
            {
                StoreName = "CLOTHÉ",
                StoreNameKh = "ក្លូថេ",
                Tagline = "Premium Contemporary Clothing",
                Address = "Phnom Penh, Cambodia",
                Phone = "012 345 678",
                Email = "contact@clothe.com",
                ExchangeRate = 4100m,
                ShowDualCurrency = true,
                ShowKhqr = true
            };

            return Task.FromResult(_cachedSettings);
        }

        public Task<bool> UpdateStoreProfileAsync(StoreReceiptSettings model, string updatedBy)
        {
            _cachedSettings = model;
            return Task.FromResult(true);
        }

        public Task<bool> UpdateReceiptCustomizerAsync(StoreReceiptSettings model, string updatedBy)
        {
            _cachedSettings = model;
            return Task.FromResult(true);
        }
    }
}
