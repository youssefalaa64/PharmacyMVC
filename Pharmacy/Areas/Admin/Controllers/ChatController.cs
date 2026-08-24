using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Services;
using Pharmacy.Utils;

namespace Pharmacy.Areas.Admin.Controllers
{
    [Area(CD.ADMIN_AREA)]
    [Authorize(Roles = CD.SUPER_ADMIN_ROLE)]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(
            IChatService chatService,
            UserManager<ApplicationUser> userManager)
        {
            _chatService = chatService;
            _userManager = userManager;
        }


        // ==========================================
        // GET: Admin/Chat
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var adminId = _userManager.GetUserId(User);

            if (adminId == null)
            {
                return Challenge();
            }

            var chats = await _chatService.GetAdminChatsAsync(adminId);

            return View(chats);
        }


        // ==========================================
        // GET: Admin/Chat/Details/5
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var adminId = _userManager.GetUserId(User);

            if (adminId == null)
            {
                return Challenge();
            }
            var chat = await _chatService.GetAdminChatAsync(
                id,
                adminId
            );

            if (chat == null)
            {
                return NotFound();
            }

            var messages = await _chatService.GetMessagesAsync(id);

            await _chatService.MarkMessagesAsReadAsync(
                id,
                adminId
            );

            ViewBag.ChatId = id;
            ViewBag.CustomerId = chat.CustomerId;
            ViewBag.CustomerName =
                chat.Customer != null
                    ? $"{chat.Customer.FirstName} {chat.Customer.LastName}"
                    : "Customer";

            return View(messages);
        }


        // ==========================================
        // POST: Admin/Chat/SendMessage
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            int chatId,
            string message)
        {
            var adminId = _userManager.GetUserId(User);

            if (adminId == null)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id = chatId }
                );
            }

            var chat = await _chatService.GetChatAsync(
                chatId,
                adminId
            );

            if (chat == null)
            {
                return Forbid();
            }

            await _chatService.SendMessageAsync(
                chatId,
                adminId,
                message
            );

            return RedirectToAction(
                nameof(Details),
                new { id = chatId }
            );
        }
    }
}