using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    public class DashboardAPIController : ControllerBase
    {
        private readonly Testlaptop37Context _context;

        public DashboardAPIController(Testlaptop37Context context)
        {
            _context = context;
        }

        // GET: api/admin/dashboard/monthly-stats
        // Lấy thống kê tháng này
        [HttpGet("monthly-stats")]
        public async Task<IActionResult> GetMonthlyStats([FromQuery] int? year = null, [FromQuery] int? month = null)
        {
            try
            {
                var now = DateTime.Now;
                var currentYear = year ?? now.Year;
                var currentMonth = month ?? now.Month;
                
                var startDate = new DateTime(currentYear, currentMonth, 1);
                var endDate = startDate.AddMonths(1);

                // Doanh thu tháng này (chỉ đơn hoàn thành)
                var monthlyRevenue = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= startDate && 
                                 si.TimeCreate < endDate && 
                                 si.Status == "Hoàn thành" && 
                                 si.TotalAmount != null)
                    .SumAsync(si => si.TotalAmount ?? 0);

                // Số đơn hàng tháng này
                var monthlyOrders = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= startDate && si.TimeCreate < endDate)
                    .CountAsync();

                // Số khách hàng mới tháng này
                var monthlyNewCustomers = await _context.Customers
                    .Where(c => c.DateOfBirth != null && 
                                c.DateOfBirth.Value.Year == currentYear &&
                                c.DateOfBirth.Value.Month == currentMonth)
                    .CountAsync();

                // Tổng số sản phẩm
                var totalProducts = await _context.Products
                    .Where(p => p.Active == true)
                    .CountAsync();

                // Tổng số khách hàng
                var totalCustomers = await _context.Customers
                    .Where(c => c.Active == true)
                    .CountAsync();

                // So sánh với tháng trước
                var previousMonthStart = startDate.AddMonths(-1);
                var previousMonthEnd = startDate;
                
                var previousMonthRevenue = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= previousMonthStart && 
                                 si.TimeCreate < previousMonthEnd && 
                                 si.Status == "Hoàn thành" && 
                                 si.TotalAmount != null)
                    .SumAsync(si => si.TotalAmount ?? 0);

                var previousMonthOrders = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= previousMonthStart && si.TimeCreate < previousMonthEnd)
                    .CountAsync();

                var revenueChange = previousMonthRevenue > 0 
                    ? ((monthlyRevenue - previousMonthRevenue) / previousMonthRevenue * 100) 
                    : (monthlyRevenue > 0 ? 100 : 0);

                var ordersChange = previousMonthOrders > 0 
                    ? ((double)(monthlyOrders - previousMonthOrders) / previousMonthOrders * 100) 
                    : (monthlyOrders > 0 ? 100 : 0);

                var result = new
                {
                    monthlyRevenue = monthlyRevenue,
                    monthlyOrders = monthlyOrders,
                    monthlyNewCustomers = monthlyNewCustomers,
                    totalProducts = totalProducts,
                    totalCustomers = totalCustomers,
                    revenueChange = Math.Round(revenueChange, 1),
                    ordersChange = Math.Round(ordersChange, 1),
                    currentYear = currentYear,
                    currentMonth = currentMonth,
                    monthName = new DateTime(currentYear, currentMonth, 1).ToString("MM/yyyy")
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê tháng", error = ex.Message });
            }
        }

        // GET: api/admin/dashboard/recent-orders
        // Lấy các đơn hàng gần đây
        [HttpGet("recent-orders")]
        public async Task<IActionResult> GetRecentOrders([FromQuery] int limit = 10)
        {
            try
            {
                var recentOrders = await _context.SaleInvoices
                    .Include(si => si.Customer)
                    .Include(si => si.Employee)
                    .OrderByDescending(si => si.TimeCreate)
                    .Take(limit)
                    .Select(si => new
                    {
                        saleInvoiceId = si.SaleInvoiceId,
                        customerName = si.Customer != null ? si.Customer.CustomerName : "-",
                        employeeName = si.Employee != null ? si.Employee.EmployeeName : "-",
                        totalAmount = si.TotalAmount ?? 0,
                        status = si.Status ?? "-",
                        timeCreate = si.TimeCreate,
                        paymentMethod = si.PaymentMethod ?? "-"
                    })
                    .ToListAsync();

                return Ok(recentOrders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy đơn hàng gần đây", error = ex.Message });
            }
        }

        // GET: api/admin/dashboard/revenue-chart
        // Lấy dữ liệu biểu đồ doanh thu theo tháng (12 tháng gần đây)
        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] int months = 12)
        {
            try
            {
                var now = DateTime.Now;
                var endDate = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                var startDate = endDate.AddMonths(-months);

                var revenueByMonth = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= startDate && 
                                 si.TimeCreate < endDate && 
                                 si.Status == "Hoàn thành" && 
                                 si.TotalAmount != null)
                    .GroupBy(si => new { 
                        Year = si.TimeCreate.Value.Year, 
                        Month = si.TimeCreate.Value.Month
                    })
                    .Select(g => new
                    {
                        year = g.Key.Year,
                        month = g.Key.Month,
                        revenue = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .OrderBy(x => x.year)
                    .ThenBy(x => x.month)
                    .ToListAsync();

                // Tạo mảng đầy đủ các tháng
                var result = new List<object>();
                for (int i = months - 1; i >= 0; i--)
                {
                    var date = now.AddMonths(-i);
                    var monthData = revenueByMonth.FirstOrDefault(r => r.year == date.Year && r.month == date.Month);
                    result.Add(new
                    {
                        date = date.ToString("MM/yyyy"),
                        monthName = $"Tháng {date.Month}/{date.Year}",
                        shortMonthName = $"T{date.Month}",
                        revenue = monthData?.revenue ?? 0
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy dữ liệu biểu đồ doanh thu", error = ex.Message });
            }
        }

        // GET: api/admin/dashboard/top-products
        // Lấy top sản phẩm bán chạy
        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int top = 5)
        {
            try
            {
                var topProducts = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && d.SaleInvoice.Status == "Hoàn thành")
                    .GroupBy(d => new { d.ProductId, ProductName = d.Product != null ? d.Product.ProductName : "Chưa có tên" })
                    .Select(g => new
                    {
                        productId = g.Key.ProductId,
                        productName = g.Key.ProductName,
                        totalSold = g.Sum(d => d.Quantity ?? 0),
                        totalRevenue = g.Sum(d => (d.Quantity ?? 0) * (d.UnitPrice ?? 0))
                    })
                    .OrderByDescending(x => x.totalSold)
                    .Take(top)
                    .ToListAsync();

                // Lấy thêm thông tin sản phẩm
                var result = new List<object>();
                int rank = 1;
                foreach (var product in topProducts)
                {
                    var productInfo = await _context.Products
                        .Include(p => p.Brand)
                        .FirstOrDefaultAsync(p => p.ProductId == product.productId);

                    result.Add(new
                    {
                        rank = rank++,
                        productId = product.productId,
                        productName = product.productName ?? "Chưa có tên",
                        brandName = productInfo?.Brand?.BrandName ?? "Không xác định",
                        price = productInfo?.SellingPrice ?? productInfo?.OriginalSellingPrice ?? 0,
                        totalSold = product.totalSold,
                        totalRevenue = product.totalRevenue
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy top sản phẩm", error = ex.Message });
            }
        }
    }
}
