using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pharmacy.Models
{
    public class Product
    {
        public int Id { get; set; }
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;
        [StringLength(150)]
        public string? GenericName { get; set; }
        [Column(TypeName = "decimal(18,2)")]

        public decimal Price { get; set; }
        public string? ProductImg { get; set; }

        public int MinStockLevel { get; set; } = 5;
        public bool RequiresPrescription { get; set; } = false;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
    }
}
