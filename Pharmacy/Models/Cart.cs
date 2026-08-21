namespace Pharmacy.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser? ApplicationUser { get; set; }
        public ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();
    }
}
