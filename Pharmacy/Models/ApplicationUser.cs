using Microsoft.AspNetCore.Identity;

namespace Pharmacy.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    = new List<Notification>();
        public ICollection<Chat> CustomerChats { get; set; }
    = new List<Chat>();

        public ICollection<Chat> AdminChats { get; set; }
            = new List<Chat>();

        public ICollection<ChatMessage> SentMessages { get; set; }
            = new List<ChatMessage>();
    }
}
