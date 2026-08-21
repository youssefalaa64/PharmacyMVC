using Microsoft.AspNetCore.SignalR;
using Pharmacy.Hubs;

namespace Pharmacy.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<Notification> _repository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            IRepository<Notification> repository,
            IHubContext<NotificationHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
        }

        public async Task CreateAsync(
            string userId,
            string message,
            string? type = null,
            int? orderId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                OrderId = orderId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            await _repository.CreateAsync(notification);
            await _repository.CommitAsync();

            await _hubContext.Clients
                .User(userId)
                .SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        notification.Id,
                        notification.Message,
                        notification.Type,
                        notification.OrderId,
                        notification.IsRead,
                        notification.CreatedAt
                    });
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(
            string userId)
        {
            var notifications = await _repository.GetAllAsync(
                filter: n => n.UserId == userId,
                IsTracking: false
            );

            return notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public async Task MarkAsReadAsync(
            int notificationId,
            string userId)
        {
            var notification = await _repository.GetOneAsync(
                filter: n =>
                    n.Id == notificationId &&
                    n.UserId == userId
            );

            if (notification == null)
            {
                return;
            }

            notification.IsRead = true;

            _repository.Update(notification);

            await _repository.CommitAsync();
        }

        public async Task MarkAllAsReadAsync(
            string userId)
        {
            var notifications = await _repository.GetAllAsync(
                filter: n =>
                    n.UserId == userId &&
                    !n.IsRead
            );

            foreach (var notification in notifications)
            {
                notification.IsRead = true;

                _repository.Update(notification);
            }

            await _repository.CommitAsync();
        }
    }
}
