using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Services;
using Pharmacy.Utils;

namespace Pharmacy.Areas.Customer.Controllers
{
    [Area(CD.CUSTOMER_AREA)]
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // GET: Customer/Notification
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var notifications =
                await _notificationService
                    .GetUserNotificationsAsync(userId);

            return View(notifications);
        }

        // POST: Customer/Notification/MarkAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            await _notificationService.MarkAsReadAsync(
                id,
                userId
            );

            return RedirectToAction(nameof(Index));
        }

        // POST: Customer/Notification/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            await _notificationService
                .MarkAllAsReadAsync(userId);

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var notifications =
                await _notificationService
                    .GetUserNotificationsAsync(userId);

            var count =
                notifications.Count(n => !n.IsRead);

            return Json(count);
        }
    }
}
