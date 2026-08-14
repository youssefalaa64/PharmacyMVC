using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmacy.Models
{
    public class ProductBatch
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }
        public int QuantityOnHand { get; set; }
    }
}
