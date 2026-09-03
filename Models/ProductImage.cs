using System.ComponentModel.DataAnnotations;

namespace WebApplication_ClothingEcommerce.Models
{
    public class ProductImage
    {
        // =========================
        // PRIMARY KEY
        // =========================

        public Guid Id { get; set; }


        // =========================
        // FOREIGN KEY
        // =========================

        [Required]
        public Guid ProductId { get; set; }


        // =========================
        // IMAGE URL
        // =========================

        [Required(ErrorMessage = "Image URL is required.")]
        public string ImageUrl { get; set; } = string.Empty;


        // =========================
        // PRIMARY IMAGE
        // =========================

        public bool IsPrimary { get; set; }


        // =========================
        // NAVIGATION
        // =========================

        public Product Product { get; set; } = null!;
    }
}