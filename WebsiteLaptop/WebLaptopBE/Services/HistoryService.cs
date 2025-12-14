using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;
using WebLaptopBE.Models;

namespace WebLaptopBE.Services
{
    public class HistoryService
    {
        private readonly WebLaptopTenTechContext _context;

        public HistoryService(WebLaptopTenTechContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Ghi log lịch sử hoạt động
        /// </summary>
        /// <param name="employeeId">Mã nhân viên thực hiện hành động</param>
        /// <param name="activityType">Chi tiết hoạt động (ví dụ: "Thêm sản phẩm: SP001 - Laptop Dell")</param>
        public async Task LogHistoryAsync(string? employeeId, string activityType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(activityType))
                {
                    return;
                }

                var historyId = await GenerateNextHistoryIdAsync();
                
                var history = new History
                {
                    HistoryId = historyId,
                    EmployeeId = employeeId,
                    ActivityType = activityType,
                    Time = DateTime.Now
                };

                _context.Histories.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Silently fail để không ảnh hưởng đến các thao tác chính
                // Có thể log vào file hoặc hệ thống logging khác nếu cần
            }
        }

        /// <summary>
        /// Tạo mã lịch sử mới (format: HS + số thứ tự)
        /// </summary>
        private async Task<string> GenerateNextHistoryIdAsync()
        {
            var lastHistory = await _context.Histories
                .OrderByDescending(h => h.HistoryId)
                .FirstOrDefaultAsync();

            if (lastHistory == null)
            {
                return "HS000001";
            }

            var lastId = lastHistory.HistoryId;
            if (lastId.StartsWith("HS") && lastId.Length >= 8)
            {
                if (int.TryParse(lastId.Substring(2), out int lastNumber))
                {
                    var nextNumber = lastNumber + 1;
                    return $"HS{nextNumber:D6}";
                }
            }

            // Fallback: tìm số lớn nhất
            var allHistories = await _context.Histories
                .Where(h => h.HistoryId.StartsWith("HS"))
                .Select(h => h.HistoryId)
                .ToListAsync();

            int maxNumber = 0;
            foreach (var id in allHistories)
            {
                if (id.Length >= 8 && int.TryParse(id.Substring(2), out int num))
                {
                    if (num > maxNumber) maxNumber = num;
                }
            }

            return $"HS{(maxNumber + 1):D6}";
        }
    }
}
