namespace WebApplication_ClothingEcommerce.Models
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = "noreply@clothe.com";
        public string SenderName { get; set; } = "CLOTHÉ Clothing Store";
        public bool EnableSsl { get; set; } = true;
        public string AdminEmail { get; set; } = "admin@clothe.com";

        // Notification feature toggles
        public bool NotifyOnNewOrder { get; set; } = true;
        public bool NotifyOnPaymentSuccess { get; set; } = true;
        public bool NotifyOnLowStock { get; set; } = true;
        public bool NotifyOnShipmentUpdate { get; set; } = true;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) &&
                                    !string.IsNullOrWhiteSpace(Username) &&
                                    !string.IsNullOrWhiteSpace(Password);

        public string MaskedPassword => string.IsNullOrWhiteSpace(Password)
            ? "Not configured"
            : new string('•', Math.Min(Password.Length, 12));
    }
}
