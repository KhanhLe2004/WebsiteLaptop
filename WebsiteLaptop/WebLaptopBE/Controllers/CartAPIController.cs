using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;
using System.Text.RegularExpressions;

namespace WebLaptopBE.Controllers
{
    [Route("api/Cart")]
    [ApiController]
    public class CartAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();

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
                        // Specifications có thể chứa configurationId hoặc thông tin cấu hình
                        // Format: "ConfigurationId:xxx" hoặc JSON
                        if (cd.Specifications.StartsWith("ConfigurationId:"))
                        {
                            configId = cd.Specifications.Substring("ConfigurationId:".Length).Trim();
                            // Lấy giá từ configuration nếu có
                            var config = _db.ProductConfigurations
                                .AsNoTracking()
                                .FirstOrDefault(pc => pc.ConfigurationId == configId);
                            if (config != null && config.Price.HasValue)
                            {
                                itemPrice = (cd.Product?.SellingPrice ?? 0) + config.Price.Value;
                            }
                        }
                    }
                    
                    // Lấy thông tin cấu hình để hiển thị
                    var configInfo = configId != null 
                        ? _db.ProductConfigurations
                            .AsNoTracking()
                            .FirstOrDefault(pc => pc.ConfigurationId == configId)
                        : null;
                    
                    string configDisplay = "";
                    if (configInfo != null)
                    {
                        var parts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(configInfo.Cpu)) parts.Add($"CPU: {configInfo.Cpu}");
                        if (!string.IsNullOrWhiteSpace(configInfo.Ram)) parts.Add($"RAM: {configInfo.Ram}");
                        if (!string.IsNullOrWhiteSpace(configInfo.Rom)) parts.Add($"ROM: {configInfo.Rom}");
                        if (!string.IsNullOrWhiteSpace(configInfo.Card)) parts.Add($"Card: {configInfo.Card}");
                        configDisplay = string.Join(", ", parts);
                    }
                    else if (!string.IsNullOrWhiteSpace(cd.Specifications) && !cd.Specifications.StartsWith("ConfigurationId:"))
                    {
                        configDisplay = cd.Specifications;
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
                    cart = new Cart
                    {
                        CartId = GenerateCartId(),
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

                // Xử lý specifications: nếu có ConfigurationId thì lưu dạng "ConfigurationId:xxx"
                string specifications = request.Specifications ?? "";
                if (!string.IsNullOrWhiteSpace(request.ConfigurationId))
                {
                    specifications = $"ConfigurationId:{request.ConfigurationId}";
                }

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa (cùng productId và cùng configuration)
                var existingItem = cart.CartDetails?.FirstOrDefault(cd => 
                    cd.ProductId == request.ProductId && 
                    cd.Specifications == specifications);
                    
                if (existingItem != null)
                {
                    // Cập nhật số lượng
                    existingItem.Quantity = (existingItem.Quantity ?? 0) + request.Quantity;
                }
                else
                {
                    // Thêm mới
                    var cartDetail = new CartDetail
                    {
                        CartDetailId = GenerateCartDetailId(),
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

                cartDetail.Quantity = request.Quantity;
                
                // Xử lý specifications: nếu có ConfigurationId thì lưu dạng "ConfigurationId:xxx"
                if (!string.IsNullOrWhiteSpace(request.ConfigurationId))
                {
                    cartDetail.Specifications = $"ConfigurationId:{request.ConfigurationId}";
                }
                else if (!string.IsNullOrWhiteSpace(request.Specifications))
                {
                    cartDetail.Specifications = request.Specifications;
                }
                
                // Tính lại giá nếu có configuration
                if (!string.IsNullOrWhiteSpace(cartDetail.Specifications) && 
                    cartDetail.Specifications.StartsWith("ConfigurationId:"))
                {
                    var configId = cartDetail.Specifications.Substring("ConfigurationId:".Length).Trim();
                    var config = _db.ProductConfigurations
                        .AsNoTracking()
                        .FirstOrDefault(pc => pc.ConfigurationId == configId);
                    // Giá sẽ được tính lại khi load cart
                }

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

                var cartDetail = _db.CartDetails.FirstOrDefault(cd => cd.CartDetailId == cartDetailId);
                if (cartDetail == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                _db.CartDetails.Remove(cartDetail);
                _db.SaveChanges();

                return Ok(new { message = "Đã xóa sản phẩm khỏi giỏ hàng" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa sản phẩm khỏi giỏ hàng", error = ex.Message });
            }
        }

        // Helper methods
        private string GenerateCartId()
        {
            var lastCart = _db.Carts.OrderByDescending(c => c.CartId).FirstOrDefault();
            if (lastCart == null)
            {
                return "CART001";
            }

            var match = Regex.Match(lastCart.CartId, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return $"CART{(number + 1):D3}";
            }

            return $"CART{DateTime.Now:yyyyMMddHHmmss}";
        }

        private string GenerateCartDetailId()
        {
            var lastDetail = _db.CartDetails.OrderByDescending(cd => cd.CartDetailId).FirstOrDefault();
            if (lastDetail == null)
            {
                return "CD001";
            }

            var match = Regex.Match(lastDetail.CartDetailId, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return $"CD{(number + 1):D3}";
            }

            return $"CD{DateTime.Now:yyyyMMddHHmmss}";
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
    }
}
