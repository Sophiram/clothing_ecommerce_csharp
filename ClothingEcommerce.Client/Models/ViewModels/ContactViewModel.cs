using System.ComponentModel.DataAnnotations;

namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number (optional)")]
        [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Please select or enter a subject.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your message.")]
        [StringLength(2000, MinimumLength = 2, ErrorMessage = "Message must be at least 2 characters long.")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;
    }
}
