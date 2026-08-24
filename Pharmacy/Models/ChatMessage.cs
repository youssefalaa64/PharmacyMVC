using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int ChatId { get; set; }
        public Chat? Chat { get; set; }

        // User who sent the message
        [Required]
        public string SenderId { get; set; } = string.Empty;

        public ApplicationUser? Sender { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}
