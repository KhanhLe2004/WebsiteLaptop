using WebLaptopBE.Models;
using System.Collections.Concurrent;

namespace WebLaptopBE.Services
{
    public class NotificationService
    {
        // Dùng ConcurrentDictionary để thread-safe
        private readonly ConcurrentDictionary<string, Notification> _notifications = new();
        private int _notificationCounter = 0;

        // Lấy tất cả thông báo
        public List<Notification> GetAllNotifications(bool unreadOnly = false)
        {
            var notifications = _notifications.Values.AsEnumerable();
            
            if (unreadOnly)
            {
                notifications = notifications.Where(n => n.IsRead == false || n.IsRead == null);
            }

            return notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToList();
        }

        // Đếm số thông báo chưa đọc
        public int GetUnreadCount()
        {
            return _notifications.Values
                .Count(n => n.IsRead == false || n.IsRead == null);
        }

        // Tạo thông báo mới
        public Notification CreateNotification(string saleInvoiceId, string stockExportId, string message, string type = "StockExportCompleted")
        {
            _notificationCounter++;
            string notificationId = $"NOT{_notificationCounter:D3}";

            var notification = new Notification
            {
                NotificationId = notificationId,
                SaleInvoiceId = saleInvoiceId,
                StockExportId = stockExportId,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _notifications.TryAdd(notificationId, notification);
            return notification;
        }

        // Đánh dấu thông báo đã đọc
        public bool MarkAsRead(string notificationId)
        {
            if (_notifications.TryGetValue(notificationId, out var notification))
            {
                notification.IsRead = true;
                return true;
            }
            return false;
        }

        // Đánh dấu tất cả đã đọc
        public int MarkAllAsRead()
        {
            int count = 0;
            foreach (var notification in _notifications.Values)
            {
                if (notification.IsRead == false || notification.IsRead == null)
                {
                    notification.IsRead = true;
                    count++;
                }
            }
            return count;
        }

        // Lấy thông báo theo ID
        public Notification? GetNotification(string notificationId)
        {
            _notifications.TryGetValue(notificationId, out var notification);
            return notification;
        }
    }
}

