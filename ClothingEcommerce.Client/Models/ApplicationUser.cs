using Microsoft.AspNetCore.Identity;

namespace WebApplication_ClothingEcommerce.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}