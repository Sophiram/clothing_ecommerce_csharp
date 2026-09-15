using WebApplication_ClothingEcommerce.Models.ViewModels;

namespace WebApplication_ClothingEcommerce.Services
{
    public class AuthResult
    {
        public bool Succeeded { get; set; }
        public bool IsLockedOut { get; set; }
        public bool IsStaff { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public interface IAccountService
    {
        Task<AuthResult> RegisterAsync(RegisterViewModel model);
        Task<AuthResult> LoginAsync(LoginViewModel model);
        Task LogoutAsync();
    }
}
