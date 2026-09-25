using System.ComponentModel.DataAnnotations;

namespace WebApplication_ClothingEcommerce.Models
{
    public class HomePageSettings
    {
        [Key]
        public int Id { get; set; }

        // ── PROMO BANNER ──
        [StringLength(200)]
        public string PromoTag { get; set; } = "Flash Sale";

        [StringLength(500)]
        public string PromoText { get; set; } = "Season Collection — Up to 40% OFF essentials & trending styles!";

        [StringLength(200)]
        public string PromoCtaText { get; set; } = "Shop Now";

        [StringLength(500)]
        public string PromoCtaUrl { get; set; } = "/Shop?onSale=true";

        // ── HERO SECTION ──
        [StringLength(100)]
        public string HeroLabel { get; set; } = "NEW COLLECTION 2026";

        [StringLength(200)]
        public string HeroTitle { get; set; } = "Wear your identity.";

        [StringLength(100)]
        public string HeroHighlightWord { get; set; } = "identity.";

        public string HeroDescription { get; set; } = "Discover modern clothing designed for everyday confidence, comfort and effortless style.";

        [StringLength(100)]
        public string HeroPrimaryBtnText { get; set; } = "Shop Collection";

        [StringLength(500)]
        public string HeroPrimaryBtnUrl { get; set; } = "/Shop";

        [StringLength(100)]
        public string HeroSecondaryBtnText { get; set; } = "Explore Products";

        [StringLength(500)]
        public string HeroSecondaryBtnUrl { get; set; } = "/Shop";

        [StringLength(500)]
        public string HeroImageUrl { get; set; } = "https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&w=1000&q=85";

        // ── HERO FLOATING CARDS & STATS ──
        [StringLength(100)]
        public string FloatingCard1Title { get; set; } = "New Season";

        [StringLength(100)]
        public string FloatingCard1Sub { get; set; } = "Fresh styles are here";

        [StringLength(100)]
        public string FloatingCard2Title { get; set; } = "Free Shipping";

        [StringLength(100)]
        public string FloatingCard2Sub { get; set; } = "On orders over $50";

        [StringLength(50)]
        public string Stat1Value { get; set; } = "500+";

        [StringLength(50)]
        public string Stat1Label { get; set; } = "PRODUCTS";

        [StringLength(50)]
        public string Stat2Value { get; set; } = "50+";

        [StringLength(50)]
        public string Stat2Label { get; set; } = "BRANDS";

        [StringLength(50)]
        public string Stat3Value { get; set; } = "10K+";

        [StringLength(50)]
        public string Stat3Label { get; set; } = "CUSTOMERS";
    }
}
