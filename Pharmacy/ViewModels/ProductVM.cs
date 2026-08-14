using Microsoft.AspNetCore.Mvc.Rendering;

namespace Pharmacy.ViewModels
{
    public class ProductVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? GenericName { get; set; }
        public decimal Price { get; set; }
        public string? ProductImg { get; set; }

        public IFormFile? ImageFile { get; set; }
        
        public int MinStockLevel { get; set; } = 5;
        public bool RequiresPrescription { get; set; } = false;
        public int CategoryId { get; set; }
        public IEnumerable<SelectListItem>? Categories { get; set; }
    }
}