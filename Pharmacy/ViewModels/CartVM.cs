using System.Collections.Generic;

namespace Pharmacy.ViewModels
{
    public class CartVM
    {
        public List<CartIVM> Items { get; set; } = new List<CartIVM>();

        
        public decimal ShippingFee { get; set; } = 30.00m;

      
        public decimal SubTotal
        {
            get
            {
                decimal total = 0;

                foreach (var item in Items)
                {
                    total += item.SubTotal;
                }

                return total;
            }
        }

        
        public decimal GrandTotal
        {
            get
            {
                if (Items.Count > 0)
                {
                    return SubTotal + ShippingFee;
                }

                return 0;
            }
        }
    }
}