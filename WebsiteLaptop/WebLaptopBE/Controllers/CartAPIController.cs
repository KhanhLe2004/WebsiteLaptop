using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;
using System.Text.RegularExpressions;
using WebLaptopBE.Data;
namespace WebLaptopBE.Controllers
{
    [Route("api/Cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private readonly WebLaptopTenTechContext _db = new();

        // GET: api/Cart/{customerId}
        [HttpGet("{customerId}")]
        public IActionResult GetCart(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "CustomerId không hợp lệ" });
                }

                // Tìm hoặc tạo giỏ hàng cho khách hàng
                var cart = _db.Carts
                    .Include(c => c.CartDetails)
                        .ThenInclude(cd => cd.Product)
                            .ThenInclude(p => p.Brand)
                    .FirstOrDefault(c => c.CustomerId == customerId);

                if (cart == null)
                {
                    // Trả về giỏ hàng rỗng nếu chưa có
                    return Ok(new
                    {
                        cartId = (string?)null,
                        customerId = customerId,
                        totalAmount = 0,
                        items = new List<object>()
                    });
                }

                // Kiểm tra CartDetails null hoặc rỗng
                if (cart.CartDetails == null || !cart.CartDetails.Any())
                {
                    return Ok(new
                    {
                        cartId = cart.CartId,
                        customerId = cart.CustomerId,
                        totalAmount = 0,
                        items = new List<object>()
                    });
                }

                // Tính lại tổng tiền
                decimal totalAmount = 0;
                var cartItems = cart.CartDetails.Select(cd => {
                    // Parse specifications để lấy configurationId nếu có
                    string? configId = null;
                    decimal itemPrice = cd.Product?.SellingPrice ?? 0;
                    
                    if (!string.IsNullOrWhiteSpace(cd.Specifications))
                    {
                        // Specifications có thể chứa:
                        // 1. Format cũ: "ConfigurationId:xxx" (backward compatibility)
                        // 2. Format mới: "CPU / RAM / ROM / Card"
                        if (cd.Specifications.StartsWith("ConfigurationId:"))
                        {
                            // Format cũ: lấy ConfigurationId
                            configId = cd.Specifications.Substring("ConfigurationId:".Length).Trim();
                            var config = _db.ProductConfigurations
                                .AsNoTracking()
                                .FirstOrDefault(pc => pc.ConfigurationId == configId);
                            if (config != null && config.Price.HasValue)
                            {
                                itemPrice = (cd.Product?.SellingPrice ?? 0) + config.Price.Value;
                            }
                        }
                        else if (cd.Specifications.Contains(" / "))
                        {
                            // Format mới: tìm configuration dựa trên specifications
                            var specParts = cd.Specifications.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                            if (specParts.Length >= 1 && !string.IsNullOrWhiteSpace(cd.ProductId))
                            {
                                var query = _db.ProductConfigurations
                                    .AsNoTracking()
                                    .Where(pc => pc.ProductId == cd.ProductId);
                                
                                // Match theo từng phần
                                if (specParts.Length >= 1 && !string.IsNullOrWhiteSpace(specParts[0]))
                                    query = query.Where(pc => pc.Cpu == specParts[0].Trim());
                                if (specParts.Length >= 2 && !string.IsNullOrWhiteSpace(specParts[1]))
                                    query = query.Where(pc => pc.Ram == specParts[1].Trim());
                                if (specParts.Length >= 3 && !string.IsNullOrWhiteSpace(specParts[2]))
                                    query = query.Where(pc => pc.Rom == specParts[2].Trim());
                                if (specParts.Length >= 4 && !string.IsNullOrWhiteSpace(specParts[3]))
                                    query = query.Where(pc => pc.Card == specParts[3].Trim());
                                
                                var config = query.FirstOrDefault();
                                if (config != null)
                                {
                                    configId = config.ConfigurationId;
                                    if (config.Price.HasValue)
                                    {
                                        itemPrice = (cd.Product?.SellingPrice ?? 0) + config.Price.Value;
                                    }
                                }
                            }
                        }
                    }
                    
                    // Lấy thông tin cấu hình để hiển thị
                    // Specifications đã được lưu dạng "CPU / RAM / ROM / Card"
                    string configDisplay = cd.Specifications ?? "";
                    
                    // Nếu vẫn còn format cũ "ConfigurationId:xxx", chuyển đổi sang format mới
                    if (!string.IsNullOrWhiteSpace(cd.Specifications) && cd.Specifications.StartsWith("ConfigurationId:"))
                    {
                        var oldConfigId = cd.Specifications.Substring("ConfigurationId:".Length).Trim();
                        var configInfo = _db.ProductConfigurations
                            .AsNoTracking()
                            .FirstOrDefault(pc => pc.ConfigurationId == oldConfigId);
                        
                        if (configInfo != null)
                        {
                            var parts = new List<string>();
                            if (!string.IsNullOrWhiteSpace(configInfo.Cpu)) parts.Add(configInfo.Cpu);
                            if (!string.IsNullOrWhiteSpace(configInfo.Ram)) parts.Add(configInfo.Ram);
                            if (!string.IsNullOrWhiteSpace(configInfo.Rom)) parts.Add(configInfo.Rom);
                            if (!string.IsNullOrWhiteSpace(configInfo.Card)) parts.Add(configInfo.Card);
                            configDisplay = string.Join(" / ", parts);
                            
                            // Cập nhật lại specifications trong database (migration từ format cũ sang mới)
                            cd.Specifications = configDisplay;
                        }
                    }
                    
                    return new
                    {
                        cartDetailId = cd.CartDetailId,
                        productId = cd.ProductId,
                        productName = cd.Product?.ProductName ?? "",
                        productModel = cd.Product?.ProductModel ?? "",
                        productImage = cd.Product?.Avatar ?? "",
                        brandName = cd.Product?.Brand?.BrandName ?? "",
                        basePrice = cd.Product?.SellingPrice ?? 0,
                        price = itemPrice,
                        quantity = cd.Quantity ?? 0,
                        specifications = cd.Specifications ?? "",
                        configurationId = configId,
                        configurationDisplay = configDisplay,
                        subtotal = itemPrice * (cd.Quantity ?? 0)
                    };
                }).ToList();

