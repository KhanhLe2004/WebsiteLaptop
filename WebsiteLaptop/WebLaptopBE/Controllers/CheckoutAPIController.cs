using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WebLaptopBE.Models;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using WebLaptopBE.Data;
using WebLaptopBE.Services;
using WebLaptopBE.Models.VnPay;
namespace WebLaptopBE.Controllers
{
    [Route("api/Checkout")]
    [ApiController]
    public class CheckoutAPIController : ControllerBase
    {
        private readonly Testlaptop33Context _db = new();
        private readonly EmailService _emailService;
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;

        public CheckoutAPIController(EmailService emailService, IVnPayService vnPayService, IConfiguration configuration)
        {
            _emailService = emailService;
            _vnPayService = vnPayService;
            _configuration = configuration;
        }

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

                // Kiểm tra phương thức thanh toán để quyết định xử lý tiếp theo
                if (request.PaymentMethod == "Chuyển khoản ngân hàng")
                {
                    // Tạo URL thanh toán VNPay
                    var vnPayRequest = new VnPaymentRequestModel
                    {
                        OrderId = int.Parse(saleInvoice.SaleInvoiceId.Substring(2)), // Lấy số từ SI001 -> 1
                        FullName = request.FullName,
                        Description = $"Thanh toán đơn hàng {saleInvoice.SaleInvoiceId}",
                        Amount = (double)totalAmount,
                        CreatedDate = DateTime.Now
                    };

                    var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnPayRequest);
                    
                    return Ok(new
                    {
                        message = "Đơn hàng đã được tạo. Chuyển hướng đến VNPay để thanh toán.",
                        orderId = saleInvoice.SaleInvoiceId,
                        totalAmount = totalAmount,
                        paymentUrl = paymentUrl,
                        requiresPayment = true
                    });
                }
                else
                {
                    // Thanh toán khi nhận hàng - xóa giỏ hàng ngay
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
                }

