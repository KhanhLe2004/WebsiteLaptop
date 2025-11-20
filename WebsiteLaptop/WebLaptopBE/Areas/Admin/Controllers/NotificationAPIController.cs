using Microsoft.AspNetCore.Mvc;
using WebLaptopBE.Services;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/notifications")]
    [ApiController]
    public class NotificationAPIController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationAPIController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET: api/admin/notifications
        // Lấy danh sách thông báo
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetNotifications([FromQuery] bool unreadOnly = false)
        {
            try
            {
                var notifications = _notificationService.GetAllNotifications(unreadOnly);

                var result = notifications.Select(n => new
                {
                    notificationId = n.NotificationId,
                    saleInvoiceId = n.SaleInvoiceId,
                    stockExportId = n.StockExportId,
                    message = n.Message,
                    isRead = n.IsRead ?? false,
                    createdAt = n.CreatedAt,
                    type = n.Type
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách thông báo", error = ex.Message });
            }
        }

        // GET: api/admin/notifications/count
        // Đếm số thông báo chưa đọc
        [HttpGet("count")]
        public ActionResult<int> GetUnreadNotificationCount()
        {
            try
            {
                var count = _notificationService.GetUnreadCount();
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đếm thông báo", error = ex.Message });
            }
        }

        // PUT: api/admin/notifications/{id}/read
        // Đánh dấu thông báo đã đọc
        [HttpPut("{id}/read")]
        public IActionResult MarkAsRead(string id)
        {
            try
            {
                var success = _notificationService.MarkAsRead(id);
                
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy thông báo" });
                }

                return Ok(new { message = "Đã đánh dấu thông báo đã đọc" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật thông báo", error = ex.Message });
            }
        }

        // PUT: api/admin/notifications/read-all
        // Đánh dấu tất cả thông báo đã đọc
        [HttpPut("read-all")]
        public IActionResult MarkAllAsRead()
        {
            try
            {
                var count = _notificationService.MarkAllAsRead();
                return Ok(new { message = $"Đã đánh dấu {count} thông báo đã đọc" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật thông báo", error = ex.Message });
            }
        }
    }
}

