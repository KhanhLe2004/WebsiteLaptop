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
        private readonly Testlaptop38Context _db = new();
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
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutRequest request)
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

                // Không cập nhật số điện thoại vào database - chỉ sử dụng số điện thoại từ request cho đơn hàng

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
                decimal originalSubtotal = 0;
                decimal finalSubtotal = 0;
                
                // Lấy danh sách khuyến mại nếu có
                List<Promotion> applicablePromotions = new List<Promotion>();
                if ((request.SelectedDiscountPromotions != null && request.SelectedDiscountPromotions.Any()) ||
                    (request.SelectedFreeshipPromotions != null && request.SelectedFreeshipPromotions.Any()))
                {
                    var allSelectedPromotionIds = new List<string>();
                    if (request.SelectedDiscountPromotions != null)
                        allSelectedPromotionIds.AddRange(request.SelectedDiscountPromotions);
                    if (request.SelectedFreeshipPromotions != null)
                        allSelectedPromotionIds.AddRange(request.SelectedFreeshipPromotions);

                    applicablePromotions = await _db.Promotions
                        .Where(p => allSelectedPromotionIds.Contains(p.PromotionId))
                        .ToListAsync();
                }

                foreach (var item in cartDetailsToProcess)
                {
                    if (item == null) continue;
                    var price = item.Product?.SellingPrice ?? 0;
                    var quantity = item.Quantity ?? 0;
                    if (quantity > 0 && price > 0)
                    {
                        var itemTotal = price * quantity;
                        originalSubtotal += itemTotal;

                        // Áp dụng khuyến mại giảm giá nếu có
                        var discountPromotion = applicablePromotions.FirstOrDefault(p => 
                            p.ProductId == item.ProductId && 
                            request.SelectedDiscountPromotions != null && 
                            request.SelectedDiscountPromotions.Contains(p.PromotionId));

                        if (discountPromotion != null)
                        {
                            // Lấy phần trăm từ ContentDetail
                            var discountPercent = ExtractDiscountPercent(discountPromotion.ContentDetail);
                            if (discountPercent > 0)
                            {
                                finalSubtotal += itemTotal * (1 - discountPercent / 100m);
                            }
                            else
                            {
                                finalSubtotal += itemTotal;
                            }
                        }
                        else
                        {
                            // Sản phẩm không có khuyến mại giảm giá
                            finalSubtotal += itemTotal;
                        }
                    }
                }
                
                if (originalSubtotal <= 0)
                {
                    return BadRequest(new { message = "Tổng tiền đơn hàng không hợp lệ" });
                }

                decimal deliveryFee = request.DeliveryFee ?? 0;
                
                // Áp dụng freeship nếu có
                if (request.SelectedFreeshipPromotions != null && 
                    request.SelectedFreeshipPromotions.Any() && 
                    applicablePromotions.Any(p => request.SelectedFreeshipPromotions.Contains(p.PromotionId)))
                {
                    deliveryFee = 0;
                }

                decimal totalAmount = finalSubtotal + deliveryFee;

                // Kiểm tra phương thức thanh toán để quyết định xử lý tiếp theo
                if (request.PaymentMethod == "Chuyển khoản ngân hàng")
                {
                    // Với VNPay, lưu thông tin tạm và tạo URL thanh toán
                    // Đơn hàng sẽ được tạo sau khi thanh toán thành công
                    
                    var pendingOrder = new PendingOrder
                    {
                        CustomerId = request.CustomerId,
                        FullName = request.FullName,
                        Phone = request.Phone,
                        Email = request.Email,
                        DeliveryAddress = request.DeliveryAddress,
                        PaymentMethod = request.PaymentMethod,
                        DeliveryFee = deliveryFee,
                        Note = null, // Đã xóa ghi chú đơn hàng
                        SelectedCartDetailIds = request.SelectedCartDetailIds ?? cartDetailsToProcess.Select(cd => cd.CartDetailId).ToList(),
                        TotalAmount = totalAmount,
                        SelectedDiscountPromotions = request.SelectedDiscountPromotions,
                        SelectedFreeshipPromotions = request.SelectedFreeshipPromotions,
                        Discount = request.Discount,
                        ShippingDiscount = request.ShippingDiscount,
                        CreatedDate = DateTime.Now
                    };


                    // Tạo mã tham chiếu tạm thời cho VNPay kết hợp với CustomerId
                    var tempOrderId = DateTime.Now.Ticks % 1000000; // Lấy 6 chữ số cuối của timestamp
                    var txnRef = $"{tempOrderId}_{request.CustomerId}"; // Kết hợp tempOrderId với CustomerId
                    
                    // Lưu mapping giữa TxnRef và thông tin đơn hàng
                    var pendingOrderWithTxnRef = new PendingOrderWithTxnRef
                    {
                        TxnRef = txnRef,
                        CustomerId = request.CustomerId,
                        FullName = request.FullName,
                        Phone = request.Phone,
                        Email = request.Email,
                        DeliveryAddress = request.DeliveryAddress,
                        PaymentMethod = request.PaymentMethod,
                        DeliveryFee = deliveryFee,
                        Note = null, // Đã xóa ghi chú đơn hàng
                        SelectedCartDetailIds = request.SelectedCartDetailIds ?? cartDetailsToProcess.Select(cd => cd.CartDetailId).ToList(),
                        TotalAmount = totalAmount,
                        SelectedDiscountPromotions = request.SelectedDiscountPromotions,
                        SelectedFreeshipPromotions = request.SelectedFreeshipPromotions,
                        Discount = request.Discount,
                        ShippingDiscount = request.ShippingDiscount,
                        CreatedDate = DateTime.Now
                    };

                    // Lưu thông tin đơn hàng tạm với key là TxnRef
                    var pendingOrderJson = System.Text.Json.JsonSerializer.Serialize(pendingOrderWithTxnRef);
                    HttpContext.Session.SetString($"PendingOrder_{txnRef}", pendingOrderJson);
                    
                    var vnPayRequest = new VnPaymentRequestModel
                    {
                        OrderId = (int)tempOrderId,
                        FullName = request.FullName,
                        Description = $"Thanh toan don hang {tempOrderId}",
                        Amount = (double)totalAmount,
                        CreatedDate = DateTime.Now
                    };

                    var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnPayRequest, txnRef);
                    
                    return Ok(new
                    {
                        message = "Chuyển hướng đến VNPay để thanh toán. Đơn hàng sẽ được tạo sau khi thanh toán thành công.",
                        tempOrderId = tempOrderId,
                        totalAmount = totalAmount,
                        paymentUrl = paymentUrl,
                        requiresPayment = true
                    });
                }
                else
                {
                    // Thanh toán khi nhận hàng - tạo đơn hàng ngay
                    var orderResult = await CreateSaleInvoiceFromCart(request, cartDetailsToProcess, totalAmount, deliveryFee);
                    if (!orderResult.Success)
                    {
                        return BadRequest(new { message = orderResult.ErrorMessage });
                    }

                    // Xóa giỏ hàng sau khi tạo đơn hàng thành công
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

                    // Gửi email xác nhận đơn hàng cho COD
                    await SendOrderConfirmationEmail(request, orderResult.OrderId, totalAmount, deliveryFee, cartDetailsToProcess);

                    return Ok(new
                    {
                        message = "Đặt hàng thành công",
                        orderId = orderResult.OrderId,
                        totalAmount = totalAmount,
                        requiresPayment = false
                    });
                }

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

        // GET: api/Checkout/promotions
        [HttpGet("promotions")]
        public async Task<IActionResult> GetPromotions([FromQuery] string customerId, [FromQuery] List<string>? selectedCartDetailIds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "CustomerId không được để trống" });
                }

                // Lấy giỏ hàng
                var cart = _db.Carts
                    .Include(c => c.CartDetails)
                        .ThenInclude(cd => cd.Product)
                    .FirstOrDefault(c => c.CustomerId == customerId);

                if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                {
                    return Ok(new { discountPromotions = new List<object>(), freeshipPromotions = new List<object>() });
                }

                // Lọc chỉ các sản phẩm đã chọn
                var cartDetailsToProcess = cart.CartDetails.ToList();
                if (selectedCartDetailIds != null && selectedCartDetailIds.Any())
                {
                    cartDetailsToProcess = cart.CartDetails
                        .Where(cd => selectedCartDetailIds.Contains(cd.CartDetailId))
                        .ToList();
                }

                if (!cartDetailsToProcess.Any())
                {
                    return Ok(new { discountPromotions = new List<object>(), freeshipPromotions = new List<object>() });
                }

                // Lấy danh sách ProductId đã chọn
                var productIds = cartDetailsToProcess.Select(cd => cd.ProductId).ToList();

                // Lấy khuyến mại cho các sản phẩm đã chọn
                var promotions = await _db.Promotions
                    .Include(p => p.Product)
                    .Where(p => productIds.Contains(p.ProductId) && !string.IsNullOrEmpty(p.Type))
                    .ToListAsync();

                // Phân loại khuyến mại
                var discountPromotions = promotions
                    .Where(p => p.Type != null && (p.Type.ToLower().Contains("giảm") || p.Type.Contains("%")))
                    .Select(p => {
                        var discountPercent = ExtractDiscountPercent(p.ContentDetail);
                        var displayText = discountPercent > 0 
                            ? $"Giảm giá {discountPercent}% - {p.Product?.ProductName}"
                            : $"{p.Type} - {p.Product?.ProductName}";
                        
                        return new {
                            promotionId = p.PromotionId,
                            productId = p.ProductId,
                            productName = p.Product?.ProductName,
                            type = p.Type,
                            contentDetail = p.ContentDetail,
                            displayText = displayText
                        };
                    })
                    .ToList();

                var freeshipPromotions = promotions
                    .Where(p => p.Type != null && p.Type.ToLower().Contains("freeship"))
                    .Select(p => new {
                        promotionId = p.PromotionId,
                        productId = p.ProductId,
                        productName = p.Product?.ProductName,
                        type = p.Type,
                        contentDetail = p.ContentDetail,
                        displayText = "Freeship"  // Chỉ hiển thị "Freeship", không có tên sản phẩm
                    })
                    .ToList();

                return Ok(new { 
                    discountPromotions = discountPromotions,
                    freeshipPromotions = freeshipPromotions
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPromotions error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khuyến mại", error = ex.Message });
            }
        }

        // POST: api/Checkout/apply-promotion
        [HttpPost("apply-promotion")]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.CustomerId))
                {
                    return BadRequest(new { message = "Thông tin không hợp lệ" });
                }

                if ((request.SelectedDiscountPromotions == null || !request.SelectedDiscountPromotions.Any()) &&
                    (request.SelectedFreeshipPromotions == null || !request.SelectedFreeshipPromotions.Any()))
                {
                    return BadRequest(new { message = "Vui lòng chọn ít nhất một khuyến mại" });
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

                if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                {
                    return BadRequest(new { message = "Giỏ hàng trống" });
                }

                // Lọc chỉ các sản phẩm đã chọn
                var cartDetailsToProcess = cart.CartDetails.ToList();
                if (request.SelectedCartDetailIds != null && request.SelectedCartDetailIds.Any())
                {
                    cartDetailsToProcess = cart.CartDetails
                        .Where(cd => request.SelectedCartDetailIds.Contains(cd.CartDetailId))
                        .ToList();
                    
                    if (!cartDetailsToProcess.Any())
                    {
                        return BadRequest(new { message = "Không có sản phẩm nào được chọn" });
                    }
                }

                // Lấy tất cả khuyến mại được chọn
                var allSelectedPromotionIds = new List<string>();
                if (request.SelectedDiscountPromotions != null)
                    allSelectedPromotionIds.AddRange(request.SelectedDiscountPromotions);
                if (request.SelectedFreeshipPromotions != null)
                    allSelectedPromotionIds.AddRange(request.SelectedFreeshipPromotions);

                var promotions = await _db.Promotions
                    .Include(p => p.Product)
                    .Where(p => allSelectedPromotionIds.Contains(p.PromotionId))
                    .ToListAsync();

                if (!promotions.Any())
                {
                    return BadRequest(new { message = "Không tìm thấy khuyến mại được chọn" });
                }

                // Tính toán khuyến mại
                decimal originalSubtotal = 0;
                decimal discountedSubtotal = 0;
                decimal deliveryFee = request.DeliveryFee ?? 30000;
                decimal finalDeliveryFee = deliveryFee;
                var applicablePromotions = new List<string>();
                var promotionDetails = new List<object>();
                var hasFreeship = false;

                foreach (var item in cartDetailsToProcess)
                {
                    if (item == null) continue;
                    var price = item.Product?.SellingPrice ?? 0;
                    var quantity = item.Quantity ?? 0;
                    if (quantity > 0 && price > 0)
                    {
                        var itemTotal = price * quantity;
                        originalSubtotal += itemTotal;

                        // Tìm khuyến mại giảm giá cho sản phẩm này
                        var discountPromotion = promotions.FirstOrDefault(p => 
                            p.ProductId == item.ProductId && 
                            request.SelectedDiscountPromotions != null && 
                            request.SelectedDiscountPromotions.Contains(p.PromotionId));

                        if (discountPromotion != null)
                        {
                            // Lấy phần trăm từ ContentDetail
                            var discountPercent = ExtractDiscountPercent(discountPromotion.ContentDetail);
                            if (discountPercent > 0)
                            {
                                var discountedPrice = itemTotal * (1 - discountPercent / 100m);
                                var discountAmount = itemTotal - discountedPrice;
                                discountedSubtotal += discountedPrice;
                                
                                applicablePromotions.Add($"Giảm giá {discountPercent}% - {item.Product?.ProductName}");
                                promotionDetails.Add(new {
                                    type = "discount",
                                    productName = item.Product?.ProductName,
                                    discountPercent = discountPercent,
                                    discountAmount = discountAmount,
                                    displayText = $"Giảm giá {discountPercent}% - {item.Product?.ProductName}"
                                });
                            }
                            else
                            {
                                discountedSubtotal += itemTotal;
                            }
                        }
                        else
                        {
                            discountedSubtotal += itemTotal;
                        }

                        // Kiểm tra freeship cho sản phẩm này
                        var freeshipPromotion = promotions.FirstOrDefault(p => 
                            p.ProductId == item.ProductId && 
                            request.SelectedFreeshipPromotions != null && 
                            request.SelectedFreeshipPromotions.Contains(p.PromotionId));

                        if (freeshipPromotion != null && !hasFreeship)
                        {
                            hasFreeship = true;
                            finalDeliveryFee = 0;
                            // Không hiển thị cụ thể sản phẩm nào có freeship
                        }
                    }
                }

                var discount = originalSubtotal - discountedSubtotal;
                var shippingDiscount = deliveryFee - finalDeliveryFee;
                var totalDiscount = discount + shippingDiscount;
                var finalTotal = discountedSubtotal + finalDeliveryFee;

                // Thêm thông báo freeship nếu có
                if (hasFreeship)
                {
                    applicablePromotions.Add("Freeship");
                    promotionDetails.Add(new {
                        type = "freeship",
                        discountAmount = shippingDiscount,
                        displayText = "Freeship cho toàn bộ đơn hàng"
                    });
                }

                return Ok(new
                {
                    message = "Áp dụng khuyến mại thành công",
                    applicablePromotions = applicablePromotions,
                    promotionDetails = promotionDetails,
                    originalSubtotal = originalSubtotal,
                    discountedSubtotal = discountedSubtotal,
                    discount = discount,
                    originalDeliveryFee = deliveryFee,
                    finalDeliveryFee = finalDeliveryFee,
                    shippingDiscount = shippingDiscount,
                    totalDiscount = totalDiscount,
                    finalTotal = finalTotal,
                    selectedDiscountPromotions = request.SelectedDiscountPromotions,
                    selectedFreeshipPromotions = request.SelectedFreeshipPromotions,
                    hasFreeship = hasFreeship
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyPromotion error: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi áp dụng khuyến mại", error = ex.Message });
            }
        }

        // Helper method để lấy phần trăm giảm giá từ ContentDetail
        private decimal ExtractDiscountPercent(string? contentDetail)
        {
            if (string.IsNullOrWhiteSpace(contentDetail))
                return 0;

            try
            {
                // Tìm số phần trăm trong chuỗi (ví dụ: "Giảm 15% giá bán" -> 15)
                var match = System.Text.RegularExpressions.Regex.Match(contentDetail, @"(\d+)%");
                if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal percent))
                {
                    return percent;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return 0;
        }

        // GET: api/Checkout/vnpay-callback
        [HttpGet("vnpay-callback")]
        public async Task<IActionResult> VnPayCallback()
        {
            try
            {
                // Log VNPay callback parameters for debugging
                System.Diagnostics.Debug.WriteLine("=== VNPay Callback ===");
                foreach (var param in Request.Query)
                {
                    System.Diagnostics.Debug.WriteLine($"{param.Key}: {param.Value}");
                }
                System.Diagnostics.Debug.WriteLine("=====================");

                var response = _vnPayService.PaymentExecute(Request.Query);
                
                if (response.Success && response.VnPayResponseCode == "00")
                {
                    // Parse TxnRef từ VNPay response
                    var txnRef = response.OrderId; // TxnRef từ VNPay
                    
                    // Tìm thông tin đơn hàng tạm từ session dựa trên TxnRef
                    string? pendingOrderJson = null;
                    string? customerId = null;
                    
                    // Tìm kiếm trực tiếp bằng TxnRef
                    var sessionKey = $"PendingOrder_{txnRef}";
                    pendingOrderJson = HttpContext.Session.GetString(sessionKey);
                    
                    if (!string.IsNullOrEmpty(pendingOrderJson))
                    {
                        try
                        {
                            var tempOrder = System.Text.Json.JsonSerializer.Deserialize<PendingOrderWithTxnRef>(pendingOrderJson);
                            if (tempOrder != null && tempOrder.TxnRef == txnRef)
                            {
                                customerId = tempOrder.CustomerId;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deserializing pending order: {ex.Message}");
                            pendingOrderJson = null;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(pendingOrderJson) && !string.IsNullOrEmpty(customerId))
                    {
                        try
                        {
                            var pendingOrder = System.Text.Json.JsonSerializer.Deserialize<PendingOrderWithTxnRef>(pendingOrderJson);
                            if (pendingOrder != null)
                            {
                                // Không cập nhật số điện thoại vào database - chỉ sử dụng số điện thoại từ pendingOrder cho đơn hàng
                                
                                // Lấy lại thông tin giỏ hàng
                                var cart = _db.Carts
                                    .Include(c => c.CartDetails)
                                        .ThenInclude(cd => cd.Product)
                                    .FirstOrDefault(c => c.CustomerId == customerId);

                                if (cart != null)
                                {
                                    var cartDetailsToProcess = cart.CartDetails
                                        .Where(cd => pendingOrder.SelectedCartDetailIds.Contains(cd.CartDetailId))
                                        .ToList();

                                    // Tạo đơn hàng thực sự
                                    var orderResult = await CreateSaleInvoiceFromPendingOrder(pendingOrder, cartDetailsToProcess);
                                    
                                    if (orderResult.Success)
                                    {
                                        // Xóa giỏ hàng sau khi tạo đơn hàng thành công
                                        _db.CartDetails.RemoveRange(cartDetailsToProcess);
                                        
                                        var remainingDetails = _db.CartDetails
                                            .Where(cd => cd.CartId == cart.CartId)
                                            .ToList();
                                        
                                        if (!remainingDetails.Any())
                                        {
                                            _db.Carts.Remove(cart);
                                        }
                                        
                                        _db.SaveChanges();

                                        // Xóa thông tin tạm khỏi session
                                        HttpContext.Session.Remove($"PendingOrder_{txnRef}");

                                        // Gửi email xác nhận đơn hàng (không chặn nếu lỗi)
                                        try
                                        {
                                            await SendOrderConfirmationEmailFromPendingOrder(pendingOrder, orderResult.OrderId, cartDetailsToProcess);
                                        }
                                        catch (Exception emailEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Email sending failed: {emailEx.Message}");
                                            // Không chặn luồng chính nếu email thất bại
                                        }

                                        // Chuyển hướng về trang thành công với thông báo
                                        return Redirect($"{GetFrontendUrl()}/User/Account?success=true&message={Uri.EscapeDataString("Đặt hàng thành công!")}&orderId={orderResult.OrderId}#tab-your-orders");
                                    }
                                    else
                                    {
                                        // Tạo đơn hàng thất bại - chuyển về trang lỗi
                                        System.Diagnostics.Debug.WriteLine($"Order creation failed: {orderResult.ErrorMessage}");
                                        return Redirect($"{GetFrontendUrl()}/Cart/Checkout?error=order-creation-failed&message={Uri.EscapeDataString(orderResult.ErrorMessage)}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing VNPay callback: {ex.Message}");
                        }
                    }
                    
                    // Nếu không tìm thấy thông tin đơn hàng tạm
                    return Redirect($"{GetFrontendUrl()}/Cart/Checkout?error=order-not-found");
                }
                
                // Thanh toán thất bại hoặc bị hủy
                var errorCode = response.VnPayResponseCode;
                var errorMessage = GetVnPayErrorMessage(errorCode);
                
                var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5000";
                return Redirect($"{GetFrontendUrl()}/Cart/Checkout?error=payment-failed&code={errorCode}&message={Uri.EscapeDataString(errorMessage)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VNPay callback error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                var errorMessage = Uri.EscapeDataString($"Lỗi xử lý callback: {ex.Message}");
                return Redirect($"{GetFrontendUrl()}/Cart/Checkout?error=payment-error&message={errorMessage}");
            }
        }
        

        // Helper methods
        private string GetFrontendUrl()
        {
            return _configuration["FrontendUrl"] ?? "http://localhost:5253";
        }

        // Tạo đơn hàng từ giỏ hàng (cho COD)
        private async Task<(bool Success, string OrderId, string ErrorMessage)> CreateSaleInvoiceFromCart(
            CheckoutRequest request, 
            List<CartDetail> cartDetailsToProcess, 
            decimal totalAmount, 
            decimal deliveryFee)
        {
            try
            {
                // Tính tổng khuyến mại
                decimal totalDiscountAmount = (request.Discount ?? 0) + (request.ShippingDiscount ?? 0);

                // Tạo đơn hàng
                var saleInvoice = new SaleInvoice
                {
                    SaleInvoiceId = GenerateSaleInvoiceId(),
                    CustomerId = request.CustomerId,
                    PaymentMethod = request.PaymentMethod ?? "Thanh toán khi nhận hàng",
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryFee = deliveryFee,
                    TotalAmount = totalAmount,
                    Discount = totalDiscountAmount,
                    Status = "Chờ xử lý",
                    TimeCreate = DateTime.Now,
                    Phone = request.Phone // Lưu số điện thoại từ form checkout
                };

                // Tạo mã đơn hàng unique
                string saleInvoiceId = GenerateSaleInvoiceId();
                int maxAttempts = 50;
                int attempts = 0;
                while (_db.SaleInvoices.Any(si => si.SaleInvoiceId == saleInvoiceId) && attempts < maxAttempts)
                {
                    saleInvoiceId = GenerateSaleInvoiceId();
                    attempts++;
                }
                
                if (attempts >= maxAttempts)
                {
                    return (false, "", "Không thể tạo mã đơn hàng. Vui lòng thử lại sau.");
                }
                
                saleInvoice.SaleInvoiceId = saleInvoiceId;
                _db.SaleInvoices.Add(saleInvoice);

                // Tạo chi tiết đơn hàng
                var result = CreateSaleInvoiceDetails(saleInvoice.SaleInvoiceId, cartDetailsToProcess);
                if (!result.Success)
                {
                    return (false, "", result.ErrorMessage);
                }

                _db.SaveChanges();
                return (true, saleInvoice.SaleInvoiceId, "");
            }
            catch (Exception ex)
            {
                return (false, "", $"Lỗi khi tạo đơn hàng: {ex.Message}");
            }
        }

        // Tạo đơn hàng từ thông tin tạm (cho VNPay)
        private async Task<(bool Success, string OrderId, string ErrorMessage)> CreateSaleInvoiceFromPendingOrder(
            PendingOrder pendingOrder, 
            List<CartDetail> cartDetailsToProcess)
        {
            try
            {
                // Tính tổng khuyến mại
                decimal totalDiscountAmount = (pendingOrder.Discount ?? 0) + (pendingOrder.ShippingDiscount ?? 0);

                // Tạo đơn hàng
                var saleInvoice = new SaleInvoice
                {
                    SaleInvoiceId = GenerateSaleInvoiceId(),
                    CustomerId = pendingOrder.CustomerId,
                    PaymentMethod = "Chuyển khoản ngân hàng",
                    DeliveryAddress = pendingOrder.DeliveryAddress,
                    DeliveryFee = pendingOrder.DeliveryFee,
                    TotalAmount = pendingOrder.TotalAmount,
                    Discount = totalDiscountAmount,
                    Status = "Chờ xử lý",
                    TimeCreate = DateTime.Now,
                    Phone = pendingOrder.Phone // Lưu số điện thoại từ form checkout
                };

                // Tạo mã đơn hàng unique
                string saleInvoiceId = GenerateSaleInvoiceId();
                int maxAttempts = 50;
                int attempts = 0;
                while (_db.SaleInvoices.Any(si => si.SaleInvoiceId == saleInvoiceId) && attempts < maxAttempts)
                {
                    saleInvoiceId = GenerateSaleInvoiceId();
                    attempts++;
                }
                
                if (attempts >= maxAttempts)
                {
                    return (false, "", "Không thể tạo mã đơn hàng. Vui lòng thử lại sau.");
                }
                
                saleInvoice.SaleInvoiceId = saleInvoiceId;
                _db.SaleInvoices.Add(saleInvoice);

                // Tạo chi tiết đơn hàng
                var result = CreateSaleInvoiceDetails(saleInvoice.SaleInvoiceId, cartDetailsToProcess);
                if (!result.Success)
                {
                    return (false, "", result.ErrorMessage);
                }

                _db.SaveChanges();
                return (true, saleInvoice.SaleInvoiceId, "");
            }
            catch (Exception ex)
            {
                return (false, "", $"Lỗi khi tạo đơn hàng: {ex.Message}");
            }
        }

        // Tạo chi tiết đơn hàng
        private (bool Success, string ErrorMessage) CreateSaleInvoiceDetails(string saleInvoiceId, List<CartDetail> cartDetailsToProcess)
        {
            try
            {
                int startDetailNumber = GetMaxSaleInvoiceDetailNumber();
                int detailIndex = 0;
                
                foreach (var cartItem in cartDetailsToProcess)
                {
                    if (cartItem == null) continue;
                    
                    if (string.IsNullOrWhiteSpace(cartItem.ProductId))
                    {
                        return (false, "Một số sản phẩm trong giỏ hàng không hợp lệ");
                    }
                    
                    var price = cartItem.Product?.SellingPrice ?? 0;
                    if (price <= 0)
                    {
                        return (false, $"Sản phẩm {cartItem.ProductId} không có giá hợp lệ");
                    }
                    
                    if (!cartItem.Quantity.HasValue || cartItem.Quantity.Value <= 0)
                    {
                        return (false, $"Số lượng sản phẩm {cartItem.ProductId} không hợp lệ");
                    }
                    
                    string detailId = $"SID{(startDetailNumber + detailIndex + 1):D3}";
                    
                    // Xử lý specifications
                    string specifications = cartItem.Specifications ?? "";
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
                    
                    var saleInvoiceDetail = new SaleInvoiceDetail
                    {
                        SaleInvoiceDetailId = detailId,
                        SaleInvoiceId = saleInvoiceId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = price,
                        Specifications = specifications
                    };
                    _db.SaleInvoiceDetails.Add(saleInvoiceDetail);
                    
                    detailIndex++;
                }
                
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi tạo chi tiết đơn hàng: {ex.Message}");
            }
        }
        
        // Gửi email xác nhận đơn hàng (cho COD)
        private async Task SendOrderConfirmationEmail(
            CheckoutRequest request, 
            string orderId, 
            decimal totalAmount, 
            decimal deliveryFee, 
            List<CartDetail> cartDetailsToProcess)
        {
            if (_emailService != null && !string.IsNullOrWhiteSpace(request.Email))
            {
                try
                {
                    var customerInfo = _db.Customers.FirstOrDefault(c => c.CustomerId == request.CustomerId);
                    var customerName = customerInfo?.CustomerName ?? request.FullName;
                    
                    var orderItems = new List<OrderItem>();
                    var invoiceDetails = _db.SaleInvoiceDetails
                        .Include(sid => sid.Product)
                        .Where(sid => sid.SaleInvoiceId == orderId)
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
                    
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendOrderConfirmationEmailAsync(
                                toEmail: request.Email,
                                customerName: customerName,
                                orderId: orderId,
                                phone: request.Phone,
                                email: request.Email,
                                address: request.DeliveryAddress,
                                note: request.Note ?? "",
                                items: orderItems,
                                subtotal: (totalAmount - deliveryFee) + ((request.Discount ?? 0) + (request.ShippingDiscount ?? 0)),
                                discount: (request.Discount ?? 0) + (request.ShippingDiscount ?? 0),
                                deliveryFee: deliveryFee,
                                totalAmount: totalAmount
                            );
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi email: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error preparing email: {ex.Message}");
                }
            }
        }

        // Gửi email xác nhận đơn hàng (cho VNPay)
        private async Task SendOrderConfirmationEmailFromPendingOrder(
            PendingOrder pendingOrder, 
            string orderId, 
            List<CartDetail> cartDetailsToProcess)
        {
            if (_emailService != null && !string.IsNullOrWhiteSpace(pendingOrder.Email))
            {
                try
                {
                    var customerInfo = _db.Customers.FirstOrDefault(c => c.CustomerId == pendingOrder.CustomerId);
                    var customerName = customerInfo?.CustomerName ?? pendingOrder.FullName;
                    
                    var orderItems = new List<OrderItem>();
                    var invoiceDetails = _db.SaleInvoiceDetails
                        .Include(sid => sid.Product)
                        .Where(sid => sid.SaleInvoiceId == orderId)
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
                    
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendOrderConfirmationEmailAsync(
                                toEmail: pendingOrder.Email,
                                customerName: customerName,
                                orderId: orderId,
                                phone: pendingOrder.Phone,
                                email: pendingOrder.Email,
                                address: pendingOrder.DeliveryAddress,
                                note: pendingOrder.Note ?? "",
                                items: orderItems,
                                subtotal: (pendingOrder.TotalAmount - pendingOrder.DeliveryFee) + ((pendingOrder.Discount ?? 0) + (pendingOrder.ShippingDiscount ?? 0)),
                                discount: (pendingOrder.Discount ?? 0) + (pendingOrder.ShippingDiscount ?? 0),
                                deliveryFee: pendingOrder.DeliveryFee,
                                totalAmount: pendingOrder.TotalAmount
                            );
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi email: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error preparing email: {ex.Message}");
                }
            }
        }

        // Helper method để lấy thông báo lỗi VNPay
        private string GetVnPayErrorMessage(string errorCode)
        {
            return errorCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP). Xin quý khách vui lòng thực hiện lại giao dịch.",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định. Xin quý khách vui lòng thực hiện lại giao dịch",
                "97" => "Chữ ký không hợp lệ",
                "99" => "Các lỗi khác (lỗi còn lại, không có trong danh sách mã lỗi đã liệt kê)",
                _ => "Giao dịch không thành công"
            };
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
            
            [JsonPropertyName("selectedDiscountPromotions")]
            public List<string>? SelectedDiscountPromotions { get; set; }
            
            [JsonPropertyName("selectedFreeshipPromotions")]
            public List<string>? SelectedFreeshipPromotions { get; set; }
            
            [JsonPropertyName("discount")]
            public decimal? Discount { get; set; }
            
            [JsonPropertyName("shippingDiscount")]
            public decimal? ShippingDiscount { get; set; }
        }

        public class ApplyPromotionRequest
        {
            [JsonPropertyName("customerId")]
            public string CustomerId { get; set; } = string.Empty;
            
            [JsonPropertyName("selectedDiscountPromotions")]
            public List<string>? SelectedDiscountPromotions { get; set; }
            
            [JsonPropertyName("selectedFreeshipPromotions")]
            public List<string>? SelectedFreeshipPromotions { get; set; }
            
            [JsonPropertyName("selectedCartDetailIds")]
            public List<string>? SelectedCartDetailIds { get; set; }
            
            [JsonPropertyName("deliveryFee")]
            public decimal? DeliveryFee { get; set; }
        }

        // Model tạm để lưu thông tin đơn hàng chờ thanh toán
        public class PendingOrder
        {
            public string CustomerId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DeliveryAddress { get; set; } = string.Empty;
            public string? PaymentMethod { get; set; }
            public decimal DeliveryFee { get; set; }
            public string? Note { get; set; }
            public List<string> SelectedCartDetailIds { get; set; } = new();
            public decimal TotalAmount { get; set; }
            public List<string>? SelectedDiscountPromotions { get; set; }
            public List<string>? SelectedFreeshipPromotions { get; set; }
            public decimal? Discount { get; set; }
            public decimal? ShippingDiscount { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        // Model tạm để lưu thông tin đơn hàng với TxnRef
        public class PendingOrderWithTxnRef : PendingOrder
        {
            public string TxnRef { get; set; } = string.Empty;
        }
    }
}
