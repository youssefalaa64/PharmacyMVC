using Pharmacy.Enums;
using System.ComponentModel.DataAnnotations;

namespace Pharmacy.ViewModels
{
    public class CheckoutVM
    {
        [Required]
        [StringLength(300)]
        [Display(Name = "Delivery Address")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; } = 0;

        public decimal DeliveryFees { get; set; } = 0;

        public decimal TotalAmount { get; set; }

        public decimal NetAmount { get; set; }

        public List<CheckoutItemVM> Items { get; set; } = new();
    }
}
