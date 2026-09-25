
using System.ComponentModel.DataAnnotations;

namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class CheckoutViewModel
    {
        // =========================================================
        // CUSTOMER INFORMATION
        // =========================================================

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        // =========================================================
        // SHIPPING & DELIVERY METHOD (RS PRINTER STORE FLOW)
        // =========================================================

        public string DeliveryType { get; set; } = "ExpressDelivery"; // "StorePickup" or "ExpressDelivery"

        public string CarrierCode { get; set; } = "VETExpress"; // "VETExpress", "CityDelivery", "OtherExpress"

        public string SelectedProvince { get; set; } = "រាជធានីភ្នំពេញ";

        public string SelectedBranchName { get; set; } = string.Empty;

        public string DeliveryNote { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;


        // =========================================================
        // PAYMENT
        // =========================================================

        [Required(ErrorMessage = "Please select a payment method.")]
        [Display(Name = "Payment Method")]
        public Guid? PaymentMethodId { get; set; }

        public string? PaymentTransactionRef { get; set; }

        public bool IsPaymentConfirmed { get; set; }


        // =========================================================
        // ORDER SUMMARY
        // =========================================================

        public List<CheckoutItem> Items { get; set; }
            = new List<CheckoutItem>();

        public decimal SubTotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public decimal Total { get; set; }
    }


    // =============================================================
    // CHECKOUT ITEM
    // =============================================================

    public class CheckoutItem
    {
        public Guid VariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal Subtotal =>
            UnitPrice * Quantity;
    }
}