                // Gửi email xác nhận đơn hàng
                if (_emailService != null && !string.IsNullOrWhiteSpace(request.Email))
                {
                    try
                    {
                        // Lấy thông tin khách hàng
                        var customerInfo = _db.Customers.FirstOrDefault(c => c.CustomerId == request.CustomerId);
                        var customerName = customerInfo?.CustomerName ?? request.FullName;
                        
                        // Lấy chi tiết đơn hàng
                        var orderItems = new List<OrderItem>();
                        var invoiceDetails = _db.SaleInvoiceDetails
                            .Include(sid => sid.Product)
                            .Where(sid => sid.SaleInvoiceId == saleInvoice.SaleInvoiceId)
                            .ToList();
                        
                        foreach (var detail in invoiceDetails)
                        {
                            var product = detail.Product;
                            orderItems.Add(new OrderItem
                            {
                                ProductName = product?.ProductName ?? "Sản phẩm",
                                Specifications = detail.Specifications,
                                Quantity = detail.Quantity ?? 0,
                                UnitPrice = detail.UnitPrice ?? 0,
                                Total = (detail.Quantity ?? 0) * (detail.UnitPrice ?? 0)
                            });
                        }
                        
                        // Gửi email (không chặn response nếu email lỗi)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"Bắt đầu gửi email đến: {request.Email}");
                                System.Diagnostics.Debug.WriteLine($"EmailService is null: {_emailService == null}");
                                
                                var emailSent = await _emailService.SendOrderConfirmationEmailAsync(
                                    toEmail: request.Email,
                                    customerName: customerName,
                                    orderId: saleInvoice.SaleInvoiceId,
                                    phone: request.Phone,
                                    email: request.Email,
                                    address: request.DeliveryAddress,
                                    note: request.Note ?? "",
                                    items: orderItems,
                                    subtotal: subtotal,
                                    discount: 0, // Có thể tính từ promotion nếu có
                                    deliveryFee: deliveryFee,
                                    totalAmount: totalAmount
                                );
                                
                                if (emailSent)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Email đã được gửi thành công đến: {request.Email}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Không thể gửi email đến: {request.Email}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi email: {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                                if (ex.InnerException != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                                    System.Diagnostics.Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                                }
                            }
                        });
                    }
                    catch (Exception emailEx)
                    {
                        // Log lỗi nhưng không ảnh hưởng đến response
                        System.Diagnostics.Debug.WriteLine($"Error preparing email: {emailEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {emailEx.StackTrace}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"EmailService is null hoặc email trống. EmailService: {_emailService == null}, Email: {request.Email}");
                }

                return Ok(new
                {
                    message = "Đặt hàng thành công",
                    orderId = saleInvoice.SaleInvoiceId,
                    totalAmount = totalAmount,
                    requiresPayment = false
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

        // GET: api/Checkout/vnpay-callback
        [HttpGet("vnpay-callback")]
        public IActionResult VnPayCallback()
        {
            try
            {
                var response = _vnPayService.PaymentExecute(Request.Query);
                
                if (response.Success && VnPayHelper.IsSuccessResponse(response.VnPayResponseCode))
                {
                    // Parse OrderId từ TxnRef sử dụng helper
                    var txnRef = response.OrderId; // TxnRef từ VNPay
                    var orderIdNumber = VnPayHelper.ParseOrderIdFromTxnRef(txnRef);
                    var orderId = $"SI{orderIdNumber:D3}";
                    var saleInvoice = _db.SaleInvoices.FirstOrDefault(si => si.SaleInvoiceId == orderId);
                    
                    if (saleInvoice != null && saleInvoice.Status != "Đã thanh toán")
                    {
                        // Cập nhật trạng thái đơn hàng
                        saleInvoice.Status = "Đã thanh toán";
                        saleInvoice.PaymentMethod = "Chuyển khoản ngân hàng (VNPay)";
                        
                        _db.SaveChanges();
                        
                        // Xóa giỏ hàng sau khi thanh toán thành công
                        var customerId = saleInvoice.CustomerId;
                        var cart = _db.Carts
                            .Include(c => c.CartDetails)
                            .FirstOrDefault(c => c.CustomerId == customerId);
                            
                        if (cart != null && cart.CartDetails != null)
                        {
                            // Lấy danh sách sản phẩm trong đơn hàng
                            var orderProductIds = _db.SaleInvoiceDetails
                                .Where(sid => sid.SaleInvoiceId == orderId)
                                .Select(sid => sid.ProductId)
                                .ToList();
                            
                            // Xóa các sản phẩm tương ứng khỏi giỏ hàng
                            var cartDetailsToRemove = cart.CartDetails
                                .Where(cd => orderProductIds.Contains(cd.ProductId))
                                .ToList();
                            
                            _db.CartDetails.RemoveRange(cartDetailsToRemove);
                            
                            // Kiểm tra xem còn sản phẩm nào trong giỏ hàng không
                            var remainingDetails = cart.CartDetails
                                .Where(cd => !orderProductIds.Contains(cd.ProductId))
                                .ToList();
                            
                            if (!remainingDetails.Any())
                            {
                                _db.Carts.Remove(cart);
                            }
                            
                            _db.SaveChanges();
                        }
                        
                        // Chuyển hướng về trang thành công với thông báo
                        return Redirect($"{GetFrontendUrl()}/User/Account?success=payment&orderId={orderId}#tab-your-orders");
                    }
                    else if (saleInvoice != null && saleInvoice.Status == "Đã thanh toán")
                    {
                        // Đơn hàng đã được thanh toán trước đó
                        return Redirect($"{GetFrontendUrl()}/User/Account?success=payment&orderId={orderId}#tab-your-orders");
                    }
                    else
                    {
                        // Không tìm thấy đơn hàng
                        return Redirect($"{GetFrontendUrl()}/Cart/Checkout?error=order-not-found");
                    }
                }
                
                // Thanh toán thất bại hoặc bị hủy
                var errorCode = response.VnPayResponseCode;
                var errorMessage = VnPayHelper.GetResponseMessage(errorCode);
                
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5000";
                return Redirect($"{GetFrontendUrl()}/Cart/Checkout?error=payment-failed&code={errorCode}&message={Uri.EscapeDataString(errorMessage)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VNPay callback error: {ex.Message}");
                return Redirect($"{GetFrontendUrl()}/Cart/Checkout?error=payment-error");
            }
        }
        

        // Helper methods
        private string GetFrontendUrl()
        {
            return _configuration["FrontendUrl"] ?? "http://localhost:5253";
        }

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
