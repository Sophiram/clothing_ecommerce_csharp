using System.ComponentModel.DataAnnotations;

namespace WebApplication_ClothingEcommerce.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [StringLength(450)]
        public string? UserId { get; set; }

        [StringLength(256)]
        public string? UserEmail { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [StringLength(450)]
        public string? EntityId { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
