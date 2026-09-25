using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication_ClothingEcommerce.Models
{
    public class DeliveryMethod
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string KhmerName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // StorePickup, VETExpress, CityDelivery, OtherExpress

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseFee { get; set; } = 2.00m;

        [StringLength(100)]
        public string EstimatedDeliveryTime { get; set; } = "1-2 days";

        public bool RequiresBranchSelection { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
