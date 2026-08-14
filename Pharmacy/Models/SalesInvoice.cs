
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmacy.Models
{
    public class SalesInvoice
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
        public ICollection<SalesInvoiceItem> InvoiceItems { get; set; } = new List<SalesInvoiceItem>();
    }
}
