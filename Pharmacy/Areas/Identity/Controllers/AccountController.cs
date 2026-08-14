using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Identity.Controllers
{
    [Area(CD.c)]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IRepository<ApplicationUserOtp> _applicationUserOtpRepository;

        private readonly IEmailSender _emailSender;


        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (!ModelState.IsValid)
            {
                return View(registerVM);
            }
            var user = new ApplicationUser()
            {
                FirstName = registerVM.FirstName,
                LastName = registerVM.LastName,
                Email = registerVM.Email,
                UserName = registerVM.Username
            };
            var result = await _userManager.CreateAsync(user, registerVM.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(registerVM);
            }
            await _userManager.AddToRoleAsync(user, CD.CUSTOMER_ROLE);
            // Send Email 

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = CD.c, userId = user.Id, token = token }, Request.Scheme);
            
            await _emailSender.SendEmailAsync(
                registerVM.Email,
                "Ecommerce confirm Email",
                $"<h1>Please click <a href={link}>here</a> to Confirm Your Mail</h1>"
                );
            TempData["Success_Notification"] = "send Email Successfully";
            return RedirectToAction(nameof(Login));
        }
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new { area = CD.CUSTOMER_AREA });
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
            {
                return View(loginVM);
            }
            var user = await _userManager.FindByEmailAsync(loginVM.UsernameOrEmail) ??
                        await _userManager.FindByNameAsync(loginVM.UsernameOrEmail);
            if (user is null)
            {
                ModelState.AddModelError("", "Invalid UserName or Password");
                return View(loginVM);
            }
            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "user is Locked Please try again Later");
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError("", "please Confirm Your Email First");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid UserName or Password");
                }
                return View(loginVM);
            }
            return RedirectToAction("Index", "Home", new { area = CD.CUSTOMER_AREA });
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                string errors = "";
                foreach (var error in result.Errors)
                {
                    errors += error.Description + "\n";
                }
                TempData["Error_Notification"] = errors;
                return RedirectToAction(nameof(Login), "Account", new { area = CD.c });
            }
            TempData["Success_Notification"] = "Email Confirmed Successfully";
            return RedirectToAction(nameof(Login), "Account", new { area = CD.c });
        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM resendEmailConfirmationVM)
        {
            var user = await _userManager.FindByEmailAsync(resendEmailConfirmationVM.UserNameOrEmail) ??
                       await _userManager.FindByNameAsync(resendEmailConfirmationVM.UserNameOrEmail);
            if (user is null)
            {
                ModelState.AddModelError("", "Invalid UserName or Password");
                return View(resendEmailConfirmationVM);
            }
            // Send Email 
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = CD.c, userId = user.Id, token = token }, Request.Scheme);
            await _emailSender.SendEmailAsync(
                user.Email,
                "Ecommerce confirm Email",
                $"<h1>Please click <a href={link}>here</a> to Confirm Your Mail</h1>"
                );

            return RedirectToAction(nameof(Login));
        }


        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM)
        {
            var user = await _userManager.FindByEmailAsync(forgetPasswordVM.UserNameOrEmail) ??
                await _userManager.FindByNameAsync(forgetPasswordVM.UserNameOrEmail);

            if (user is null)
            {
                ModelState.AddModelError("", "Invalid UserName or Email");
                return View(forgetPasswordVM);
            }

            var otp = new Random().Next(1000, 9999).ToString();

            var applicationUserOtp = new ApplicationUserOtp(user.Id, otp);

            await _applicationUserOtpRepository.CreateAsync(applicationUserOtp);
            await _applicationUserOtpRepository.CommitAsync();
            // send email 
            await _emailSender.SendEmailAsync(
               user.Email,
               "Ecommerce Reset Password",
               $"<h1>use this  <span style=\"color:red\">{otp}</span> as a otp to reset your password </h1>"
               );

            TempData["Success_Notification"] = "send Email Successfully";

            return RedirectToAction(nameof(VerifyOTP), new { userId = user.Id });
        }
        [HttpGet]
        public IActionResult VerifyOTP(string userId)
        {
            return View(new VerifyOTPVM() { UserId = userId });
        }
        [HttpPost]
        public async Task<IActionResult> VerifyOTP(VerifyOTPVM verifyOTPVM)
        {
            var user = await _userManager.FindByIdAsync(verifyOTPVM.UserId);
            if (user is null)
            {
                ModelState.AddModelError("", "invalid user");
                return View(verifyOTPVM);
            }
            var otps = await _applicationUserOtpRepository.GetAllAsync(e =>
                e.ApplicationUserId == user.Id &&
                e.IsValid &&
                e.ValidTo > DateTime.UtcNow
            );
            var otp = otps.OrderBy(e => e.CreatedAt).LastOrDefault();
            if (otp is null || otp.OTP != verifyOTPVM.OTP)
            {
                ModelState.AddModelError("", "invalid / Expired OTP");
                return View(verifyOTPVM);
            }
            otp.IsValid = false;
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _applicationUserOtpRepository.CommitAsync();


            return RedirectToAction(nameof(ResetPassword), new { userId = user.Id, token = token });
        }
        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            return View(new ResetPasswordVM() { UserId = userId, Token = token });
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM resetPasswordVM)
        {
            var user = await _userManager.FindByIdAsync(resetPasswordVM.UserId);
            if (user is null)
            {
                ModelState.AddModelError("", "invalid user");
                return View(resetPasswordVM);
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordVM.Token, resetPasswordVM.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(resetPasswordVM);
            }
            return RedirectToAction(nameof(Login));
        }
    }
}
