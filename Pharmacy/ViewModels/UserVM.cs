using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Pharmacy.ViewModels
{
    public class UserVM
    {
        public string? Id { get; set; }
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Phone]
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        [DataType(DataType.Password)]
        public string? Password { get; set; }
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string? ConfirmPassword { get; set; }
        public List<string> SelectedRoles { get; set; }
            = new List<string>();
        public IEnumerable<SelectListItem> Roles { get; set; }
            = new List<SelectListItem>();
        public bool IsLocked { get; set; }
    }
}
