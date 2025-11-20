using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WebLaptopBE.Models;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WebLaptopBE.Data;
namespace WebLaptopBE.Controllers
{
    [Route("api/Checkout")]
    [ApiController]
    public class CheckoutAPIController : ControllerBase
    {
        private readonly Testlaptop33Context _db = new();

        // POST: api/Checkout/create
        [HttpPost("create")]
        public IActionResult CreateOrder([FromBody] CheckoutRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.CustomerId))
                {
                    return BadRequest(new { message = "Thông tin không hợp lệ" });
                }

                // Kiểm tra khách hàng
                var customer = _db.Customers.FirstOrDefault(c => c.CustomerId == request.CustomerId);
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                // Lấy giỏ hàng
                var cart = _db.Carts
                    .Include(c => c.CartDetails)
                        .ThenInclude(cd => cd.Product)
                    .FirstOrDefault(c => c.CustomerId == request.CustomerId);

                if (cart == null)
                {
                    return BadRequest(new { message = "Không tìm thấy giỏ hàng" });
                }

                if (cart.CartDetails == null || !cart.CartDetails.Any())
                {
                    return BadRequest(new { message = "Giỏ hàng trống" });
                }

                // Lọc chỉ các sản phẩm đã chọn (nếu có)
                var cartDetailsToProcess = cart.CartDetails.ToList();
                if (request.SelectedCartDetailIds != null && request.SelectedCartDetailIds.Any())
                {
                    cartDetailsToProcess = cart.CartDetails
                        .Where(cd => request.SelectedCartDetailIds.Contains(cd.CartDetailId))
                        .ToList();
                    
                    if (!cartDetailsToProcess.Any())
                    {
                        return BadRequest(new { message = "Không có sản phẩm nào được chọn để thanh toán" });
                    }
                }

