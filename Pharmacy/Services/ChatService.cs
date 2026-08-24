namespace Pharmacy.Services
{
    public class ChatService : IChatService
    {
        private readonly IRepository<Chat> _chatRepository;
        private readonly IRepository<ChatMessage> _messageRepository;

        public ChatService(
            IRepository<Chat> chatRepository,
            IRepository<ChatMessage> messageRepository)
        {
            _chatRepository = chatRepository;
            _messageRepository = messageRepository;
        }

        public async Task<Chat?> GetChatAsync(
            int chatId,
            string userId)
        {
            return await _chatRepository.GetOneAsync(
                filter: c =>
                    c.Id == chatId &&
                    (
                        c.CustomerId == userId ||
                        c.AdminId == userId
                    ),
               includes:
[
    c => c.Customer,
    c => c.Messages
],
                IsTracking: false
            );
        }

        public async Task<Chat?> GetCustomerChatAsync(
            string customerId)
        {
            return await _chatRepository.GetOneAsync(
                filter: c => c.CustomerId == customerId,
                includes:
                [
                    c => c.Messages
                ],
                IsTracking: false
            );
        }

        public async Task<List<Chat>> GetCustomerChatsAsync(
            string customerId)
        {
            var chats = await _chatRepository.GetAllAsync(
                filter: c => c.CustomerId == customerId,
                includes:
                [
                    c => c.Messages
                ],
                IsTracking: false
            );

            return chats
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
        }

        public async Task<List<Chat>> GetAdminChatsAsync(
            string adminId)
        {
            var chats = await _chatRepository.GetAllAsync(
                filter: c =>
                    c.AdminId == null ||
                    c.AdminId == adminId,
                includes:
                [
                    c => c.Customer,
            c => c.Messages
                ],
                IsTracking: false
            );

            return chats
       .OrderByDescending(c =>
           c.Messages
               .Select(m => (DateTime?)m.SentAt)
               .Max() ?? c.CreatedAt
       )
       .ToList();
        }
        public async Task<Chat?> GetAdminChatAsync(
    int chatId,
    string adminId)
        {
            return await _chatRepository.GetOneAsync(
                filter: c =>
                    c.Id == chatId &&
                    (
                        c.AdminId == null ||
                        c.AdminId == adminId
                    ),
                includes:
                [
                    c => c.Customer,
            c => c.Messages
                ],
                IsTracking: false
            );
        }

        public async Task<Chat> GetOrCreateCustomerChatAsync(
            string customerId)
        {
            var chat = await GetCustomerChatAsync(customerId);

            if (chat != null)
            {
                return chat;
            }

            return await CreateChatAsync(customerId);
        }

        public async Task<Chat> CreateChatAsync(
            string customerId,
            string? adminId = null)
        {
            var chat = new Chat
            {
                CustomerId = customerId,
                AdminId = adminId,
                CreatedAt = DateTime.Now
            };

            await _chatRepository.CreateAsync(chat);
            await _chatRepository.CommitAsync();

            return chat;
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(
            int chatId)
        {
            var messages = await _messageRepository.GetAllAsync(
                filter: m => m.ChatId == chatId,
                IsTracking: false
            );

            return messages
                .OrderBy(m => m.SentAt)
                .ToList();
        }

        public async Task<ChatMessage?> SendMessageAsync(
        int chatId,
        string senderId,
        string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var chat = await _chatRepository.GetOneAsync(
                filter: c =>
                    c.Id == chatId &&
                    (
                        c.CustomerId == senderId ||
                        c.AdminId == senderId ||
                        c.AdminId == null
                    )
            );

            if (chat == null)
            {
                return null;
            }

            // Assign the chat to the admin
            // when the admin sends the first message.
            if (chat.AdminId == null &&
                chat.CustomerId != senderId)
            {
                chat.AdminId = senderId;

                _chatRepository.Update(chat);
            }

            var chatMessage = new ChatMessage
            {
                ChatId = chatId,
                SenderId = senderId,
                Message = message.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            };

            await _messageRepository.CreateAsync(chatMessage);

            await _messageRepository.CommitAsync();

            return chatMessage;
        }
        public async Task MarkMessagesAsReadAsync(
            int chatId,
            string userId)
        {
            var chat = await _chatRepository.GetOneAsync(
                filter: c =>
                    c.Id == chatId &&
                    (
                        c.CustomerId == userId ||
                        c.AdminId == userId
                    )
            );

            if (chat == null)
            {
                return;
            }

            var messages = await _messageRepository.GetAllAsync(
                filter: m =>
                    m.ChatId == chatId &&
                    m.SenderId != userId &&
                    !m.IsRead
            );

            foreach (var message in messages)
            {
                message.IsRead = true;
                _messageRepository.Update(message);
            }

            await _messageRepository.CommitAsync();
        }
    }
}
