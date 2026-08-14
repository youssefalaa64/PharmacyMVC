using System.ComponentModel.DataAnnotations;

namespace Pharmacy.ViewModels
{
    public class CategoryVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
       
        public string Name { get; set; } = string.Empty;
    }
}
