using System.ComponentModel.DataAnnotations;

namespace WebApplication_ClothingEcommerce.Models
{
    public class StoreReceiptSettings
    {
        [Key]
        public int Id { get; set; }

        // ==========================================
        // 1. STORE PROFILE & IDENTITY (Store Admin & Super Admin)
        // ==========================================
        [Required]
        [StringLength(100)]
        public string StoreName { get; set; } = "CLOTHÉ";

        [StringLength(150)]
        public string StoreNameKh { get; set; } = "ហាងសម្លៀកបំពាក់ CLOTHÉ";

        [StringLength(200)]
        public string Tagline { get; set; } = "Atelier & Luxury Fashion House";

        [StringLength(250)]
        public string TaglineKh { get; set; } = "ម៉ូដទាន់សម័យ និងប្រណីតភាព";

        [StringLength(300)]
        public string Address { get; set; } = "#88 Preah Norodom Blvd, BKK1, Phnom Penh, Cambodia";

        [StringLength(350)]
        public string AddressKh { get; set; } = "អគារលេខ ៨៨ មហាវិថីព្រះនរោត្តម សង្កាត់បឹងកេងកង១ រាជធានីភ្នំពេញ";

        [StringLength(50)]
        public string Phone { get; set; } = "+855 (0) 23 999 888";

        [StringLength(100)]
        public string Email { get; set; } = "info@clothe-atelier.com";

        [StringLength(200)]
        public string Website { get; set; } = "https://clothe-store.com";

        [StringLength(100)]
        public string Telegram { get; set; } = "@clothe_support";

        // ==========================================
        // 2. RECEIPT & INVOICE CUSTOMIZATION (Super Admin)
        // ==========================================
        [StringLength(50)]
        public string VatNumber { get; set; } = "K005-902201889";

        [StringLength(20)]
        public string ReceiptPrefix { get; set; } = "REC-";

        /// <summary>
        /// Default receipt UI layout preset: "ModernLuxury", "AuthenticThermal", "CorporateInvoice", "MinimalChic"
        /// </summary>
        [StringLength(50)]
        public string DefaultReceiptTheme { get; set; } = "ModernLuxury";

        [StringLength(500)]
        public string ReturnPolicyEn { get; set; } = "Items may be exchanged within 7 days of purchase with original receipt and tags attached. No cash refunds.";

        [StringLength(600)]
        public string ReturnPolicyKh { get; set; } = "ទំនិញដែលបានទិញរួចអាចប្តូរបានក្នុងរយៈពេល ៧ ថ្ងៃ ដោយមានវិក្កយបត្រ និងស្លាកសញ្ញាដើម។ មិនមានការបង្វិលប្រាក់វិញទេ។";

        [StringLength(300)]
        public string ThankYouNoteEn { get; set; } = "Thank you for shopping at CLOTHÉ! We appreciate your patronage.";

        [StringLength(400)]
        public string ThankYouNoteKh { get; set; } = "សូមអរគុណសម្រាប់ការគាំទ្រហាងយើងខ្ញុំ! សូមអញ្ជើញមកម្តងទៀត។";

        public bool ShowDualCurrency { get; set; } = true;

        public bool ShowKhqr { get; set; } = true;

        public decimal ExchangeRate { get; set; } = 4100m;

        // ==========================================
        // 3. AUDIT METADATA
        // ==========================================
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UpdatedBy { get; set; } = "System";
    }
}
