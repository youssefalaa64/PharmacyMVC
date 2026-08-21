using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Services;
using Pharmacy.Utils;

namespace Pharmacy.Areas.Admin.Controllers
{
    [Area(CD.ADMIN_AREA)]
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


        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId =
                _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }


            var notifications =
                await _notificationService
                    .GetUserNotificationsAsync(userId);


            return Json(notifications.Select(n => new
            {
                n.Id,
                n.Message,
                n.Type,
                n.OrderId,
                n.IsRead,
                n.CreatedAt
            }));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(
            int id)
        {
            var userId =
                _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }


            await _notificationService
                .MarkAsReadAsync(id, userId);


            return Ok();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId =
                _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }


            await _notificationService
                .MarkAllAsReadAsync(userId);


            return Ok();
        }
    }
}
