using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmacy.Models
{
    public class Customer
    {
        public int Id { get; set; }
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; } = 0;
        public ICollection<SalesInvoice> SalesInvoices { get; set; }
           = new List<SalesInvoice>();
    }
}
