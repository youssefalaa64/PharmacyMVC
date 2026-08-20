using Microsoft.CodeAnalysis.CSharp.Syntax;
using Pharmacy.Models;

namespace Pharmacy.ViewModels
{
    public class ProductFilterVM
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public decimal? Minprice { get; set; }
        public decimal? Maxprice { get; set; }
        public string? Category { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
    }
}
