using WebApplication_ClothingEcommerce.Models;

namespace WebApplication_ClothingEcommerce.Models.ViewModels
{
    public class CartViewModel
    {
        public Cart Cart { get; set; } = null!;

        public decimal SubTotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public decimal Total { get; set; }
    }
}