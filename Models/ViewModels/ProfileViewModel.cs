using System.ComponentModel.DataAnnotations;
using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        public IEnumerable<Address> Addresses { get; set; }
            = new List<Address>();

        // Default Address
        public Address? DefaultAddress => Addresses.FirstOrDefault(a => a.IsDefault) ?? Addresses.FirstOrDefault();

        // Dashboard Statistics
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int WishlistCount { get; set; }
        public int CartCount { get; set; }
        public DateTime MemberSince { get; set; }
        public string Status { get; set; } = "Active";

        // Recent Orders
        public IEnumerable<Order> RecentOrders { get; set; } = new List<Order>();

        // Profile Image
        public string? ProfileImageUrl { get; set; }

        // Navigation State
        public string ActiveTab { get; set; } = "overview";
    }
}