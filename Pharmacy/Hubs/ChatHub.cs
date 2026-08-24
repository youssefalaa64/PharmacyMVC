using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Pharmacy.Services;
using Pharmacy.Utils;

namespace Pharmacy.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly INotificationService _notificationService;
        UserManager<ApplicationUser> _userManager;

        public ChatHub(IChatService chatService, INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _chatService = chatService;
            _notificationService = notificationService;
            this._userManager = userManager;
        }

        public async Task JoinChat(int chatId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException(
                    "User is not authenticated."
                );
            }

            var isAdmin =
                Context.User?.IsInRole(
                    CD.SUPER_ADMIN_ROLE
                ) == true;

            Chat? chat;

            if (isAdmin)
            {
                chat = await _chatService.GetAdminChatAsync(
                    chatId,
                    userId
                );
            }
            else
            {
                chat = await _chatService.GetChatAsync(
                    chatId,
                    userId
                );
            }

            if (chat == null)
            {
                throw new HubException(
                    "You are not allowed to access this chat."
                );
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GetChatGroup(chatId)
            );
        }

        public async Task LeaveChat(int chatId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                GetChatGroup(chatId)
            );
        }

        public async Task SendMessage(
      int chatId,
      string message)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException(
                    "User is not authenticated."
                );
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var chatMessage =
                await _chatService.SendMessageAsync(
                    chatId,
                    userId,
                    message
                );

            if (chatMessage == null)
            {
                throw new HubException(
                    "Unable to send message."
                );
            }

            // Get the chat
            var chat = await _chatService.GetChatAsync(
                chatId,
                userId
            );

            if (chat == null)
            {
                throw new HubException(
                    "Chat not found."
                );
            }

            // Send message to everyone inside the chat
            await Clients
                .Group(GetChatGroup(chatId))
                .SendAsync(
                    "ReceiveMessage",
                    new
                    {
                        id = chatMessage.Id,
                        chatId = chatMessage.ChatId,
                        senderId = chatMessage.SenderId,
                        message = chatMessage.Message,
                        sentAt = chatMessage.SentAt,
                        isRead = chatMessage.IsRead
                    }
                );


            // ==========================================
            // Send Notification
            // ==========================================

            if (chat.CustomerId == userId)
            {
                // Customer sent message

                if (!string.IsNullOrEmpty(chat.AdminId))
                {
                    // Chat already assigned
                    await _notificationService.CreateAsync(
                        chat.AdminId,
                        "You have a new chat message.",
                        "Chat",
                        chatId
                    );
                }
                else
                {
                    // Chat is not assigned
                    // Notify all Super Admins

                    var admins =
                        await _userManager.GetUsersInRoleAsync(
                            CD.SUPER_ADMIN_ROLE
                        );

                    foreach (var admin in admins)
                    {
                        await _notificationService.CreateAsync(
                            admin.Id,
                            "A customer sent a new chat message.",
                            "Chat",
                            chatId
                        );
                    }
                }
            }
            else if (chat.AdminId == userId)
            {
                // Admin sent message

                await _notificationService.CreateAsync(
                    chat.CustomerId,
                    "You have a new message from support.",
                    "Chat",
                    chatId
                );
            }
        }
        

        public async Task MarkAsRead(int chatId)
        {
            var userId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException(
                    "User is not authenticated."
                );
            }

            await _chatService.MarkMessagesAsReadAsync(
                chatId,
                userId
            );

            await Clients
                .Group(GetChatGroup(chatId))
                .SendAsync(
                    "MessagesRead",
                    new
                    {
                        chatId,
                        userId
                    }
                );
        }

        private static string GetChatGroup(int chatId)
        {
            return $"chat-{chatId}";
        }
    }
}