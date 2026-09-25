using System.ComponentModel.DataAnnotations;

namespace WebApplication_ClothingEcommerce.Models
{
    public class TelegramSettings
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [StringLength(255)]
        public string BotToken { get; set; } = string.Empty;

        [StringLength(100)]
        public string ChatId { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public bool NotifyOnNewOrder { get; set; } = true;

        public bool NotifyOnPaymentReceived { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
