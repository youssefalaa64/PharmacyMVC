using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Pharmacy.Services;
using Pharmacy.Utils;

namespace Pharmacy.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
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