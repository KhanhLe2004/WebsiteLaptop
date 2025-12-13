using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/history")]
    [ApiController]
    public class HistoryAPIController : ControllerBase
    {
        private readonly Testlaptop38Context _context;

        public HistoryAPIController(Testlaptop38Context context)
        {
            _context = context;
        }

        // GET: api/admin/history
        // Lấy danh sách lịch sử hoạt động có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<HistoryDTO>>> GetHistory(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? employeeId = null,
            [FromQuery] DateTime? dateFrom = null, // Thêm filter ngày bắt đầu
            [FromQuery] DateTime? dateTo = null) // Thêm filter ngày kết thúc
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Histories
                    .Include(h => h.Employee)
                    .AsQueryable();

                // Tìm kiếm theo ActivityType hoặc EmployeeName
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(h =>
                        (h.ActivityType != null && h.ActivityType.ToLower().Contains(searchTerm)) ||
                        (h.Employee != null && h.Employee.EmployeeName != null && h.Employee.EmployeeName.ToLower().Contains(searchTerm)) ||
                        (h.EmployeeId != null && h.EmployeeId.ToLower().Contains(searchTerm)));
                }

                // Lọc theo nhân viên
                if (!string.IsNullOrWhiteSpace(employeeId))
                {
                    query = query.Where(h => h.EmployeeId == employeeId);
                }

                // Lọc theo ngày
                if (dateFrom.HasValue)
                {
                    query = query.Where(h => h.Time != null && h.Time.Value.Date >= dateFrom.Value.Date);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(h => h.Time != null && h.Time.Value.Date <= dateTo.Value.Date);
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var histories = await query
                    .OrderByDescending(h => h.Time)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new HistoryDTO
                    {
                        HistoryId = h.HistoryId,
                        ActivityType = h.ActivityType,
                        EmployeeId = h.EmployeeId,
                        EmployeeName = h.Employee != null ? h.Employee.EmployeeName : null,
                        Time = h.Time
                    })
                    .ToListAsync();

                var result = new PagedResult<HistoryDTO>
                {
                    Items = histories,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách lịch sử", error = ex.Message });
            }
        }

        // GET: api/admin/history/{id}
        // Lấy chi tiết một lịch sử
        [HttpGet("{id}")]
        public async Task<ActionResult<HistoryDTO>> GetHistoryById(string id)
        {
            try
            {
                var history = await _context.Histories
                    .Include(h => h.Employee)
                    .FirstOrDefaultAsync(h => h.HistoryId == id);

                if (history == null)
                {
                    return NotFound(new { message = "Không tìm thấy lịch sử" });
                }

                var historyDTO = new HistoryDTO
                {
                    HistoryId = history.HistoryId,
                    ActivityType = history.ActivityType,
                    EmployeeId = history.EmployeeId,
                    EmployeeName = history.Employee != null ? history.Employee.EmployeeName : null,
                    Time = history.Time
                };

                return Ok(historyDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin lịch sử", error = ex.Message });
            }
        }
    }
}

