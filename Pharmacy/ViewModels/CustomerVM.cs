using System.ComponentModel.DataAnnotations;

namespace Pharmacy.ViewModels
{
    public class CustomerVM
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }
        public decimal CurrentBalance { get; set; }
    }
}
