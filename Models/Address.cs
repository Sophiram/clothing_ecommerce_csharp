namespace WebApplication_ClothingEcommerce.Models
{
    public class Address
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public string Province { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public Customer Customer { get; set; } = null!;
    }
}
