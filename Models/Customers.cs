using System.ComponentModel.DataAnnotations;
using WebApplication_ClothingEcommerce.Data.Enums;

namespace WebApplication_ClothingEcommerce.Models
{
    public class Customer
    {
        public Guid Id { get; set; }

        // 🔗 NEW — ភ្ជាប់ទៅ ApplicationUser (Identity)
        public string? ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser? ApplicationUser { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public CustomerStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public Cart? Cart { get; set; }
        public Wishlist? Wishlist { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
