using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/statistical-report")]
    [ApiController]
    public class StatisticalReportAPIController : ControllerBase
    {
        private readonly Testlaptop36Context _context;

        public StatisticalReportAPIController(Testlaptop36Context context)
        {
            _context = context;
        }

        // GET: api/admin/statistical-report/overview
        // Lấy thống kê tổng quan: Tổng doanh thu, Tổng đơn hàng, Giá trị TB/đơn, SP TB/đơn hàng
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverviewStatistics()
        {
            try
            {
                var saleInvoices = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .Where(si => si.TotalAmount != null)
                    .ToListAsync();

                var totalRevenue = saleInvoices.Sum(si => si.TotalAmount ?? 0);
                var totalOrders = saleInvoices.Count;
                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                // Tính số sản phẩm trung bình mỗi đơn hàng
                var totalProducts = saleInvoices
                    .SelectMany(si => si.SaleInvoiceDetails ?? new List<WebLaptopBE.Models.SaleInvoiceDetail>())
                    .Sum(detail => detail.Quantity ?? 0);
                var averageProductsPerOrder = totalOrders > 0 ? (double)totalProducts / totalOrders : 0;

                var result = new
                {
                    totalRevenue = totalRevenue,
                    totalOrders = totalOrders,
                    averageOrderValue = Math.Round(averageOrderValue, 2),
                    averageProductsPerOrder = Math.Round(averageProductsPerOrder, 2)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê tổng quan", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/revenue-by-month
        // Lấy doanh thu theo tháng
        [HttpGet("revenue-by-month")]
        public async Task<IActionResult> GetRevenueByMonth([FromQuery] int year = 2025)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                var revenueByMonth = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= startDate && si.TimeCreate <= endDate && si.TotalAmount != null)
                    .GroupBy(si => new { si.TimeCreate.Value.Year, si.TimeCreate.Value.Month })
                    .Select(g => new
                    {
                        year = g.Key.Year,
                        month = g.Key.Month,
                        revenue = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .OrderBy(x => x.year)
                    .ThenBy(x => x.month)
                    .ToListAsync();

                // Tạo mảng đầy đủ 12 tháng
                var result = new List<object>();
                for (int month = 1; month <= 12; month++)
                {
                    var data = revenueByMonth.FirstOrDefault(r => r.month == month);
                    result.Add(new
                    {
                        month = month,
                        monthName = $"Tháng {month}",
                        revenue = data?.revenue ?? 0
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy doanh thu theo tháng", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/top-products
        // Lấy top 5 sản phẩm bán chạy
        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int top = 5)
        {
            try
            {
                var topProducts = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .GroupBy(d => new { d.ProductId, d.Product.ProductName })
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

                // Lấy thêm thông tin danh mục và thương hiệu
                var result = new List<object>();
                foreach (var product in topProducts)
                {
                    var productInfo = await _context.Products
                        .Include(p => p.Brand)
                        .FirstOrDefaultAsync(p => p.ProductId == product.productId);

                    // Lấy danh mục từ ProductModel hoặc tên sản phẩm (có thể tùy chỉnh theo cấu trúc DB)
                    var category = "Chưa phân loại";
                    if (productInfo != null && !string.IsNullOrEmpty(productInfo.ProductModel))
                    {
                        category = productInfo.ProductModel;
                    }

                    result.Add(new
                    {
                        productId = product.productId,
                        productName = product.productName ?? "Chưa có tên",
                        category = category,
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
                return StatusCode(500, new { message = "Lỗi khi lấy top sản phẩm bán chạy", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/orders-by-status
        // Lấy số lượng đơn hàng theo trạng thái
        [HttpGet("orders-by-status")]
        public async Task<IActionResult> GetOrdersByStatus()
        {
            try
            {
                var ordersByStatus = await _context.SaleInvoices
                    .Where(si => !string.IsNullOrEmpty(si.Status))
                    .GroupBy(si => si.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                var result = ordersByStatus.Select(x => new
                {
                    status = x.status,
                    count = x.count
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy đơn hàng theo trạng thái", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/user-growth
        // Lấy tăng trưởng người dùng 6 tháng gần đây
        [HttpGet("user-growth")]
        public async Task<IActionResult> GetUserGrowth()
        {
            try
            {
                var today = DateTime.Now;
                var sixMonthsAgo = today.AddMonths(-6);
                var startDate = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

                // Lấy tổng số người dùng hiện tại
                var totalUsers = await _context.Customers.CountAsync();

                // Lấy số người dùng mới theo tháng
                var newUsersByMonth = await _context.Customers
                    .Where(c => c.DateOfBirth != null) // Có thể sử dụng trường khác để xác định ngày đăng ký
                    .GroupBy(c => new { c.DateOfBirth.Value.Year, c.DateOfBirth.Value.Month })
                    .Select(g => new
                    {
                        year = g.Key.Year,
                        month = g.Key.Month,
                        count = g.Count()
                    })
                    .Where(x => new DateTime(x.year, x.month, 1) >= startDate)
                    .OrderBy(x => x.year)
                    .ThenBy(x => x.month)
                    .ToListAsync();

                // Tạo dữ liệu 6 tháng gần đây
                var result = new List<object>();
                var cumulativeUsers = totalUsers - newUsersByMonth.Sum(x => x.count);

                for (int i = 5; i >= 0; i--)
                {
                    var date = today.AddMonths(-i);
                    var monthData = newUsersByMonth.FirstOrDefault(x => x.year == date.Year && x.month == date.Month);
                    var newUsers = monthData?.count ?? 0;
                    cumulativeUsers += newUsers;

                    result.Add(new
                    {
                        year = date.Year,
                        month = date.Month,
                        label = $"{date.Month:D2}/{date.Year}",
                        newUsers = newUsers,
                        totalUsers = cumulativeUsers
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy tăng trưởng người dùng", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/product-sales-details
        // Lấy chi tiết sản phẩm bán chạy
        [HttpGet("product-sales-details")]
        public async Task<IActionResult> GetProductSalesDetails([FromQuery] int top = 5)
        {
            try
            {
                var productSales = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .GroupBy(d => new 
                    { 
                        d.ProductId, 
                        ProductName = d.Product != null ? d.Product.ProductName : "Chưa có tên"
                    })
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

                var result = new List<object>();
                int rank = 1;

                foreach (var product in productSales)
                {
                    var productInfo = await _context.Products
                        .Include(p => p.Brand)
                        .FirstOrDefaultAsync(p => p.ProductId == product.productId);

                    var category = "Chưa phân loại";
                    if (productInfo != null && !string.IsNullOrEmpty(productInfo.ProductModel))
                    {
                        category = productInfo.ProductModel;
                    }

                    result.Add(new
                    {
                        rank = rank++,
                        productId = product.productId,
                        productName = product.productName ?? "Chưa có tên",
                        category = category,
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
                return StatusCode(500, new { message = "Lỗi khi lấy chi tiết sản phẩm bán chạy", error = ex.Message });
            }
        }
    }
}
