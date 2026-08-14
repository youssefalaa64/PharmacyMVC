using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NuGet.Configuration;
using Pharmacy.Models;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Identity.Controllers
{
    [Authorize]
    [Area(CD.IDENTITY_AREA)]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _usermanager;

        public ProfileController(UserManager<ApplicationUser> usermanager)
        {
            _usermanager = usermanager;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _usermanager.GetUserAsync(User);
            var userVM = new ApplicationUserVM
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Adresse = user.Address,
                Email = user.Email


            };
            return View(userVM);


        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ApplicationUserVM applicationUserVM)
        {
            var user = await _usermanager.GetUserAsync(User);
            user.FirstName = applicationUserVM.FirstName;
            user.LastName = applicationUserVM.LastName;
            user.PhoneNumber = applicationUserVM.PhoneNumber;
            user.Address = applicationUserVM.Adresse;
            user.Email = applicationUserVM.Email;
            var result = await _usermanager.UpdateAsync(user);
            
            if (!result.Succeeded)
            {
                TempData["failed"] = "Update failed";
            }
            else
            {
                TempData["succeeded"] = "Updated user succefully";
            }

            return RedirectToAction(nameof(Index));
        }
//f
        [HttpPost]
        public async Task<IActionResult> UpdatePAssword(UpdatePasswordVM updatePasswordVM)
        {
            var user = await _usermanager.GetUserAsync(User);
           var result = await _usermanager.ChangePasswordAsync(user, updatePasswordVM.CurrentPassword, updatePasswordVM.NewPassword);
            if (!ModelState.IsValid)
            {
                TempData["failed"] = "Please check your input fields.";
                return RedirectToAction(nameof(Index));
            }
            if (!result.Succeeded)
            {
                TempData["failed"] = string.Join(", ", result.Errors.Select(e => e.Description)); 
            }
            else
            {
                TempData["succeeded"] = "Password updated succefully";
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
