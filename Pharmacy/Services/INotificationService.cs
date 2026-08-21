namespace Pharmacy.Services
{
    public interface INotificationService
    {
        Task CreateAsync(
            string userId,
            string message,
            string? type = null,
            int? orderId = null);
        Task<List<Notification>> GetUserNotificationsAsync(
            string userId);
        Task MarkAsReadAsync(
            int notificationId,
            string userId);
        Task MarkAllAsReadAsync(
            string userId);
    }
}
