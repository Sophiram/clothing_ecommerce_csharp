using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication_ClothingEcommerce.Data;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class HomePageController : Controller
    {
        private readonly AppDbContext _context;

        public HomePageController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/HomePage
        public async Task<IActionResult> Index()
        {
            var settings = await _context.HomePageSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new HomePageSettings { Id = 0 };
                _context.HomePageSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        // POST: Admin/HomePage/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(HomePageSettings model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Failed to save homepage settings. Please check the form data.";
                return View("Index", model);
            }

            var existing = await _context.HomePageSettings.FirstOrDefaultAsync(s => s.Id == model.Id);
            if (existing == null)
            {
                model.Id = 0;
                _context.HomePageSettings.Add(model);
            }
            else
            {
                existing.PromoTag = model.PromoTag;
                existing.PromoText = model.PromoText;
                existing.PromoCtaText = model.PromoCtaText;
                existing.PromoCtaUrl = model.PromoCtaUrl;

                existing.HeroLabel = model.HeroLabel;
                existing.HeroTitle = model.HeroTitle;
                existing.HeroHighlightWord = model.HeroHighlightWord;
                existing.HeroDescription = model.HeroDescription;
                existing.HeroPrimaryBtnText = model.HeroPrimaryBtnText;
                existing.HeroPrimaryBtnUrl = model.HeroPrimaryBtnUrl;
                existing.HeroSecondaryBtnText = model.HeroSecondaryBtnText;
                existing.HeroSecondaryBtnUrl = model.HeroSecondaryBtnUrl;
                existing.HeroImageUrl = model.HeroImageUrl;

                existing.FloatingCard1Title = model.FloatingCard1Title;
                existing.FloatingCard1Sub = model.FloatingCard1Sub;
                existing.FloatingCard2Title = model.FloatingCard2Title;
                existing.FloatingCard2Sub = model.FloatingCard2Sub;

                existing.Stat1Value = model.Stat1Value;
                existing.Stat1Label = model.Stat1Label;
                existing.Stat2Value = model.Stat2Value;
                existing.Stat2Label = model.Stat2Label;
                existing.Stat3Value = model.Stat3Value;
                existing.Stat3Label = model.Stat3Label;

                _context.Update(existing);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Homepage hero section & banner settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
