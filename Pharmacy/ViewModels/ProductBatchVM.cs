using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Pharmacy.ViewModels
{
    public class ProductBatchVM
    {
        public int Id { get; set; }
        [Required]
        public int ProductId { get; set; }
        [Required]
        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;
        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }
        [Required]
        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }
        [Required]
        [Range(0, int.MaxValue)]
        public int QuantityOnHand { get; set; }
        public IEnumerable<SelectListItem> Products { get; set; }
            = new List<SelectListItem>();
    }
}
