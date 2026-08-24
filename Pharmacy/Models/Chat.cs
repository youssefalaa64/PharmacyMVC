namespace Pharmacy.Models
{
    public class Chat
    {
        public int Id { get; set; }

        // Customer
        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser? Customer { get; set; }

        // Admin
        public string? AdminId { get; set; }
        public ApplicationUser? Admin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<ChatMessage> Messages { get; set; }
            = new List<ChatMessage>();
    }
}
