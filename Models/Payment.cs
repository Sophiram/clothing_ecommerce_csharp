using WebApplication_ClothingEcommerce.Data.Enums;

namespace WebApplication_ClothingEcommerce.Models
{
    public class Payment
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }
        public Guid PaymentMethodId { get; set; }


        public PaymentMethod PaymentMethod { get; set; } = null!;

        public PaymentStatus PaymentStatus { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaidAt { get; set; }

        public Order Order { get; set; } = null!;
    }
}
