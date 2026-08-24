using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Services;
using Pharmacy.Utils;

namespace Pharmacy.Areas.Customer.Controllers
{
    [Area(CD.CUSTOMER_AREA)]
    [Authorize(Roles =CD.CUSTOMER_ROLE)]
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

        // GET: Customer/Chat
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var chat =
                await _chatService
                    .GetOrCreateCustomerChatAsync(userId);

            var messages =
                await _chatService
                    .GetMessagesAsync(chat.Id);

            await _chatService
                .MarkMessagesAsReadAsync(chat.Id, userId);

            ViewBag.ChatId = chat.Id;

            return View(messages);
        }

        // POST: Customer/Chat/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            int chatId,
            string message)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return RedirectToAction(nameof(Index));
            }

            var chat =
                await _chatService
                    .GetChatAsync(chatId, userId);

            if (chat == null)
            {
                return Forbid();
            }

            await _chatService.SendMessageAsync(
                chatId,
                userId,
                message
            );

            return RedirectToAction(nameof(Index));
        }
    }
}