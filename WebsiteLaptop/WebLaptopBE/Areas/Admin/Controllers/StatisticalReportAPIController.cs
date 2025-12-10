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
                // Tổng doanh thu - chỉ tính những đơn hàng hoàn thành
                var completedInvoices = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .Where(si => si.TotalAmount != null && si.Status == "Hoàn thành")
                    .ToListAsync();

                var totalRevenue = completedInvoices.Sum(si => si.TotalAmount ?? 0);

                // Tổng đơn hàng - tính tất cả trạng thái
                var allInvoices = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .Where(si => si.TotalAmount != null)
                    .ToListAsync();

                var totalOrders = allInvoices.Count;

                // Giá trị trung bình/đơn - tính theo đơn hoàn thành
                var averageOrderValue = completedInvoices.Count > 0 ? totalRevenue / completedInvoices.Count : 0;

                // Tính số sản phẩm trung bình mỗi đơn hàng - tính theo đơn hoàn thành (để nhất quán)
                var totalProductsCompleted = completedInvoices
                    .SelectMany(si => si.SaleInvoiceDetails ?? new List<WebLaptopBE.Models.SaleInvoiceDetail>())
                    .Sum(detail => detail.Quantity ?? 0);
                var averageProductsPerOrder = completedInvoices.Count > 0 ? (double)totalProductsCompleted / completedInvoices.Count : 0;

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
        // Lấy doanh thu theo tháng (chỉ tính đơn hàng hoàn thành)
        [HttpGet("revenue-by-month")]
        public async Task<IActionResult> GetRevenueByMonth([FromQuery] int year = 2025)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                var revenueByMonth = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= startDate && si.TimeCreate <= endDate && si.TotalAmount != null && si.Status == "Hoàn thành")
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
        // Lấy top 5 sản phẩm bán chạy (chỉ từ hóa đơn hoàn thành)
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
        // Lấy chi tiết sản phẩm bán chạy (chỉ từ hóa đơn hoàn thành)
        [HttpGet("product-sales-details")]
        public async Task<IActionResult> GetProductSalesDetails([FromQuery] int top = 5)
        {
            try
            {
                var productSales = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && d.SaleInvoice.Status == "Hoàn thành")
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

        // GET: api/admin/statistical-report/import-statistics
        // Lấy thống kê số lượng sản phẩm nhập
        [HttpGet("import-statistics")]
        public async Task<IActionResult> GetImportStatistics()
        {
            try
            {
                // Tổng số lượng sản phẩm đã nhập
                var totalImportedQuantity = await _context.StockImportDetails
                    .SumAsync(sid => sid.Quantity ?? 0);

                // Tổng số phiếu nhập
                var totalImportInvoices = await _context.StockImports.CountAsync();

                // Tổng giá trị nhập
                var totalImportValue = await _context.StockImports
                    .SumAsync(si => si.TotalAmount ?? 0);

                // Số lượng nhập trung bình mỗi phiếu
                var averageQuantityPerImport = totalImportInvoices > 0 
                    ? (double)totalImportedQuantity / totalImportInvoices 
                    : 0;

                var result = new
                {
                    totalImportedQuantity = totalImportedQuantity,
                    totalImportInvoices = totalImportInvoices,
                    totalImportValue = totalImportValue,
                    averageQuantityPerImport = Math.Round(averageQuantityPerImport, 2)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê nhập hàng", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/stock-statistics
        // Lấy thống kê số lượng tồn kho (tính theo cấu hình)
        [HttpGet("stock-statistics")]
        public async Task<IActionResult> GetStockStatistics()
        {
            try
            {
                // Tổng số lượng tồn kho từ tất cả các cấu hình
                var totalStockQuantity = await _context.ProductConfigurations
                    .SumAsync(pc => pc.Quantity ?? 0);

                // Tổng số cấu hình sản phẩm
                var totalConfigurations = await _context.ProductConfigurations.CountAsync();

                // Số cấu hình có tồn kho > 0
                var configurationsInStock = await _context.ProductConfigurations
                    .CountAsync(pc => pc.Quantity > 0);

                // Số cấu hình hết hàng
                var configurationsOutOfStock = await _context.ProductConfigurations
                    .CountAsync(pc => pc.Quantity == 0 || pc.Quantity == null);

                // Giá trị tồn kho (tính theo giá cấu hình)
                var totalStockValue = await _context.ProductConfigurations
                    .Where(pc => pc.Quantity > 0 && pc.Price != null)
                    .SumAsync(pc => (pc.Quantity ?? 0) * (pc.Price ?? 0));

                var result = new
                {
                    totalStockQuantity = totalStockQuantity,
                    totalConfigurations = totalConfigurations,
                    configurationsInStock = configurationsInStock,
                    configurationsOutOfStock = configurationsOutOfStock,
                    totalStockValue = totalStockValue
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê tồn kho", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/export-statistics
        // Lấy thống kê phiếu xuất
        [HttpGet("export-statistics")]
        public async Task<IActionResult> GetExportStatistics()
        {
            try
            {
                // Tổng số phiếu xuất
                var totalExportInvoices = await _context.StockExports.CountAsync();

                // Tổng số lượng sản phẩm đã xuất
                var totalExportedQuantity = await _context.StockExportDetails
                    .SumAsync(sed => sed.Quantity ?? 0);

                // Số phiếu xuất theo trạng thái
                var exportsByStatus = await _context.StockExports
                    .Where(se => !string.IsNullOrEmpty(se.Status))
                    .GroupBy(se => se.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                var result = new
                {
                    totalExportInvoices = totalExportInvoices,
                    totalExportedQuantity = totalExportedQuantity,
                    exportsByStatus = exportsByStatus
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê phiếu xuất", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/warranty-statistics
        // Lấy thống kê bảo hành và sửa chữa
        [HttpGet("warranty-statistics")]
        public async Task<IActionResult> GetWarrantyStatistics()
        {
            try
            {
                // Thống kê theo loại (Type)
                var warrantyByType = await _context.Warranties
                    .Where(w => !string.IsNullOrEmpty(w.Type))
                    .GroupBy(w => w.Type)
                    .Select(g => new
                    {
                        type = g.Key,
                        count = g.Count(),
                        totalAmount = g.Sum(w => w.TotalAmount ?? 0)
                    })
                    .ToListAsync();

                // Thống kê theo trạng thái (Status)
                var warrantyByStatus = await _context.Warranties
                    .Where(w => !string.IsNullOrEmpty(w.Status))
                    .GroupBy(w => w.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                // Tổng số lượng bảo hành
                var totalWarranties = await _context.Warranties.CountAsync();

                // Tổng chi phí bảo hành
                var totalWarrantyCost = await _context.Warranties
                    .SumAsync(w => w.TotalAmount ?? 0);

                var result = new
                {
                    warrantyByType = warrantyByType,
                    warrantyByStatus = warrantyByStatus,
                    totalWarranties = totalWarranties,
                    totalWarrantyCost = totalWarrantyCost
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê bảo hành", error = ex.Message });
            }
        }
    }
}
