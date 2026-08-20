using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pharmacy.Models;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Admin.Controllers
{
    [Authorize]
    [Area(CD.ADMIN_AREA)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/User
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();

            var result = new List<UserVM>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserVM
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    SelectedRoles = roles.ToList(),

                    IsLocked =
                        user.LockoutEnd.HasValue &&
                        user.LockoutEnd.Value > DateTimeOffset.UtcNow
                });
            }

            return View(result);
        }

        // GET: Admin/User/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UserVM();

            await LoadRoles(model);

            return View(model);
        }

        // POST: Admin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadRoles(model);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password!
            );

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description
                    );
                }

                await LoadRoles(model);

                return View(model);
            }

            if (model.SelectedRoles.Any())
            {
                var roleResult =
                    await _userManager.AddToRolesAsync(
                        user,
                        model.SelectedRoles
                    );

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description
                        );
                    }

                    await LoadRoles(model);

                    return View(model);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/User/Edit/id
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserVM
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                SelectedRoles = roles.ToList(),

                IsLocked =
                    user.LockoutEnd.HasValue &&
                    user.LockoutEnd.Value > DateTimeOffset.UtcNow
            };

            await LoadRoles(model);

            return View(model);
        }

        // POST: Admin/User/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserVM model)
        {
            // Password is optional during Edit
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));

            if (!ModelState.IsValid)
            {
                await LoadRoles(model);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id!);

            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description
                    );
                }

                await LoadRoles(model);

                return View(model);
            }

            // Update roles
            var currentRoles =
                await _userManager.GetRolesAsync(user);

            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(
                    user,
                    currentRoles
                );
            }

            if (model.SelectedRoles.Any())
            {
                await _userManager.AddToRolesAsync(
                    user,
                    model.SelectedRoles
                );
            }

            // Optional password change
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token =
                    await _userManager.GeneratePasswordResetTokenAsync(user);

                var passwordResult =
                    await _userManager.ResetPasswordAsync(
                        user,
                        token,
                        model.Password
                    );

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description
                        );
                    }

                    await LoadRoles(model);

                    return View(model);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/User/Lock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEnabledAsync(user, true);

            await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.UtcNow.AddYears(100)
            );

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/User/Unlock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(
                user,
                null
            );

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRoles(UserVM model)
        {
            var roles = _roleManager.Roles.ToList();

            model.Roles = roles.Select(r => new SelectListItem
            {
                Value = r.Name!,
                Text = r.Name!,
                Selected = model.SelectedRoles.Contains(r.Name!)
            });
        }
    }
}