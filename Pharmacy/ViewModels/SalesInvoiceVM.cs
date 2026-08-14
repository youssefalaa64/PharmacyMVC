using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Pharmacy.ViewModels
{
    public class SalesInvoiceVM
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;
        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public int? CustomerId { get; set; }
        public int? OrderId { get; set; }
        public IEnumerable<SelectListItem> Customers { get; set; }
            = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Orders { get; set; }
            = new List<SelectListItem>();
        public List<SalesInvoiceItemVM> InvoiceItems { get; set; }
            = new List<SalesInvoiceItemVM>();
    }
}