                totalAmount = cartItems.Sum(item => item.subtotal);

                // Cập nhật tổng tiền
                cart.TotalAmount = totalAmount;
                _db.SaveChanges();

                return Ok(new
                {
                    cartId = cart.CartId,
                    customerId = cart.CustomerId,
                    totalAmount = totalAmount,
                    items = cartItems
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy giỏ hàng", error = ex.Message });
            }
        }

        // POST: api/Cart/add
        [HttpPost("add")]
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.CustomerId) || string.IsNullOrWhiteSpace(request.ProductId))
                {
                    return BadRequest(new { message = "Thông tin không hợp lệ" });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new { message = "Số lượng phải lớn hơn 0" });
                }

                // Kiểm tra sản phẩm tồn tại
                var product = _db.Products.FirstOrDefault(p => p.ProductId == request.ProductId && p.Active == true);
                if (product == null)
                {
                    return NotFound(new { message = "Sản phẩm không tồn tại hoặc đã ngừng bán" });
                }

                // Tìm hoặc tạo giỏ hàng
                var cart = _db.Carts
                    .Include(c => c.CartDetails)
                    .FirstOrDefault(c => c.CustomerId == request.CustomerId);

                if (cart == null)
                {
                    // Tạo ID và kiểm tra trùng
                    string cartId = GenerateCartId();
                    int cartIdAttempts = 0;
                    int cartIdMaxAttempts = 50;
                    while (_db.Carts.Any(c => c.CartId == cartId) && cartIdAttempts < cartIdMaxAttempts)
                    {
                        cartId = GenerateCartId();
                        cartIdAttempts++;
                    }
                    
                    if (cartIdAttempts >= cartIdMaxAttempts)
                    {
                        return StatusCode(500, new { message = "Không thể tạo mã giỏ hàng. Vui lòng thử lại sau." });
                    }
                    
                    cart = new Cart
                    {
                        CartId = cartId,
                        CustomerId = request.CustomerId,
                        TotalAmount = 0
                    };
                    _db.Carts.Add(cart);
                    _db.SaveChanges();
                    
                    // Reload để có CartDetails
                    cart = _db.Carts
                        .Include(c => c.CartDetails)
                        .FirstOrDefault(c => c.CustomerId == request.CustomerId);
                }

                // Đảm bảo CartDetails không null
                if (cart == null)
                {
                    return StatusCode(500, new { message = "Không thể tạo hoặc tìm thấy giỏ hàng" });
                }

                // Xử lý specifications: nếu có ConfigurationId thì lấy thông tin và format "CPU / RAM / ROM / Card"
                string specifications = request.Specifications ?? "";
                ProductConfiguration? config = null;
                if (!string.IsNullOrWhiteSpace(request.ConfigurationId))
                {
                    config = _db.ProductConfigurations
                        .AsNoTracking()
                        .FirstOrDefault(pc => pc.ConfigurationId == request.ConfigurationId);
                    
                    if (config != null)
                    {
                        var parts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(config.Cpu)) parts.Add(config.Cpu);
                        if (!string.IsNullOrWhiteSpace(config.Ram)) parts.Add(config.Ram);
                        if (!string.IsNullOrWhiteSpace(config.Rom)) parts.Add(config.Rom);
                        if (!string.IsNullOrWhiteSpace(config.Card)) parts.Add(config.Card);
                        specifications = string.Join(" / ", parts);
                    }
                    else
                    {
                        specifications = request.Specifications ?? "";
                    }
                }

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa (cùng productId và cùng configuration)
                var existingItem = cart.CartDetails?.FirstOrDefault(cd => 
                    cd.ProductId == request.ProductId && 
                    cd.Specifications == specifications);

                // Kiểm tra số lượng tồn kho nếu có cấu hình
                if (config != null && config.Quantity.HasValue)
                {
                    // Tính số lượng có sẵn (tồn kho - số lượng trong đơn hàng "Chờ xử lý" và "Đang xử lý")
                    int availableQuantity = CalculateAvailableQuantity(request.ProductId, specifications, request.ConfigurationId);
                    
                    // Tính tổng số lượng hiện có trong giỏ hàng (nếu đã có sản phẩm cùng cấu hình)
                    int currentQuantityInCart = existingItem != null ? (existingItem.Quantity ?? 0) : 0;
                    
                    // Kiểm tra tổng số lượng (số lượng hiện có + số lượng muốn thêm) không vượt quá số lượng có sẵn
                    int totalQuantity = currentQuantityInCart + request.Quantity;
                    if (totalQuantity > availableQuantity)
                    {
                        int canAddQuantity = availableQuantity - currentQuantityInCart;
                        if (canAddQuantity <= 0)
                        {
                            return BadRequest(new { 
                                message = $"Sản phẩm này đã hết hàng hoặc đã đạt số lượng tối đa. Số lượng có sẵn: {availableQuantity}.",
                                success = false
                            });
                        }
                        return BadRequest(new { 
                            message = $"Số lượng có sẵn: {availableQuantity}. Bạn đã có {currentQuantityInCart} sản phẩm trong giỏ hàng. Có thể thêm tối đa {canAddQuantity} sản phẩm.",
                            success = false,
                            maxQuantity = availableQuantity,
                            currentInCart = currentQuantityInCart,
                            available = canAddQuantity
                        });
                    }
                }
                    
                if (existingItem != null)
                {
                    // Cập nhật số lượng
                    existingItem.Quantity = (existingItem.Quantity ?? 0) + request.Quantity;
                }
                else
                {
                    // Thêm mới - tạo ID và kiểm tra trùng
                    string cartDetailId = GenerateCartDetailId();
                    int detailIdAttempts = 0;
                    int detailIdMaxAttempts = 50;
                    while (_db.CartDetails.Any(cd => cd.CartDetailId == cartDetailId) && detailIdAttempts < detailIdMaxAttempts)
                    {
                        cartDetailId = GenerateCartDetailId();
                        detailIdAttempts++;
                    }
                    
                    if (detailIdAttempts >= detailIdMaxAttempts)
                    {
                        return StatusCode(500, new { message = "Không thể tạo mã chi tiết giỏ hàng. Vui lòng thử lại sau." });
                    }
                    
                    var cartDetail = new CartDetail
                    {
                        CartDetailId = cartDetailId,
                        CartId = cart.CartId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        Specifications = specifications
                    };
                    _db.CartDetails.Add(cartDetail);
                }

                _db.SaveChanges();

                return Ok(new { 
                    message = "Đã thêm sản phẩm vào giỏ hàng",
                    success = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Lỗi khi thêm sản phẩm vào giỏ hàng", 
                    error = ex.Message,
                    success = false
                });
            }
        }

        // PUT: api/Cart/update
        [HttpPut("update")]
        public IActionResult UpdateCartItem([FromBody] UpdateCartItemRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.CartDetailId))
                {
                    return BadRequest(new { message = "Thông tin không hợp lệ" });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new { message = "Số lượng phải lớn hơn 0" });
                }

                var cartDetail = _db.CartDetails
                    .Include(cd => cd.Product)
                    .FirstOrDefault(cd => cd.CartDetailId == request.CartDetailId);

                if (cartDetail == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                // Xử lý specifications: nếu có ConfigurationId thì lấy thông tin và format "CPU / RAM / ROM / Card"
                ProductConfiguration? config = null;
                if (!string.IsNullOrWhiteSpace(request.ConfigurationId))
                {
                    config = _db.ProductConfigurations
                        .AsNoTracking()
                        .FirstOrDefault(pc => pc.ConfigurationId == request.ConfigurationId);
                    
                    if (config != null)
                    {
                        var parts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(config.Cpu)) parts.Add(config.Cpu);
                        if (!string.IsNullOrWhiteSpace(config.Ram)) parts.Add(config.Ram);
                        if (!string.IsNullOrWhiteSpace(config.Rom)) parts.Add(config.Rom);
                        if (!string.IsNullOrWhiteSpace(config.Card)) parts.Add(config.Card);
                        cartDetail.Specifications = string.Join(" / ", parts);
                    }
                    else if (!string.IsNullOrWhiteSpace(request.Specifications))
                    {
                        cartDetail.Specifications = request.Specifications;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(request.Specifications))
                {
                    cartDetail.Specifications = request.Specifications;
                }
                else if (!string.IsNullOrWhiteSpace(cartDetail.Specifications))
                {
                    // Nếu không có ConfigurationId mới, tìm config từ specifications hiện tại
                    var specParts = cartDetail.Specifications.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                    if (specParts.Length >= 1 && !string.IsNullOrWhiteSpace(cartDetail.ProductId))
                    {
                        var query = _db.ProductConfigurations
                            .AsNoTracking()
                            .Where(pc => pc.ProductId == cartDetail.ProductId);
                        
                        if (specParts.Length >= 1 && !string.IsNullOrWhiteSpace(specParts[0]))
                            query = query.Where(pc => pc.Cpu == specParts[0].Trim());
                        if (specParts.Length >= 2 && !string.IsNullOrWhiteSpace(specParts[1]))
                            query = query.Where(pc => pc.Ram == specParts[1].Trim());
                        if (specParts.Length >= 3 && !string.IsNullOrWhiteSpace(specParts[2]))
                            query = query.Where(pc => pc.Rom == specParts[2].Trim());
                        if (specParts.Length >= 4 && !string.IsNullOrWhiteSpace(specParts[3]))
                            query = query.Where(pc => pc.Card == specParts[3].Trim());
                        
                        config = query.FirstOrDefault();
                    }
                }

                // Kiểm tra số lượng tối đa nếu có cấu hình
                if (config != null && config.Quantity.HasValue)
                {
                    // Tính số lượng có sẵn (tồn kho - số lượng trong đơn hàng "Chờ xử lý" và "Đang xử lý")
                    string specifications = cartDetail.Specifications ?? "";
                    int availableQuantity = CalculateAvailableQuantity(cartDetail.ProductId, specifications, null);
                    
                    if (request.Quantity > availableQuantity)
                    {
                        return BadRequest(new { 
                            message = $"Số lượng có sẵn: {availableQuantity}. Không thể cập nhật số lượng vượt quá giới hạn này.",
                            success = false,
                            maxQuantity = availableQuantity
                        });
                    }
                }

                cartDetail.Quantity = request.Quantity;

                _db.SaveChanges();

                return Ok(new { message = "Đã cập nhật giỏ hàng" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật giỏ hàng", error = ex.Message });
            }
        }

        // DELETE: api/Cart/remove/{cartDetailId}
        [HttpDelete("remove/{cartDetailId}")]
        public IActionResult RemoveFromCart(string cartDetailId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cartDetailId))
                {
                    return BadRequest(new { message = "CartDetailId không hợp lệ" });
                }

                var cartDetail = _db.CartDetails
                    .Include(cd => cd.Cart)
                    .FirstOrDefault(cd => cd.CartDetailId == cartDetailId);
                if (cartDetail == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                // Lưu CartId trước khi xóa CartDetail
                string? cartId = cartDetail.CartId;

                // Xóa CartDetail
                _db.CartDetails.Remove(cartDetail);
                _db.SaveChanges();

                // Kiểm tra xem Cart còn CartDetail nào không
                if (!string.IsNullOrWhiteSpace(cartId))
                {
                    var remainingDetails = _db.CartDetails
                        .Any(cd => cd.CartId == cartId);
                    
                    // Nếu không còn CartDetail nào, xóa luôn Cart
                    if (!remainingDetails)
                    {
                        var cart = _db.Carts.FirstOrDefault(c => c.CartId == cartId);
                        if (cart != null)
                        {
                            _db.Carts.Remove(cart);
                            _db.SaveChanges();
                        }
                    }
                }

                return Ok(new { message = "Đã xóa sản phẩm khỏi giỏ hàng" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa sản phẩm khỏi giỏ hàng", error = ex.Message });
            }
        }

        // GET: api/Cart/available-quantity
        // Lấy số lượng có sẵn cho một sản phẩm với cấu hình cụ thể
        [HttpGet("available-quantity")]
        public IActionResult GetAvailableQuantity([FromQuery] string productId, [FromQuery] string? specifications, [FromQuery] string? configurationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productId))
                {
                    return BadRequest(new { message = "ProductId không được để trống" });
                }

                int availableQuantity = CalculateAvailableQuantity(productId, specifications, configurationId);
                
                return Ok(new
                {
                    productId = productId,
                    specifications = specifications ?? "",
                    configurationId = configurationId ?? "",
                    availableQuantity = availableQuantity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy số lượng có sẵn", error = ex.Message });
            }
        }

        // POST: api/Cart/check-stock
        // Kiểm tra số lượng tồn kho trước khi thanh toán
        [HttpPost("check-stock")]
        public IActionResult CheckStock([FromBody] CheckStockRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.CustomerId))
                {
                    return BadRequest(new { message = "Thông tin không hợp lệ" });
                }

                if (request.CartDetailIds == null || !request.CartDetailIds.Any())
                {
                    return BadRequest(new { message = "Không có sản phẩm nào được chọn" });
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
                var selectedCartDetails = cart.CartDetails
                    .Where(cd => request.CartDetailIds.Contains(cd.CartDetailId))
                    .ToList();

                if (!selectedCartDetails.Any())
                {
                    return BadRequest(new { message = "Không có sản phẩm nào được chọn" });
                }

                var outOfStockItems = new List<object>();

                foreach (var cartDetail in selectedCartDetails)
                {
                    if (cartDetail == null || string.IsNullOrWhiteSpace(cartDetail.ProductId))
                        continue;

                    // Tìm configuration từ specifications
                    ProductConfiguration? config = null;
                    if (!string.IsNullOrWhiteSpace(cartDetail.Specifications))
                    {
                        if (cartDetail.Specifications.StartsWith("ConfigurationId:"))
                        {
                            var configId = cartDetail.Specifications.Substring("ConfigurationId:".Length).Trim();
                            config = _db.ProductConfigurations
                                .AsNoTracking()
                                .FirstOrDefault(pc => pc.ConfigurationId == configId);
                        }
                        else if (cartDetail.Specifications.Contains(" / "))
                        {
                            var specParts = cartDetail.Specifications.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                            if (specParts.Length >= 1)
                            {
                                var query = _db.ProductConfigurations
                                    .AsNoTracking()
                                    .Where(pc => pc.ProductId == cartDetail.ProductId);
                                
                                if (specParts.Length >= 1 && !string.IsNullOrWhiteSpace(specParts[0]))
                                    query = query.Where(pc => pc.Cpu == specParts[0].Trim());
                                if (specParts.Length >= 2 && !string.IsNullOrWhiteSpace(specParts[1]))
                                    query = query.Where(pc => pc.Ram == specParts[1].Trim());
                                if (specParts.Length >= 3 && !string.IsNullOrWhiteSpace(specParts[2]))
                                    query = query.Where(pc => pc.Rom == specParts[2].Trim());
                                if (specParts.Length >= 4 && !string.IsNullOrWhiteSpace(specParts[3]))
                                    query = query.Where(pc => pc.Card == specParts[3].Trim());
                                
                                config = query.FirstOrDefault();
                            }
                        }
                    }

                    // Nếu không có configuration, bỏ qua (không kiểm tra số lượng)
                    if (config == null || !config.Quantity.HasValue)
                        continue;

                    int stockQuantity = config.Quantity.Value;
                    int cartQuantity = cartDetail.Quantity ?? 0;

                    // Tính tổng số lượng đã đặt trong các đơn hàng khác
                    // Chỉ tính các đơn hàng có status: "Chờ xử lý" và "Đang xử lý"
                    var ordersInProgress = _db.SaleInvoices
                        .Where(si => si.Status == "Chờ xử lý" || si.Status == "Đang xử lý")
                        .Select(si => si.SaleInvoiceId)
                        .ToList();

                    int orderedQuantity = 0;
                    if (ordersInProgress.Any())
                    {
                        // Tính tổng số lượng đã đặt cho sản phẩm và cấu hình này
                        // So sánh chính xác specifications
                        var orderDetails = _db.SaleInvoiceDetails
                            .Where(sid => ordersInProgress.Contains(sid.SaleInvoiceId) &&
                                          sid.ProductId == cartDetail.ProductId)
                            .ToList();

                        // So sánh specifications chi tiết
                        string cartSpec = cartDetail.Specifications ?? "";
                        foreach (var orderDetail in orderDetails)
                        {
                            string orderSpec = orderDetail.Specifications ?? "";
                            
                            // So sánh chính xác specifications
                            if (cartSpec == orderSpec)
                            {
                                orderedQuantity += orderDetail.Quantity ?? 0;
                            }
                        }
                    }

                    // Kiểm tra: số lượng trong đơn hàng khác + số lượng trong giỏ hàng > số lượng tồn kho
                    // Nếu bằng nhau thì vẫn cho phép đặt hàng
                    if (orderedQuantity + cartQuantity > stockQuantity)
                    {
                        int availableQuantity = stockQuantity - orderedQuantity;
                        outOfStockItems.Add(new
                        {
                            productId = cartDetail.ProductId,
                            productName = cartDetail.Product?.ProductName ?? "",
                            productModel = cartDetail.Product?.ProductModel ?? "",
                            specifications = cartDetail.Specifications ?? "",
                            cartQuantity = cartQuantity,
                            stockQuantity = stockQuantity,
                            orderedQuantity = orderedQuantity,
                            availableQuantity = availableQuantity,
                            message = availableQuantity < 0 
                                ? $"Sản phẩm {cartDetail.Product?.ProductName} {cartDetail.Product?.ProductModel} (cấu hình: {cartDetail.Specifications}) đã hết hàng. Đã có {orderedQuantity} sản phẩm trong các đơn hàng khác, chỉ còn {stockQuantity} sản phẩm trong kho."
                                : $"Sản phẩm {cartDetail.Product?.ProductName} {cartDetail.Product?.ProductModel} (cấu hình: {cartDetail.Specifications}) chỉ còn {availableQuantity} sản phẩm."
                        });
                    }
                }

                if (outOfStockItems.Any())
                {
                    return BadRequest(new
                    {
                        message = "Sản phẩm không đủ số lượng",
                        outOfStockItems = outOfStockItems,
                        hasError = true
                    });
                }

                return Ok(new
                {
                    message = "Tất cả sản phẩm đều đủ số lượng",
                    hasError = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi kiểm tra số lượng", error = ex.Message });
            }
        }

        // DELETE: api/Cart/{customerId}
        // Xóa toàn bộ giỏ hàng của khách hàng
        [HttpDelete("{customerId}")]
        public IActionResult ClearCart(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "CustomerId không hợp lệ" });
                }

                // Tìm giỏ hàng của khách hàng
                var cart = _db.Carts
                    .Include(c => c.CartDetails)
                    .FirstOrDefault(c => c.CustomerId == customerId);

                if (cart == null)
                {
                    return Ok(new { message = "Giỏ hàng đã trống" });
                }

                // Xóa tất cả CartDetails
                if (cart.CartDetails != null && cart.CartDetails.Any())
                {
                    _db.CartDetails.RemoveRange(cart.CartDetails);
                }

                // Xóa Cart
                _db.Carts.Remove(cart);
                _db.SaveChanges();

                return Ok(new { message = "Đã xóa toàn bộ giỏ hàng" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa giỏ hàng", error = ex.Message });
            }
        }

        // Helper method để tính số lượng có sẵn (tồn kho - số lượng trong đơn hàng "Chờ xử lý" và "Đang xử lý")
        private int CalculateAvailableQuantity(string productId, string? specifications, string? configurationId = null)
        {
            try
            {
                // Tìm configuration
                ProductConfiguration? config = null;
                if (!string.IsNullOrWhiteSpace(configurationId))
                {
                    config = _db.ProductConfigurations
                        .AsNoTracking()
                        .FirstOrDefault(pc => pc.ConfigurationId == configurationId);
                }
                else if (!string.IsNullOrWhiteSpace(specifications))
                {
                    if (specifications.StartsWith("ConfigurationId:"))
                    {
                        var configId = specifications.Substring("ConfigurationId:".Length).Trim();
                        config = _db.ProductConfigurations
                            .AsNoTracking()
                            .FirstOrDefault(pc => pc.ConfigurationId == configId);
                    }
                    else if (specifications.Contains(" / "))
                    {
                        var specParts = specifications.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
                        if (specParts.Length >= 1 && !string.IsNullOrWhiteSpace(productId))
                        {
                            var query = _db.ProductConfigurations
                                .AsNoTracking()
                                .Where(pc => pc.ProductId == productId);
                            
                            if (specParts.Length >= 1 && !string.IsNullOrWhiteSpace(specParts[0]))
                                query = query.Where(pc => pc.Cpu == specParts[0].Trim());
                            if (specParts.Length >= 2 && !string.IsNullOrWhiteSpace(specParts[1]))
                                query = query.Where(pc => pc.Ram == specParts[1].Trim());
                            if (specParts.Length >= 3 && !string.IsNullOrWhiteSpace(specParts[2]))
                                query = query.Where(pc => pc.Rom == specParts[2].Trim());
                            if (specParts.Length >= 4 && !string.IsNullOrWhiteSpace(specParts[3]))
                                query = query.Where(pc => pc.Card == specParts[3].Trim());
                            
                            config = query.FirstOrDefault();
                        }
                    }
                }

                // Nếu không có configuration hoặc không có quantity, trả về 999 (không giới hạn)
                if (config == null || !config.Quantity.HasValue)
                {
                    return 999;
                }

                int stockQuantity = config.Quantity.Value;

                // Tính tổng số lượng đã đặt trong các đơn hàng "Chờ xử lý" và "Đang xử lý"
                var ordersInProgress = _db.SaleInvoices
                    .Where(si => si.Status == "Chờ xử lý" || si.Status == "Đang xử lý")
                    .Select(si => si.SaleInvoiceId)
                    .ToList();

                int orderedQuantity = 0;
                if (ordersInProgress.Any())
                {
                    string spec = specifications ?? "";
                    var orderDetails = _db.SaleInvoiceDetails
                        .Where(sid => ordersInProgress.Contains(sid.SaleInvoiceId) &&
                                      sid.ProductId == productId)
                        .ToList();

                    foreach (var orderDetail in orderDetails)
                    {
                        string orderSpec = orderDetail.Specifications ?? "";
                        if (spec == orderSpec)
                        {
                            orderedQuantity += orderDetail.Quantity ?? 0;
                        }
                    }
                }

                // Số lượng có sẵn = Tồn kho - Số lượng trong đơn hàng
                int availableQuantity = stockQuantity - orderedQuantity;
                return availableQuantity < 0 ? 0 : availableQuantity;
            }
            catch (Exception)
            {
                // Nếu có lỗi, trả về 999 (không giới hạn)
                return 999;
            }
        }

        // Helper methods
        private string GenerateCartId()
        {
            try
            {
                // Lấy tất cả các ID có format CA001, CA002... (chỉ lấy ID có độ dài 5 ký tự)
                var lastCart = _db.Carts
                    .Where(c => c.CartId != null && 
                                c.CartId.StartsWith("CA") && 
                                c.CartId.Length == 5)
                    .OrderByDescending(c => c.CartId)
                    .FirstOrDefault();
                
                if (lastCart == null)
                {
                    return "CA001";
                }

                var match = Regex.Match(lastCart.CartId, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int number))
                {
                    return $"CA{(number + 1):D3}";
                }

                // Nếu không parse được, tìm ID lớn nhất bằng cách so sánh số
                int maxNumber = 0;
                var allCarts = _db.Carts
                    .Where(c => c.CartId != null && 
                                c.CartId.StartsWith("CA") && 
                                c.CartId.Length == 5)
                    .Select(c => c.CartId)
                    .ToList();
                
                foreach (var id in allCarts)
                {
                    var m = Regex.Match(id, @"\d+");
                    if (m.Success && int.TryParse(m.Value, out int num))
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
                }
                
                return $"CA{(maxNumber + 1):D3}";
            }
            catch
            {
                // Fallback: tìm số lớn nhất và tăng lên
                try
                {
                    int maxNumber = 0;
                    var allCarts = _db.Carts
                        .Where(c => c.CartId != null && c.CartId.StartsWith("CA"))
                        .Select(c => c.CartId)
                        .ToList();
                    
                    foreach (var id in allCarts)
                    {
                        var m = Regex.Match(id, @"\d+");
                        if (m.Success && int.TryParse(m.Value, out int num))
                        {
                            maxNumber = Math.Max(maxNumber, num);
                        }
                    }
                    
                    return $"CA{(maxNumber + 1):D3}";
                }
                catch
                {
                    // Nếu vẫn lỗi, trả về CA001
                    return "CA001";
                }
            }
        }

        private string GenerateCartDetailId()
        {
            try
            {
                // Lấy tất cả các ID có format CAD001, CAD002... (chỉ lấy ID có độ dài 6 ký tự)
                var lastDetail = _db.CartDetails
                    .Where(cd => cd.CartDetailId != null && 
                                 cd.CartDetailId.StartsWith("CAD") && 
                                 cd.CartDetailId.Length == 6)
                    .OrderByDescending(cd => cd.CartDetailId)
                    .FirstOrDefault();
                
                if (lastDetail == null)
                {
                    return "CAD001";
                }

                var match = Regex.Match(lastDetail.CartDetailId, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int number))
                {
                    return $"CAD{(number + 1):D3}";
                }

                // Nếu không parse được, tìm ID lớn nhất bằng cách so sánh số
                int maxNumber = 0;
                var allDetails = _db.CartDetails
                    .Where(cd => cd.CartDetailId != null && 
                                 cd.CartDetailId.StartsWith("CAD") && 
                                 cd.CartDetailId.Length == 6)
                    .Select(cd => cd.CartDetailId)
                    .ToList();
                
                foreach (var id in allDetails)
                {
                    var m = Regex.Match(id, @"\d+");
                    if (m.Success && int.TryParse(m.Value, out int num))
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
                }
                
                return $"CAD{(maxNumber + 1):D3}";
            }
            catch
            {
                // Fallback: tìm số lớn nhất và tăng lên
                try
                {
                    int maxNumber = 0;
                    var allDetails = _db.CartDetails
                        .Where(cd => cd.CartDetailId != null && cd.CartDetailId.StartsWith("CAD"))
                        .Select(cd => cd.CartDetailId)
                        .ToList();
                    
                    foreach (var id in allDetails)
                    {
                        var m = Regex.Match(id, @"\d+");
                        if (m.Success && int.TryParse(m.Value, out int num))
                        {
                            maxNumber = Math.Max(maxNumber, num);
                        }
                    }
                    
                    return $"CAD{(maxNumber + 1):D3}";
                }
                catch
                {
                    // Nếu vẫn lỗi, trả về CAD001
                    return "CAD001";
                }
            }
        }

        public class AddToCartRequest
        {
            public string CustomerId { get; set; } = string.Empty;
            public string ProductId { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string? Specifications { get; set; }
            public string? ConfigurationId { get; set; }
        }

        public class UpdateCartItemRequest
        {
            public string CartDetailId { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string? Specifications { get; set; }
            public string? ConfigurationId { get; set; }
        }

        public class CheckStockRequest
        {
            public string CustomerId { get; set; } = string.Empty;
            public List<string> CartDetailIds { get; set; } = new();
        }
    }
}
