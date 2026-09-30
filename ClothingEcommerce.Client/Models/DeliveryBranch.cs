using System.ComponentModel.DataAnnotations;

namespace WebApplication_ClothingEcommerce.Models
{
    public class DeliveryBranch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string CarrierCode { get; set; } = "VETExpress";

        [Required]
        [StringLength(100)]
        public string Province { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string BranchName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
