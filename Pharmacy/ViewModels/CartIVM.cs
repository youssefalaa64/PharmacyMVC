namespace Pharmacy.ViewModels
{
    public class CartIVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProductImg { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal SubTotal
        {
            get
            {
                return UnitPrice * Quantity;
            }
        }
    }
}