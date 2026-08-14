using Pharmacy.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmacy.Models
{
    public class Order
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryFees { get; set; } = 0;
        public decimal NetAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        [StringLength(300)]
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser? ApplicationUser { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();
    }
}
