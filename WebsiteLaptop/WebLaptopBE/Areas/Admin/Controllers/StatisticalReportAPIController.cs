using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/statistical-report")]
    [ApiController]
    public class StatisticalReportAPIController : ControllerBase
    {
        private readonly WebLaptopTenTechContext _context;

        public StatisticalReportAPIController(WebLaptopTenTechContext context)
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
                // Tổng doanh thu: 
                // - Nếu PaymentMethod = "Thanh toán khi nhận hàng" thì chỉ tính khi Status = "Hoàn thành"
                // - Nếu PaymentMethod = "Chuyển khoản ngân hàng" thì tính luôn không cần xét trạng thái
                var completedInvoices = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .Where(si => si.TotalAmount != null &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
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
                    .Where(si => si.TimeCreate >= startDate && 
                                 si.TimeCreate <= endDate && 
                                 si.TotalAmount != null &&
                                 ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                  (si.PaymentMethod == "Chuyển khoản ngân hàng")))
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
                    .Where(d => d.SaleInvoice != null && 
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
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
                    .Where(d => d.SaleInvoice != null && 
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
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

        // GET: api/admin/statistical-report/import-amount-by-month
        // Lấy số tiền nhập hàng theo tháng
        [HttpGet("import-amount-by-month")]
        public async Task<IActionResult> GetImportAmountByMonth([FromQuery] int year = 2025)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                var importAmountByMonth = await _context.StockImports
                    .Where(si => si.Time != null && si.Time >= startDate && si.Time <= endDate && si.TotalAmount != null)
                    .GroupBy(si => new { si.Time.Value.Year, si.Time.Value.Month })
                    .Select(g => new
                    {
                        year = g.Key.Year,
                        month = g.Key.Month,
                        amount = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .OrderBy(x => x.year)
                    .ThenBy(x => x.month)
                    .ToListAsync();

                // Tạo mảng đầy đủ 12 tháng
                var result = new List<object>();
                for (int month = 1; month <= 12; month++)
                {
                    var data = importAmountByMonth.FirstOrDefault(r => r.month == month);
                    result.Add(new
                    {
                        month = month,
                        monthName = $"Tháng {month}",
                        amount = data?.amount ?? 0
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy số tiền nhập hàng theo tháng", error = ex.Message });
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

        // GET: api/admin/statistical-report/exports-by-status
        // Lấy số lượng phiếu xuất theo trạng thái
        [HttpGet("exports-by-status")]
        public async Task<IActionResult> GetExportsByStatus()
        {
            try
            {
                var exportsByStatus = await _context.StockExports
                    .Where(se => !string.IsNullOrEmpty(se.Status))
                    .GroupBy(se => se.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                var result = exportsByStatus.Select(x => new
                {
                    status = x.status,
                    count = x.count
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy phiếu xuất theo trạng thái", error = ex.Message });
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

        // GET: api/admin/statistical-report/revenue-by-brand
        // Lấy doanh thu theo thương hiệu
        [HttpGet("revenue-by-brand")]
        public async Task<IActionResult> GetRevenueByBrand()
        {
            try
            {
                var revenueByBrand = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && 
                                d.Quantity != null && 
                                d.UnitPrice != null &&
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var grouped = revenueByBrand
                    .GroupBy(d => new 
                    { 
                        BrandId = d.Product?.Brand?.BrandId ?? "Không xác định",
                        BrandName = d.Product?.Brand?.BrandName ?? "Không xác định"
                    })
                    .Select(g => new
                    {
                        brandId = g.Key.BrandId,
                        brandName = g.Key.BrandName,
                        totalRevenue = g.Sum(d => (d.Quantity ?? 0) * (d.UnitPrice ?? 0)),
                        totalSold = g.Sum(d => d.Quantity ?? 0)
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                return Ok(grouped);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy doanh thu theo thương hiệu", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/top-customers
        // Lấy top khách hàng mua nhiều nhất
        [HttpGet("top-customers")]
        public async Task<IActionResult> GetTopCustomers([FromQuery] int top = 5)
        {
            try
            {
                var topCustomers = await _context.SaleInvoices
                    .Include(si => si.Customer)
                    .Where(si => si.CustomerId != null &&
                                si.TotalAmount != null &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .GroupBy(si => new 
                    { 
                        si.CustomerId,
                        CustomerName = si.Customer != null ? si.Customer.CustomerName : "Không xác định"
                    })
                    .Select(g => new
                    {
                        customerId = g.Key.CustomerId,
                        customerName = g.Key.CustomerName,
                        totalOrders = g.Count(),
                        totalSpent = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .OrderByDescending(x => x.totalSpent)
                    .Take(top)
                    .ToListAsync();

                return Ok(topCustomers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy top khách hàng", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/sales-by-employee
        // Lấy doanh số theo nhân viên
        [HttpGet("sales-by-employee")]
        public async Task<IActionResult> GetSalesByEmployee()
        {
            try
            {
                var invoices = await _context.SaleInvoices
                    .Include(si => si.Employee)
                    .Where(si => si.EmployeeId != null &&
                                si.TotalAmount != null &&
                                si.TotalAmount > 0 &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var salesByEmployee = invoices
                    .GroupBy(si => new 
                    { 
                        si.EmployeeId,
                        EmployeeName = si.Employee?.EmployeeName ?? "Không xác định"
                    })
                    .Select(g => new
                    {
                        employeeId = g.Key.EmployeeId,
                        employeeName = g.Key.EmployeeName,
                        totalOrders = g.Count(),
                        totalRevenue = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                return Ok(salesByEmployee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy doanh số theo nhân viên", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/revenue-by-payment-method
        // Lấy doanh thu theo phương thức thanh toán
        [HttpGet("revenue-by-payment-method")]
        public async Task<IActionResult> GetRevenueByPaymentMethod()
        {
            try
            {
                var invoices = await _context.SaleInvoices
                    .Where(si => !string.IsNullOrEmpty(si.PaymentMethod) &&
                                si.TotalAmount != null &&
                                si.TotalAmount > 0 &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var revenueByPayment = invoices
                    .GroupBy(si => si.PaymentMethod)
                    .Select(g => new
                    {
                        paymentMethod = g.Key,
                        totalRevenue = g.Sum(si => si.TotalAmount ?? 0),
                        orderCount = g.Count()
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                return Ok(revenueByPayment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy doanh thu theo phương thức thanh toán", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/import-vs-export
        // So sánh nhập và xuất hàng
        [HttpGet("import-vs-export")]
        public async Task<IActionResult> GetImportVsExport([FromQuery] int year = 2025)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                // Tổng tiền nhập theo tháng
                var importByMonth = await _context.StockImports
                    .Where(si => si.Time != null && si.Time >= startDate && si.Time <= endDate && si.TotalAmount != null)
                    .GroupBy(si => new { si.Time.Value.Year, si.Time.Value.Month })
                    .Select(g => new
                    {
                        year = g.Key.Year,
                        month = g.Key.Month,
                        amount = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .ToListAsync();

                // Tổng tiền xuất theo tháng (tính từ giá trị bán trong SaleInvoice)
                // StockExport liên kết với SaleInvoice, nên tính từ giá bán của đơn hàng
                // Nếu không có SaleInvoice, tính từ giá trị xuất hàng (có thể tính từ chi tiết)
                var exportByMonth = await _context.StockExports
                    .Include(se => se.SaleInvoice)
                    .Where(se => se.Time != null && 
                                 se.Time >= startDate && 
                                 se.Time <= endDate)
                    .GroupBy(se => new 
                    { 
                        se.Time.Value.Year, 
                        se.Time.Value.Month 
                    })
                    .Select(g => new
                    {
                        year = g.Key.Year,
                        month = g.Key.Month,
                        amount = g.Sum(se => se.SaleInvoice != null && se.SaleInvoice.TotalAmount != null 
                            ? se.SaleInvoice.TotalAmount.Value 
                            : 0)
                    })
                    .ToListAsync();

                // Tạo mảng đầy đủ 12 tháng
                var result = new List<object>();
                for (int month = 1; month <= 12; month++)
                {
                    var importData = importByMonth.FirstOrDefault(r => r.month == month);
                    var exportData = exportByMonth.FirstOrDefault(r => r.month == month);
                    result.Add(new
                    {
                        month = month,
                        monthName = $"Tháng {month}",
                        importAmount = importData?.amount ?? 0,
                        exportAmount = exportData?.amount ?? 0
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi so sánh nhập và xuất hàng", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/supplier-statistics
        // Thống kê theo nhà cung cấp
        [HttpGet("supplier-statistics")]
        public async Task<IActionResult> GetSupplierStatistics()
        {
            try
            {
                var supplierStats = await _context.StockImports
                    .Include(si => si.Supplier)
                    .Where(si => si.SupplierId != null)
                    .GroupBy(si => new 
                    { 
                        si.SupplierId,
                        SupplierName = si.Supplier != null ? si.Supplier.SupplierName : "Không xác định"
                    })
                    .Select(g => new
                    {
                        supplierId = g.Key.SupplierId,
                        supplierName = g.Key.SupplierName,
                        totalImports = g.Count(),
                        totalAmount = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .OrderByDescending(x => x.totalAmount)
                    .Take(5)
                    .ToListAsync();

                return Ok(supplierStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê nhà cung cấp", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/overview/export-excel
        // Xuất báo cáo tổng quan ra Excel
        [HttpGet("overview/export-excel")]
        public async Task<IActionResult> ExportOverviewToExcel()
        {
            try
            {
                var completedInvoices = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .Where(si => si.TotalAmount != null && si.Status == "Hoàn thành")
                    .ToListAsync();

                var totalRevenue = completedInvoices.Sum(si => si.TotalAmount ?? 0);
                var allInvoices = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .Where(si => si.TotalAmount != null)
                    .ToListAsync();
                var totalOrders = allInvoices.Count;
                var averageOrderValue = completedInvoices.Count > 0 ? totalRevenue / completedInvoices.Count : 0;
                var totalProductsCompleted = completedInvoices
                    .SelectMany(si => si.SaleInvoiceDetails ?? new List<WebLaptopBE.Models.SaleInvoiceDetail>())
                    .Sum(detail => detail.Quantity ?? 0);
                var averageProductsPerOrder = completedInvoices.Count > 0 ? (double)totalProductsCompleted / completedInvoices.Count : 0;

                // Thêm thông tin nhập hàng
                var totalImportedQuantity = await _context.StockImportDetails
                    .SumAsync(sid => sid.Quantity ?? 0);
                var totalImportInvoices = await _context.StockImports.CountAsync();
                var totalImportValue = await _context.StockImports
                    .SumAsync(si => si.TotalAmount ?? 0);

                // Thông tin tồn kho
                var totalStockQuantity = await _context.ProductConfigurations
                    .SumAsync(pc => pc.Quantity ?? 0);
                var totalConfigurations = await _context.ProductConfigurations.CountAsync();
                var configurationsInStock = await _context.ProductConfigurations
                    .CountAsync(pc => pc.Quantity > 0);
                var totalStockValue = await _context.ProductConfigurations
                    .Where(pc => pc.Quantity > 0 && pc.Price != null)
                    .SumAsync(pc => (pc.Quantity ?? 0) * (pc.Price ?? 0));

                // Thông tin xuất hàng
                var totalExportInvoices = await _context.StockExports.CountAsync();
                var totalExportedQuantity = await _context.StockExportDetails
                    .SumAsync(sed => sed.Quantity ?? 0);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Báo cáo tổng quan");

                // Header
                worksheet.Cells[1, 1].Value = "TenTech - BÁO CÁO THỐNG KÊ TỔNG QUAN";
                worksheet.Cells[1, 1, 1, 3].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "Ngày xuất báo cáo:";
                worksheet.Cells[row, 2].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row += 2;

                // Phần 1: Thống kê bán hàng
                worksheet.Cells[row, 1].Value = "1. THỐNG KÊ BÁN HÀNG";
                worksheet.Cells[row, 1, row, 3].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;
                worksheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));
                row++;

                worksheet.Cells[row, 1].Value = "Tổng doanh thu:";
                worksheet.Cells[row, 2].Value = totalRevenue;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                row++;

                worksheet.Cells[row, 1].Value = "Tổng đơn hàng:";
                worksheet.Cells[row, 2].Value = totalOrders;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                worksheet.Cells[row, 1].Value = "Giá trị trung bình/đơn:";
                worksheet.Cells[row, 2].Value = Math.Round(averageOrderValue, 2);
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                row++;

                worksheet.Cells[row, 1].Value = "Sản phẩm trung bình/đơn:";
                worksheet.Cells[row, 2].Value = Math.Round(averageProductsPerOrder, 2);
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row += 2;

                // Phần 2: Thống kê nhập hàng
                worksheet.Cells[row, 1].Value = "2. THỐNG KÊ NHẬP HÀNG";
                worksheet.Cells[row, 1, row, 3].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;
                worksheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));
                row++;

                worksheet.Cells[row, 1].Value = "Tổng số lượng nhập hàng:";
                worksheet.Cells[row, 2].Value = totalImportedQuantity;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                row++;

                worksheet.Cells[row, 1].Value = "Tổng số phiếu nhập:";
                worksheet.Cells[row, 2].Value = totalImportInvoices;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                worksheet.Cells[row, 1].Value = "Tổng giá trị nhập hàng:";
                worksheet.Cells[row, 2].Value = totalImportValue;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                row += 2;

                // Phần 3: Thống kê tồn kho
                worksheet.Cells[row, 1].Value = "3. THỐNG KÊ TỒN KHO";
                worksheet.Cells[row, 1, row, 3].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;
                worksheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));
                row++;

                worksheet.Cells[row, 1].Value = "Tổng số lượng tồn kho:";
                worksheet.Cells[row, 2].Value = totalStockQuantity;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                row++;

                worksheet.Cells[row, 1].Value = "Tổng số cấu hình sản phẩm:";
                worksheet.Cells[row, 2].Value = totalConfigurations;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                worksheet.Cells[row, 1].Value = "Số cấu hình còn hàng:";
                worksheet.Cells[row, 2].Value = configurationsInStock;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                worksheet.Cells[row, 1].Value = "Giá trị tồn kho:";
                worksheet.Cells[row, 2].Value = totalStockValue;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                row += 2;

                // Phần 4: Thống kê xuất hàng
                worksheet.Cells[row, 1].Value = "4. THỐNG KÊ XUẤT HÀNG";
                worksheet.Cells[row, 1, row, 3].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;
                worksheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));
                row++;

                worksheet.Cells[row, 1].Value = "Tổng số phiếu xuất:";
                worksheet.Cells[row, 2].Value = totalExportInvoices;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                worksheet.Cells[row, 1].Value = "Tổng số lượng đã xuất:";
                worksheet.Cells[row, 2].Value = totalExportedQuantity;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                row++;

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"BaoCaoTongQuan_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất báo cáo tổng quan ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/overview/export-pdf
        // Xuất báo cáo tổng quan ra PDF
        [HttpGet("overview/export-pdf")]
        public async Task<IActionResult> ExportOverviewToPdf()
        {
            try
            {
                var completedInvoices = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .Where(si => si.TotalAmount != null &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var totalRevenue = completedInvoices.Sum(si => si.TotalAmount ?? 0);
                var allInvoices = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .Where(si => si.TotalAmount != null)
                    .ToListAsync();
                var totalOrders = allInvoices.Count;
                var averageOrderValue = completedInvoices.Count > 0 ? totalRevenue / completedInvoices.Count : 0;
                var totalProductsCompleted = completedInvoices
                    .SelectMany(si => si.SaleInvoiceDetails ?? new List<WebLaptopBE.Models.SaleInvoiceDetail>())
                    .Sum(detail => detail.Quantity ?? 0);
                var averageProductsPerOrder = completedInvoices.Count > 0 ? (double)totalProductsCompleted / completedInvoices.Count : 0;

                // Thêm thông tin nhập hàng
                var totalImportedQuantity = await _context.StockImportDetails
                    .SumAsync(sid => sid.Quantity ?? 0);
                var totalImportInvoices = await _context.StockImports.CountAsync();
                var totalImportValue = await _context.StockImports
                    .SumAsync(si => si.TotalAmount ?? 0);

                // Thông tin tồn kho
                var totalStockQuantity = await _context.ProductConfigurations
                    .SumAsync(pc => pc.Quantity ?? 0);
                var totalConfigurations = await _context.ProductConfigurations.CountAsync();
                var configurationsInStock = await _context.ProductConfigurations
                    .CountAsync(pc => pc.Quantity > 0);
                var totalStockValue = await _context.ProductConfigurations
                    .Where(pc => pc.Quantity > 0 && pc.Price != null)
                    .SumAsync(pc => (pc.Quantity ?? 0) * (pc.Price ?? 0));

                // Thông tin xuất hàng
                var totalExportInvoices = await _context.StockExports.CountAsync();
                var totalExportedQuantity = await _context.StockExportDetails
                    .SumAsync(sed => sed.Quantity ?? 0);

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text("TenTech - BÁO CÁO THỐNG KÊ TỔNG QUAN")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Phần 1: Thống kê bán hàng
                                column.Item().Text("1. THỐNG KÊ BÁN HÀNG").Bold().FontSize(12);
                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng doanh thu:").Bold();
                                    row.RelativeItem(2).Text(FormatCurrency(totalRevenue)).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng đơn hàng:").Bold();
                                    row.RelativeItem(2).Text(totalOrders.ToString()).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Giá trị trung bình/đơn:").Bold();
                                    row.RelativeItem(2).Text(FormatCurrency((decimal)averageOrderValue)).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Sản phẩm trung bình/đơn:").Bold();
                                    row.RelativeItem(2).Text(Math.Round(averageProductsPerOrder, 2).ToString()).FontSize(12);
                                });
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Phần 2: Thống kê nhập hàng
                                column.Item().Text("2. THỐNG KÊ NHẬP HÀNG").Bold().FontSize(12);
                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng số lượng nhập hàng:").Bold();
                                    row.RelativeItem(2).Text(totalImportedQuantity.ToString("N0")).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng số phiếu nhập:").Bold();
                                    row.RelativeItem(2).Text(totalImportInvoices.ToString()).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng giá trị nhập hàng:").Bold();
                                    row.RelativeItem(2).Text(FormatCurrency(totalImportValue)).FontSize(12);
                                });
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Phần 3: Thống kê tồn kho
                                column.Item().Text("3. THỐNG KÊ TỒN KHO").Bold().FontSize(12);
                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng số lượng tồn kho:").Bold();
                                    row.RelativeItem(2).Text(totalStockQuantity.ToString("N0")).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng số cấu hình sản phẩm:").Bold();
                                    row.RelativeItem(2).Text(totalConfigurations.ToString()).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Số cấu hình còn hàng:").Bold();
                                    row.RelativeItem(2).Text(configurationsInStock.ToString()).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Giá trị tồn kho:").Bold();
                                    row.RelativeItem(2).Text(FormatCurrency(totalStockValue)).FontSize(12);
                                });
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Phần 4: Thống kê xuất hàng
                                column.Item().Text("4. THỐNG KÊ XUẤT HÀNG").Bold().FontSize(12);
                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng số phiếu xuất:").Bold();
                                    row.RelativeItem(2).Text(totalExportInvoices.ToString()).FontSize(12);
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Tổng số lượng đã xuất:").Bold();
                                    row.RelativeItem(2).Text(totalExportedQuantity.ToString("N0")).FontSize(12);
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"BaoCaoTongQuan_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất báo cáo tổng quan ra PDF", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/revenue-by-month/export-excel
        // Xuất doanh thu theo tháng ra Excel
        [HttpGet("revenue-by-month/export-excel")]
        public async Task<IActionResult> ExportRevenueByMonthToExcel([FromQuery] int year = 2025)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                var revenueByMonth = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= startDate && 
                                 si.TimeCreate <= endDate && 
                                 si.TotalAmount != null &&
                                 ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                  (si.PaymentMethod == "Chuyển khoản ngân hàng")))
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

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add($"Doanh thu {year}");

                // Header
                worksheet.Cells[1, 1].Value = $"TenTech - DOANH THU THEO THÁNG NĂM {year}";
                worksheet.Cells[1, 1, 1, 3].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "Tháng";
                worksheet.Cells[row, 2].Value = "Doanh thu";
                worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250));
                row++;

                decimal totalRevenue = 0;
                for (int month = 1; month <= 12; month++)
                {
                    var data = revenueByMonth.FirstOrDefault(r => r.month == month);
                    var revenue = data?.revenue ?? 0;
                    totalRevenue += revenue;

                    worksheet.Cells[row, 1].Value = $"Tháng {month}";
                    worksheet.Cells[row, 2].Value = revenue;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                    row++;
                }

                worksheet.Cells[row, 1].Value = "TỔNG CỘNG";
                worksheet.Cells[row, 2].Value = totalRevenue;
                worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"DoanhThuTheoThang_{year}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất doanh thu theo tháng ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/revenue-by-month/export-pdf
        // Xuất doanh thu theo tháng ra PDF
        [HttpGet("revenue-by-month/export-pdf")]
        public async Task<IActionResult> ExportRevenueByMonthToPdf([FromQuery] int year = 2025)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                var revenueByMonth = await _context.SaleInvoices
                    .Where(si => si.TimeCreate >= startDate && 
                                 si.TimeCreate <= endDate && 
                                 si.TotalAmount != null &&
                                 ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                  (si.PaymentMethod == "Chuyển khoản ngân hàng")))
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

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text($"TenTech - DOANH THU THEO THÁNG NĂM {year}")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("Tháng").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Doanh thu").Bold();
                                    });

                                    decimal totalRevenue = 0;
                                    for (int month = 1; month <= 12; month++)
                                    {
                                        var data = revenueByMonth.FirstOrDefault(r => r.month == month);
                                        var revenue = data?.revenue ?? 0;
                                        totalRevenue += revenue;

                                        table.Cell().Element(CellStyle).Text($"Tháng {month}");
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(revenue));
                                    }

                                    table.Cell().Element(CellStyleHeader).Text("TỔNG CỘNG").Bold();
                                    table.Cell().Element(CellStyleHeader).AlignRight().Text(FormatCurrency(totalRevenue)).Bold();
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"DoanhThuTheoThang_{year}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất doanh thu theo tháng ra PDF", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/top-products/export-excel
        // Xuất top sản phẩm bán chạy ra Excel
        [HttpGet("top-products/export-excel")]
        public async Task<IActionResult> ExportTopProductsToExcel([FromQuery] int top = 5)
        {
            try
            {
                var topProducts = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && 
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
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

                var result = new List<object>();
                foreach (var product in topProducts)
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
                        productId = product.productId,
                        productName = product.productName ?? "Chưa có tên",
                        category = category,
                        brandName = productInfo?.Brand?.BrandName ?? "Không xác định",
                        price = productInfo?.SellingPrice ?? productInfo?.OriginalSellingPrice ?? 0,
                        totalSold = product.totalSold,
                        totalRevenue = product.totalRevenue
                    });
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add($"Top {top} sản phẩm");

                // Header
                worksheet.Cells[1, 1].Value = $"TenTech - TOP {top} SẢN PHẨM BÁN CHẠY";
                worksheet.Cells[1, 1, 1, 7].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "STT";
                worksheet.Cells[row, 2].Value = "Mã SP";
                worksheet.Cells[row, 3].Value = "Tên sản phẩm";
                worksheet.Cells[row, 4].Value = "Thương hiệu";
                worksheet.Cells[row, 5].Value = "Danh mục";
                worksheet.Cells[row, 6].Value = "Số lượng bán";
                worksheet.Cells[row, 7].Value = "Doanh thu";
                worksheet.Cells[row, 1, row, 7].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250));
                row++;

                int stt = 1;
                foreach (var item in result)
                {
                    var product = (dynamic)item;
                    worksheet.Cells[row, 1].Value = stt;
                    worksheet.Cells[row, 2].Value = product.productId;
                    worksheet.Cells[row, 3].Value = product.productName;
                    worksheet.Cells[row, 4].Value = product.brandName;
                    worksheet.Cells[row, 5].Value = product.category;
                    worksheet.Cells[row, 6].Value = product.totalSold;
                    worksheet.Cells[row, 7].Value = product.totalRevenue;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                    row++;
                    stt++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"TopSanPhamBanChay_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất top sản phẩm ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/top-products/export-pdf
        // Xuất top sản phẩm bán chạy ra PDF
        [HttpGet("top-products/export-pdf")]
        public async Task<IActionResult> ExportTopProductsToPdf([FromQuery] int top = 5)
        {
            try
            {
                var topProducts = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && 
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
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

                var result = new List<object>();
                foreach (var product in topProducts)
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
                        productId = product.productId,
                        productName = product.productName ?? "Chưa có tên",
                        category = category,
                        brandName = productInfo?.Brand?.BrandName ?? "Không xác định",
                        price = productInfo?.SellingPrice ?? productInfo?.OriginalSellingPrice ?? 0,
                        totalSold = product.totalSold,
                        totalRevenue = product.totalRevenue
                    });
                }

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text($"TenTech - TOP {top} SẢN PHẨM BÁN CHẠY")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.5f);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("STT").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Mã SP").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Tên sản phẩm").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Thương hiệu").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignCenter().Text("SL bán").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Doanh thu").Bold();
                                    });

                                    int stt = 1;
                                    foreach (var item in result)
                                    {
                                        var productId = item.GetType().GetProperty("productId")?.GetValue(item)?.ToString() ?? "";
                                        var productName = item.GetType().GetProperty("productName")?.GetValue(item)?.ToString() ?? "";
                                        var brandName = item.GetType().GetProperty("brandName")?.GetValue(item)?.ToString() ?? "";
                                        var totalSoldObj = item.GetType().GetProperty("totalSold")?.GetValue(item);
                                        var totalSold = totalSoldObj != null ? totalSoldObj.ToString() ?? "0" : "0";
                                        var totalRevenueObj = item.GetType().GetProperty("totalRevenue")?.GetValue(item);
                                        var totalRevenue = totalRevenueObj != null ? (decimal)totalRevenueObj : 0m;
                                        
                                        table.Cell().Element(CellStyle).Text(stt.ToString());
                                        table.Cell().Element(CellStyle).Text(productId);
                                        table.Cell().Element(CellStyle).Text(productName);
                                        table.Cell().Element(CellStyle).Text(brandName);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(totalSold);
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(totalRevenue));
                                        stt++;
                                    }
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"TopSanPhamBanChay_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất top sản phẩm ra PDF", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/top-customers/export-excel
        // Xuất top khách hàng ra Excel
        [HttpGet("top-customers/export-excel")]
        public async Task<IActionResult> ExportTopCustomersToExcel([FromQuery] int top = 5)
        {
            try
            {
                var topCustomers = await _context.SaleInvoices
                    .Include(si => si.Customer)
                    .Where(si => si.CustomerId != null &&
                                si.TotalAmount != null &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .GroupBy(si => new 
                    { 
                        si.CustomerId,
                        CustomerName = si.Customer != null ? si.Customer.CustomerName : "Không xác định"
                    })
                    .Select(g => new
                    {
                        customerId = g.Key.CustomerId,
                        customerName = g.Key.CustomerName,
                        totalOrders = g.Count(),
                        totalSpent = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .OrderByDescending(x => x.totalSpent)
                    .Take(top)
                    .ToListAsync();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add($"Top {top} khách hàng");

                worksheet.Cells[1, 1].Value = $"TenTech - TOP {top} KHÁCH HÀNG";
                worksheet.Cells[1, 1, 1, 4].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "STT";
                worksheet.Cells[row, 2].Value = "Mã KH";
                worksheet.Cells[row, 3].Value = "Tên khách hàng";
                worksheet.Cells[row, 4].Value = "Số đơn";
                worksheet.Cells[row, 5].Value = "Tổng chi tiêu";
                worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250));
                row++;

                int stt = 1;
                foreach (var customer in topCustomers)
                {
                    worksheet.Cells[row, 1].Value = stt;
                    worksheet.Cells[row, 2].Value = customer.customerId;
                    worksheet.Cells[row, 3].Value = customer.customerName;
                    worksheet.Cells[row, 4].Value = customer.totalOrders;
                    worksheet.Cells[row, 5].Value = customer.totalSpent;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                    row++;
                    stt++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"TopKhachHang_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất top khách hàng ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/top-customers/export-pdf
        // Xuất top khách hàng ra PDF
        [HttpGet("top-customers/export-pdf")]
        public async Task<IActionResult> ExportTopCustomersToPdf([FromQuery] int top = 5)
        {
            try
            {
                var topCustomers = await _context.SaleInvoices
                    .Include(si => si.Customer)
                    .Where(si => si.CustomerId != null &&
                                si.TotalAmount != null &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .GroupBy(si => new 
                    { 
                        si.CustomerId,
                        CustomerName = si.Customer != null ? si.Customer.CustomerName : "Không xác định"
                    })
                    .Select(g => new
                    {
                        customerId = g.Key.CustomerId,
                        customerName = g.Key.CustomerName,
                        totalOrders = g.Count(),
                        totalSpent = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .OrderByDescending(x => x.totalSpent)
                    .Take(top)
                    .ToListAsync();

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text($"TenTech - TOP {top} KHÁCH HÀNG")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.5f);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("STT").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Mã KH").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Tên khách hàng").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignCenter().Text("Số đơn").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Tổng chi tiêu").Bold();
                                    });

                                    int stt = 1;
                                    foreach (var customer in topCustomers)
                                    {
                                        table.Cell().Element(CellStyle).Text(stt.ToString());
                                        table.Cell().Element(CellStyle).Text(customer.customerId);
                                        table.Cell().Element(CellStyle).Text(customer.customerName);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(customer.totalOrders.ToString());
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(customer.totalSpent));
                                        stt++;
                                    }
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"TopKhachHang_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất top khách hàng ra PDF", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/sales-by-employee/export-excel
        // Xuất doanh số theo nhân viên ra Excel
        [HttpGet("sales-by-employee/export-excel")]
        public async Task<IActionResult> ExportSalesByEmployeeToExcel()
        {
            try
            {
                var invoices = await _context.SaleInvoices
                    .Include(si => si.Employee)
                    .Where(si => si.EmployeeId != null &&
                                si.TotalAmount != null &&
                                si.TotalAmount > 0 &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var salesByEmployee = invoices
                    .GroupBy(si => new 
                    { 
                        si.EmployeeId,
                        EmployeeName = si.Employee?.EmployeeName ?? "Không xác định"
                    })
                    .Select(g => new
                    {
                        employeeId = g.Key.EmployeeId,
                        employeeName = g.Key.EmployeeName,
                        totalOrders = g.Count(),
                        totalRevenue = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Doanh số nhân viên");

                worksheet.Cells[1, 1].Value = "TenTech - DOANH SỐ THEO NHÂN VIÊN";
                worksheet.Cells[1, 1, 1, 4].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "STT";
                worksheet.Cells[row, 2].Value = "Mã NV";
                worksheet.Cells[row, 3].Value = "Tên nhân viên";
                worksheet.Cells[row, 4].Value = "Số đơn";
                worksheet.Cells[row, 5].Value = "Doanh số";
                worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250));
                row++;

                int stt = 1;
                foreach (var emp in salesByEmployee)
                {
                    worksheet.Cells[row, 1].Value = stt;
                    worksheet.Cells[row, 2].Value = emp.employeeId;
                    worksheet.Cells[row, 3].Value = emp.employeeName;
                    worksheet.Cells[row, 4].Value = emp.totalOrders;
                    worksheet.Cells[row, 5].Value = emp.totalRevenue;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                    row++;
                    stt++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"DoanhSoNhanVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất doanh số nhân viên ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/sales-by-employee/export-pdf
        // Xuất doanh số theo nhân viên ra PDF
        [HttpGet("sales-by-employee/export-pdf")]
        public async Task<IActionResult> ExportSalesByEmployeeToPdf()
        {
            try
            {
                var invoices = await _context.SaleInvoices
                    .Include(si => si.Employee)
                    .Where(si => si.EmployeeId != null &&
                                si.TotalAmount != null &&
                                si.TotalAmount > 0 &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var salesByEmployee = invoices
                    .GroupBy(si => new 
                    { 
                        si.EmployeeId,
                        EmployeeName = si.Employee?.EmployeeName ?? "Không xác định"
                    })
                    .Select(g => new
                    {
                        employeeId = g.Key.EmployeeId,
                        employeeName = g.Key.EmployeeName,
                        totalOrders = g.Count(),
                        totalRevenue = g.Sum(si => si.TotalAmount ?? 0)
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text("TenTech - DOANH SỐ THEO NHÂN VIÊN")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.5f);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("STT").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Mã NV").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Tên nhân viên").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignCenter().Text("Số đơn").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Doanh số").Bold();
                                    });

                                    int stt = 1;
                                    foreach (var emp in salesByEmployee)
                                    {
                                        table.Cell().Element(CellStyle).Text(stt.ToString());
                                        table.Cell().Element(CellStyle).Text(emp.employeeId);
                                        table.Cell().Element(CellStyle).Text(emp.employeeName);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(emp.totalOrders.ToString());
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(emp.totalRevenue));
                                        stt++;
                                    }
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"DoanhSoNhanVien_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất doanh số nhân viên ra PDF", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/orders-by-status/export-excel
        // Xuất đơn hàng theo trạng thái ra Excel
        [HttpGet("orders-by-status/export-excel")]
        public async Task<IActionResult> ExportOrdersByStatusToExcel()
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

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Đơn hàng theo trạng thái");

                worksheet.Cells[1, 1].Value = "TenTech - ĐƠN HÀNG THEO TRẠNG THÁI";
                worksheet.Cells[1, 1, 1, 2].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "Trạng thái";
                worksheet.Cells[row, 2].Value = "Số lượng";
                worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250));
                row++;

                int total = 0;
                foreach (var item in ordersByStatus)
                {
                    worksheet.Cells[row, 1].Value = item.status;
                    worksheet.Cells[row, 2].Value = item.count;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                    total += item.count;
                    row++;
                }

                worksheet.Cells[row, 1].Value = "TỔNG CỘNG";
                worksheet.Cells[row, 2].Value = total;
                worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"DonHangTheoTrangThai_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất đơn hàng theo trạng thái ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/orders-by-status/export-pdf
        // Xuất đơn hàng theo trạng thái ra PDF
        [HttpGet("orders-by-status/export-pdf")]
        public async Task<IActionResult> ExportOrdersByStatusToPdf()
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

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text("TenTech - ĐƠN HÀNG THEO TRẠNG THÁI")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("Trạng thái").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Số lượng").Bold();
                                    });

                                    int total = 0;
                                    foreach (var item in ordersByStatus)
                                    {
                                        table.Cell().Element(CellStyle).Text(item.status);
                                        table.Cell().Element(CellStyle).AlignRight().Text(item.count.ToString());
                                        total += item.count;
                                    }

                                    table.Cell().Element(CellStyleHeader).Text("TỔNG CỘNG").Bold();
                                    table.Cell().Element(CellStyleHeader).AlignRight().Text(total.ToString()).Bold();
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"DonHangTheoTrangThai_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất đơn hàng theo trạng thái ra PDF", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/revenue-by-brand/export-excel
        // Xuất doanh thu theo thương hiệu ra Excel
        [HttpGet("revenue-by-brand/export-excel")]
        public async Task<IActionResult> ExportRevenueByBrandToExcel()
        {
            try
            {
                var revenueByBrand = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && 
                                d.Quantity != null && 
                                d.UnitPrice != null &&
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var grouped = revenueByBrand
                    .GroupBy(d => new 
                    { 
                        BrandId = d.Product?.Brand?.BrandId ?? "Không xác định",
                        BrandName = d.Product?.Brand?.BrandName ?? "Không xác định"
                    })
                    .Select(g => new
                    {
                        brandId = g.Key.BrandId,
                        brandName = g.Key.BrandName,
                        totalRevenue = g.Sum(d => (d.Quantity ?? 0) * (d.UnitPrice ?? 0)),
                        totalSold = g.Sum(d => d.Quantity ?? 0)
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Doanh thu theo thương hiệu");

                worksheet.Cells[1, 1].Value = "TenTech - DOANH THU THEO THƯƠNG HIỆU";
                worksheet.Cells[1, 1, 1, 4].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "STT";
                worksheet.Cells[row, 2].Value = "Mã thương hiệu";
                worksheet.Cells[row, 3].Value = "Tên thương hiệu";
                worksheet.Cells[row, 4].Value = "Số lượng bán";
                worksheet.Cells[row, 5].Value = "Doanh thu";
                worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250));
                row++;

                int stt = 1;
                decimal totalRevenue = 0;
                foreach (var item in grouped)
                {
                    worksheet.Cells[row, 1].Value = stt;
                    worksheet.Cells[row, 2].Value = item.brandId;
                    worksheet.Cells[row, 3].Value = item.brandName;
                    worksheet.Cells[row, 4].Value = item.totalSold;
                    worksheet.Cells[row, 5].Value = item.totalRevenue;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                    totalRevenue += item.totalRevenue;
                    row++;
                    stt++;
                }

                worksheet.Cells[row, 1].Value = "TỔNG CỘNG";
                worksheet.Cells[row, 5].Value = totalRevenue;
                worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"DoanhThuTheoThuongHieu_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất doanh thu theo thương hiệu ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/revenue-by-brand/export-pdf
        // Xuất doanh thu theo thương hiệu ra PDF
        [HttpGet("revenue-by-brand/export-pdf")]
        public async Task<IActionResult> ExportRevenueByBrandToPdf()
        {
            try
            {
                var revenueByBrand = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && 
                                d.Quantity != null && 
                                d.UnitPrice != null &&
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var grouped = revenueByBrand
                    .GroupBy(d => new 
                    { 
                        BrandId = d.Product?.Brand?.BrandId ?? "Không xác định",
                        BrandName = d.Product?.Brand?.BrandName ?? "Không xác định"
                    })
                    .Select(g => new
                    {
                        brandId = g.Key.BrandId,
                        brandName = g.Key.BrandName,
                        totalRevenue = g.Sum(d => (d.Quantity ?? 0) * (d.UnitPrice ?? 0)),
                        totalSold = g.Sum(d => d.Quantity ?? 0)
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text("TenTech - DOANH THU THEO THƯƠNG HIỆU")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.5f);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("STT").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Mã TH").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Tên thương hiệu").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignCenter().Text("SL bán").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Doanh thu").Bold();
                                    });

                                    int stt = 1;
                                    decimal totalRevenue = 0;
                                    foreach (var item in grouped)
                                    {
                                        table.Cell().Element(CellStyle).Text(stt.ToString());
                                        table.Cell().Element(CellStyle).Text(item.brandId);
                                        table.Cell().Element(CellStyle).Text(item.brandName);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(item.totalSold.ToString());
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(item.totalRevenue));
                                        totalRevenue += item.totalRevenue;
                                        stt++;
                                    }

                                    table.Cell().Element(CellStyleHeader).Text("TỔNG CỘNG").Bold();
                                    table.Cell().Element(CellStyleHeader).Text("");
                                    table.Cell().Element(CellStyleHeader).Text("");
                                    table.Cell().Element(CellStyleHeader).Text("");
                                    table.Cell().Element(CellStyleHeader).AlignRight().Text(FormatCurrency(totalRevenue)).Bold();
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"DoanhThuTheoThuongHieu_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất doanh thu theo thương hiệu ra PDF", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/revenue-by-payment-method/export-excel
        // Xuất doanh thu theo phương thức thanh toán ra Excel
        [HttpGet("revenue-by-payment-method/export-excel")]
        public async Task<IActionResult> ExportRevenueByPaymentMethodToExcel()
        {
            try
            {
                var invoices = await _context.SaleInvoices
                    .Where(si => !string.IsNullOrEmpty(si.PaymentMethod) &&
                                si.TotalAmount != null &&
                                si.TotalAmount > 0 &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var revenueByPayment = invoices
                    .GroupBy(si => si.PaymentMethod)
                    .Select(g => new
                    {
                        paymentMethod = g.Key,
                        totalRevenue = g.Sum(si => si.TotalAmount ?? 0),
                        orderCount = g.Count()
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Doanh thu theo phương thức thanh toán");

                worksheet.Cells[1, 1].Value = "TenTech - DOANH THU THEO PHƯƠNG THỨC THANH TOÁN";
                worksheet.Cells[1, 1, 1, 3].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "Phương thức thanh toán";
                worksheet.Cells[row, 2].Value = "Số đơn";
                worksheet.Cells[row, 3].Value = "Doanh thu";
                worksheet.Cells[row, 1, row, 3].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250));
                row++;

                decimal totalRevenue = 0;
                int totalOrders = 0;
                foreach (var item in revenueByPayment)
                {
                    worksheet.Cells[row, 1].Value = item.paymentMethod;
                    worksheet.Cells[row, 2].Value = item.orderCount;
                    worksheet.Cells[row, 3].Value = item.totalRevenue;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0";
                    totalRevenue += item.totalRevenue;
                    totalOrders += item.orderCount;
                    row++;
                }

                worksheet.Cells[row, 1].Value = "TỔNG CỘNG";
                worksheet.Cells[row, 2].Value = totalOrders;
                worksheet.Cells[row, 3].Value = totalRevenue;
                worksheet.Cells[row, 1, row, 3].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0";

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"DoanhThuTheoPhuongThucThanhToan_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất doanh thu theo phương thức thanh toán ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/revenue-by-payment-method/export-pdf
        // Xuất doanh thu theo phương thức thanh toán ra PDF
        [HttpGet("revenue-by-payment-method/export-pdf")]
        public async Task<IActionResult> ExportRevenueByPaymentMethodToPdf()
        {
            try
            {
                var invoices = await _context.SaleInvoices
                    .Where(si => !string.IsNullOrEmpty(si.PaymentMethod) &&
                                si.TotalAmount != null &&
                                si.TotalAmount > 0 &&
                                ((si.PaymentMethod == "Thanh toán khi nhận hàng" && si.Status == "Hoàn thành") ||
                                 (si.PaymentMethod == "Chuyển khoản ngân hàng")))
                    .ToListAsync();

                var revenueByPayment = invoices
                    .GroupBy(si => si.PaymentMethod)
                    .Select(g => new
                    {
                        paymentMethod = g.Key,
                        totalRevenue = g.Sum(si => si.TotalAmount ?? 0),
                        orderCount = g.Count()
                    })
                    .Where(x => x.totalRevenue > 0)
                    .OrderByDescending(x => x.totalRevenue)
                    .ToList();

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text("TenTech - DOANH THU THEO PHƯƠNG THỨC THANH TOÁN")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("Phương thức thanh toán").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignCenter().Text("Số đơn").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Doanh thu").Bold();
                                    });

                                    decimal totalRevenue = 0;
                                    int totalOrders = 0;
                                    foreach (var item in revenueByPayment)
                                    {
                                        table.Cell().Element(CellStyle).Text(item.paymentMethod);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(item.orderCount.ToString());
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(item.totalRevenue));
                                        totalRevenue += item.totalRevenue;
                                        totalOrders += item.orderCount;
                                    }

                                    table.Cell().Element(CellStyleHeader).Text("TỔNG CỘNG").Bold();
                                    table.Cell().Element(CellStyleHeader).AlignCenter().Text(totalOrders.ToString()).Bold();
                                    table.Cell().Element(CellStyleHeader).AlignRight().Text(FormatCurrency(totalRevenue)).Bold();
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"DoanhThuTheoPhuongThucThanhToan_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất doanh thu theo phương thức thanh toán ra PDF", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/product-sales-details/export-excel
        // Xuất chi tiết sản phẩm bán chạy ra Excel
        [HttpGet("product-sales-details/export-excel")]
        public async Task<IActionResult> ExportProductSalesDetailsToExcel([FromQuery] int top = 5)
        {
            try
            {
                var productSales = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && 
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
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

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add($"Top {top} sản phẩm bán chạy");

                worksheet.Cells[1, 1].Value = $"TenTech - TOP {top} SẢN PHẨM BÁN CHẠY";
                worksheet.Cells[1, 1, 1, 7].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                int row = 3;
                worksheet.Cells[row, 1].Value = "STT";
                worksheet.Cells[row, 2].Value = "Mã SP";
                worksheet.Cells[row, 3].Value = "Tên sản phẩm";
                worksheet.Cells[row, 4].Value = "Thương hiệu";
                worksheet.Cells[row, 5].Value = "Danh mục";
                worksheet.Cells[row, 6].Value = "Số lượng bán";
                worksheet.Cells[row, 7].Value = "Doanh thu";
                worksheet.Cells[row, 1, row, 7].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250));
                row++;

                foreach (var item in result)
                {
                    var rankValue = item.GetType().GetProperty("rank")?.GetValue(item);
                    var productId = item.GetType().GetProperty("productId")?.GetValue(item);
                    var productName = item.GetType().GetProperty("productName")?.GetValue(item);
                    var brandName = item.GetType().GetProperty("brandName")?.GetValue(item);
                    var category = item.GetType().GetProperty("category")?.GetValue(item);
                    var totalSold = item.GetType().GetProperty("totalSold")?.GetValue(item);
                    var totalRevenue = item.GetType().GetProperty("totalRevenue")?.GetValue(item);
                    
                    worksheet.Cells[row, 1].Value = rankValue;
                    worksheet.Cells[row, 2].Value = productId;
                    worksheet.Cells[row, 3].Value = productName;
                    worksheet.Cells[row, 4].Value = brandName;
                    worksheet.Cells[row, 5].Value = category;
                    worksheet.Cells[row, 6].Value = totalSold;
                    worksheet.Cells[row, 7].Value = totalRevenue;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"ChiTietSanPhamBanChay_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất chi tiết sản phẩm bán chạy ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/statistical-report/product-sales-details/export-pdf
        // Xuất chi tiết sản phẩm bán chạy ra PDF
        [HttpGet("product-sales-details/export-pdf")]
        public async Task<IActionResult> ExportProductSalesDetailsToPdf([FromQuery] int top = 5)
        {
            try
            {
                var productSales = await _context.SaleInvoiceDetails
                    .Include(d => d.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(d => d.SaleInvoice)
                    .Where(d => d.SaleInvoice != null && 
                                d.SaleInvoice.TotalAmount != null &&
                                ((d.SaleInvoice.PaymentMethod == "Thanh toán khi nhận hàng" && d.SaleInvoice.Status == "Hoàn thành") ||
                                 (d.SaleInvoice.PaymentMethod == "Chuyển khoản ngân hàng")))
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

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text($"TenTech - TOP {top} SẢN PHẨM BÁN CHẠY")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);
                                column.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.5f);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("STT").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Mã SP").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Tên sản phẩm").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Thương hiệu").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignCenter().Text("SL bán").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Doanh thu").Bold();
                                    });

                                    foreach (var item in result)
                                    {
                                        var rankValue = item.GetType().GetProperty("rank")?.GetValue(item)?.ToString() ?? "0";
                                        var productId = item.GetType().GetProperty("productId")?.GetValue(item)?.ToString() ?? "";
                                        var productName = item.GetType().GetProperty("productName")?.GetValue(item)?.ToString() ?? "";
                                        var brandName = item.GetType().GetProperty("brandName")?.GetValue(item)?.ToString() ?? "";
                                        var totalSold = item.GetType().GetProperty("totalSold")?.GetValue(item)?.ToString() ?? "0";
                                        var totalRevenueObj = item.GetType().GetProperty("totalRevenue")?.GetValue(item);
                                        var totalRevenue = totalRevenueObj != null ? (decimal)totalRevenueObj : 0m;
                                        
                                        table.Cell().Element(CellStyle).Text(rankValue);
                                        table.Cell().Element(CellStyle).Text(productId);
                                        table.Cell().Element(CellStyle).Text(productName);
                                        table.Cell().Element(CellStyle).Text(brandName);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(totalSold);
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(totalRevenue));
                                    }
                                });
                            });
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"ChiTietSanPhamBanChay_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất chi tiết sản phẩm bán chạy ra PDF", error = ex.Message });
            }
        }

        // Helper methods
        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .Padding(5)
                .Background(Colors.White);
        }

        private static IContainer CellStyleHeader(IContainer container)
        {
            return container
                .Border(1)
                .Padding(5)
                .Background(Colors.White);
        }

        private static string FormatCurrency(decimal amount)
        {
            return amount.ToString("N0") + " đ";
        }
    }
}
