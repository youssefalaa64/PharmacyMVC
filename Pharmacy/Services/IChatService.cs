namespace Pharmacy.Services
{
    public interface IChatService
    {
        Task<Chat?> GetChatAsync(
            int chatId,
            string userId);

        Task<Chat?> GetCustomerChatAsync(
            string customerId);

        Task<List<Chat>> GetCustomerChatsAsync(
            string customerId);

        Task<List<Chat>> GetAdminChatsAsync(
            string adminId);
        Task<Chat?> GetAdminChatAsync(
                int chatId,
                    string adminId);



        Task<Chat> CreateChatAsync(
            string customerId,
            string? adminId = null);

        Task<ChatMessage?> SendMessageAsync(
            int chatId,
            string senderId,
            string message);

        Task MarkMessagesAsReadAsync(
            int chatId,
            string userId);
        Task<Chat> GetOrCreateCustomerChatAsync(
                        string customerId);
        Task<List<ChatMessage>> GetMessagesAsync(
                        int chatId);
    }
}
