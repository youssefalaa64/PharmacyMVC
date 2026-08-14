using Microsoft.AspNetCore.Mvc.Rendering;
using Pharmacy.Enums;
using System.ComponentModel.DataAnnotations;

namespace Pharmacy.ViewModels
{
    public class OrderVM
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal TotalAmount { get; set; }
        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; }
        [Range(0, double.MaxValue)]
        public decimal DeliveryFees { get; set; }
        public decimal NetAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        [Required]
        [StringLength(300)]
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public IEnumerable<SelectListItem> Users { get; set; }
            = new List<SelectListItem>();
        public List<OrderItemVM> OrderItems { get; set; }
            = new List<OrderItemVM>();
    }
}