                // Validate thông tin giao hàng
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return BadRequest(new { message = "Vui lòng nhập họ và tên" });
                }
                
                if (string.IsNullOrWhiteSpace(request.Phone))
                {
                    return BadRequest(new { message = "Vui lòng nhập số điện thoại" });
                }
                
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Vui lòng nhập email" });
                }
                
                if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                {
                    return BadRequest(new { message = "Vui lòng nhập địa chỉ giao hàng" });
                }

                // Validate email
                if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new { message = "Email không hợp lệ" });
                }

                // Validate phone
                if (!Regex.IsMatch(request.Phone, @"^\d{9,15}$"))
                {
                    return BadRequest(new { message = "Số điện thoại không hợp lệ" });
                }

                // Tính tổng tiền chỉ cho các sản phẩm đã chọn
                decimal subtotal = 0;
                foreach (var item in cartDetailsToProcess)
                {
                    if (item == null) continue;
                    var price = item.Product?.SellingPrice ?? 0;
                    var quantity = item.Quantity ?? 0;
                    if (quantity > 0 && price > 0)
                    {
                        subtotal += price * quantity;
                    }
                }
                
                if (subtotal <= 0)
                {
                    return BadRequest(new { message = "Tổng tiền đơn hàng không hợp lệ" });
                }

                decimal deliveryFee = request.DeliveryFee ?? 0;
                decimal totalAmount = subtotal + deliveryFee;

                // Tạo đơn hàng
                var saleInvoice = new SaleInvoice
                {
                    SaleInvoiceId = GenerateSaleInvoiceId(),
                    CustomerId = request.CustomerId,
                    PaymentMethod = request.PaymentMethod ?? "Thanh toán khi nhận hàng",
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryFee = deliveryFee,
                    TotalAmount = totalAmount,
                    Status = "Chờ xử lý",
                    TimeCreate = DateTime.Now
                };

                // Tạo mã đơn hàng
                string saleInvoiceId = GenerateSaleInvoiceId();
                
                // Kiểm tra mã đơn hàng đã tồn tại chưa và tạo lại nếu trùng
                int maxAttempts = 50;
                int attempts = 0;
                while (_db.SaleInvoices.Any(si => si.SaleInvoiceId == saleInvoiceId) && attempts < maxAttempts)
                {
                    saleInvoiceId = GenerateSaleInvoiceId();
                    attempts++;
                }
                
                if (attempts >= maxAttempts)
                {
                    return BadRequest(new { message = "Không thể tạo mã đơn hàng. Vui lòng thử lại sau." });
                }
                
                saleInvoice.SaleInvoiceId = saleInvoiceId;
                _db.SaleInvoices.Add(saleInvoice);

                // Lấy ID lớn nhất hiện có một lần duy nhất trước khi tạo chi tiết
                int startDetailNumber = GetMaxSaleInvoiceDetailNumber();
                
                // Tạo chi tiết đơn hàng với ID tuần tự (chỉ cho các sản phẩm đã chọn)
                var detailIds = new HashSet<string>();
                int detailIndex = 0;
                
                foreach (var cartItem in cartDetailsToProcess)
                {
                    if (cartItem == null) continue;
                    
                    // Kiểm tra ProductId
                    if (string.IsNullOrWhiteSpace(cartItem.ProductId))
                    {
                        return BadRequest(new { message = "Một số sản phẩm trong giỏ hàng không hợp lệ" });
                    }
                    
                    var price = cartItem.Product?.SellingPrice ?? 0;
                    if (price <= 0)
                    {
                        return BadRequest(new { message = $"Sản phẩm {cartItem.ProductId} không có giá hợp lệ" });
                    }
                    
                    if (!cartItem.Quantity.HasValue || cartItem.Quantity.Value <= 0)
                    {
                        return BadRequest(new { message = $"Số lượng sản phẩm {cartItem.ProductId} không hợp lệ" });
                    }
                    
                    // Tạo ID tuần tự: SID002, SID003, SID004...
                    string detailId = $"SID{(startDetailNumber + detailIndex + 1):D3}";
                    
               
                    
                    detailIds.Add(detailId);
                    
                    // Xử lý specifications: đảm bảo format "CPU / RAM / ROM / Card"
                    string specifications = cartItem.Specifications ?? "";
                    
                    // Nếu là format cũ "ConfigurationId:xxx", chuyển đổi sang format mới
                    if (!string.IsNullOrWhiteSpace(specifications) && specifications.StartsWith("ConfigurationId:"))
                    {
                        var configId = specifications.Substring("ConfigurationId:".Length).Trim();
                        var config = _db.ProductConfigurations
                            .AsNoTracking()
                            .FirstOrDefault(pc => pc.ConfigurationId == configId);
                        
                        if (config != null)
                        {
                            var parts = new List<string>();
                            if (!string.IsNullOrWhiteSpace(config.Cpu)) parts.Add(config.Cpu);
                            if (!string.IsNullOrWhiteSpace(config.Ram)) parts.Add(config.Ram);
                            if (!string.IsNullOrWhiteSpace(config.Rom)) parts.Add(config.Rom);
                            if (!string.IsNullOrWhiteSpace(config.Card)) parts.Add(config.Card);
                            specifications = string.Join(" / ", parts);
                        }
                    }
                    // Nếu đã là format mới (có dấu " / "), giữ nguyên
                    // Nếu không có format nào, để trống
                    
                    var saleInvoiceDetail = new SaleInvoiceDetail
                    {
                        SaleInvoiceDetailId = detailId,
                        SaleInvoiceId = saleInvoice.SaleInvoiceId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = price,
                        Specifications = specifications
                    };
                    _db.SaleInvoiceDetails.Add(saleInvoiceDetail);
                    
                    detailIndex++;
                }
                
                if (!detailIds.Any())
                {
                    return BadRequest(new { message = "Không có sản phẩm hợp lệ trong giỏ hàng" });
                }

                // Lưu đơn hàng trước
                _db.SaveChanges();

                // Xóa các CartDetail đã chọn sau khi tạo đơn hàng thành công
                _db.CartDetails.RemoveRange(cartDetailsToProcess);
                
                // Kiểm tra xem còn CartDetail nào không, nếu không còn thì xóa luôn Cart
                var remainingDetails = _db.CartDetails
                    .Where(cd => cd.CartId == cart.CartId)
                    .ToList();
                
                if (!remainingDetails.Any())
                {
                    _db.Carts.Remove(cart);
                }
                
                _db.SaveChanges();

                return Ok(new
                {
                    message = "Đặt hàng thành công",
                    orderId = saleInvoice.SaleInvoiceId,
                    totalAmount = totalAmount
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerException = dbEx.InnerException?.Message ?? "";
                var errorMsg = "Lỗi khi lưu dữ liệu vào database";
                if (innerException.Contains("PRIMARY KEY") || innerException.Contains("duplicate"))
                {
                    errorMsg = "Mã đơn hàng hoặc chi tiết đơn hàng đã tồn tại. Vui lòng thử lại.";
                }
                else if (innerException.Contains("FOREIGN KEY") || innerException.Contains("constraint"))
                {
                    errorMsg = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại thông tin.";
                }
                return StatusCode(500, new { message = errorMsg, error = dbEx.Message, details = innerException });
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                System.Diagnostics.Debug.WriteLine($"Checkout Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                return StatusCode(500, new { 
                    message = "Lỗi khi tạo đơn hàng", 
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        // Helper methods
        private string GenerateSaleInvoiceId()
        {
            try
            {
                // Lấy tất cả các ID có format SI001, SI002... (chỉ lấy ID có độ dài 5 ký tự)
                var lastInvoice = _db.SaleInvoices
                    .Where(si => si.SaleInvoiceId != null && 
                                 si.SaleInvoiceId.StartsWith("SI") && 
                                 si.SaleInvoiceId.Length == 5)
                    .OrderByDescending(si => si.SaleInvoiceId)
                    .FirstOrDefault();
                
                if (lastInvoice == null)
                {
                    return "SI001";
                }

                var match = Regex.Match(lastInvoice.SaleInvoiceId, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int number))
                {
                    return $"SI{(number + 1):D3}";
                }

                // Nếu không parse được, tìm ID lớn nhất bằng cách so sánh số
                int maxNumber = 0;
                var allInvoices = _db.SaleInvoices
                    .Where(si => si.SaleInvoiceId != null && 
                                 si.SaleInvoiceId.StartsWith("SI") && 
                                 si.SaleInvoiceId.Length == 5)
                    .Select(si => si.SaleInvoiceId)
                    .ToList();
                
                foreach (var id in allInvoices)
                {
                    var m = Regex.Match(id, @"\d+");
                    if (m.Success && int.TryParse(m.Value, out int num))
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
                }
                
                return $"SI{(maxNumber + 1):D3}";
            }
            catch
            {
                // Fallback: tìm số lớn nhất và tăng lên
                try
                {
                    int maxNumber = 0;
                    var allInvoices = _db.SaleInvoices
                        .Where(si => si.SaleInvoiceId != null && si.SaleInvoiceId.StartsWith("SI"))
                        .Select(si => si.SaleInvoiceId)
                        .ToList();
                    
                    foreach (var id in allInvoices)
                    {
                        var m = Regex.Match(id, @"\d+");
                        if (m.Success && int.TryParse(m.Value, out int num))
                        {
                            maxNumber = Math.Max(maxNumber, num);
                        }
                    }
                    
                    return $"SI{(maxNumber + 1):D3}";
                }
                catch
                {
                    // Nếu vẫn lỗi, trả về SI001
                    return "SI001";
                }
            }
        }

        // Lấy số lớn nhất trong các ID chi tiết có format SID001, SID002...
        private int GetMaxSaleInvoiceDetailNumber()
        {
            try
            {
                var lastDetail = _db.SaleInvoiceDetails
                    .Where(sid => sid.SaleInvoiceDetailId != null && sid.SaleInvoiceDetailId.StartsWith("SID") && sid.SaleInvoiceDetailId.Length == 6)
                    .OrderByDescending(sid => sid.SaleInvoiceDetailId)
                    .FirstOrDefault();
                
                if (lastDetail == null)
                {
                    return 0; // Bắt đầu từ 0, ID đầu tiên sẽ là SID001
                }

                var match = Regex.Match(lastDetail.SaleInvoiceDetailId, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int number))
                {
                    return number;
                }
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private string GenerateSaleInvoiceDetailId()
        {
            int maxNumber = GetMaxSaleInvoiceDetailNumber();
            return $"SID{(maxNumber + 1):D3}";
        }

        public class CheckoutRequest
        {
            [JsonPropertyName("customerId")]
            public string CustomerId { get; set; } = string.Empty;
            
            [JsonPropertyName("fullName")]
            public string FullName { get; set; } = string.Empty;
            
            [JsonPropertyName("phone")]
            public string Phone { get; set; } = string.Empty;
            
            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;
            
            [JsonPropertyName("deliveryAddress")]
            public string DeliveryAddress { get; set; } = string.Empty;
            
            [JsonPropertyName("paymentMethod")]
            public string? PaymentMethod { get; set; }
            
            [JsonPropertyName("deliveryFee")]
            public decimal? DeliveryFee { get; set; }
            
            [JsonPropertyName("note")]
            public string? Note { get; set; }
            
            [JsonPropertyName("selectedCartDetailIds")]
            public List<string>? SelectedCartDetailIds { get; set; }
        }
    }
}
