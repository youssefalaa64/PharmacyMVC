namespace Pharmacy.ViewModels
{
    public class CartVM
    {
        public int CartId { get; set; }

        public List<CartItemVM> Items { get; set; } = new();

        public decimal TotalAmount =>
            Items.Sum(x => x.TotalPrice);
    }
}