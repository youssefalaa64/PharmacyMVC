using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(200)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Type { get; set; }

        public int? OrderId { get; set; }

        public Order? Order { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
