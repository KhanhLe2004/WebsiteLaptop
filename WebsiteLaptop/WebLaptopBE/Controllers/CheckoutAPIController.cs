using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;
using System.Text.RegularExpressions;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();

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

                if (cart == null || !cart.CartDetails.Any())
                {
                    return BadRequest(new { message = "Giỏ hàng trống" });
                }

                // Validate thông tin giao hàng
                if (string.IsNullOrWhiteSpace(request.FullName) ||
                    string.IsNullOrWhiteSpace(request.Phone) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.DeliveryAddress))
                {
                    return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin giao hàng" });
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

                // Tính tổng tiền
                decimal subtotal = 0;
                foreach (var item in cart.CartDetails)
                {
                    var price = item.Product?.SellingPrice ?? 0;
                    var quantity = item.Quantity ?? 0;
                    subtotal += price * quantity;
                }

                decimal deliveryFee = request.DeliveryFee ?? 0;
                decimal totalAmount = subtotal + deliveryFee;

                // Tạo đơn hàng
                var saleInvoice = new SaleInvoice
                {
                    SaleInvoiceId = GenerateSaleInvoiceId(),
                    CustomerId = request.CustomerId,
                    PaymentMethod = request.PaymentMethod ?? "COD",
                    DeliveryAddress = request.DeliveryAddress,
                    DeliveryFee = deliveryFee,
                    TotalAmount = totalAmount,
                    Status = "Đang xử lý",
                    TimeCreate = DateTime.Now
                };

                _db.SaleInvoices.Add(saleInvoice);

                // Tạo chi tiết đơn hàng
                foreach (var cartItem in cart.CartDetails)
                {
                    var price = cartItem.Product?.SellingPrice ?? 0;
                    var saleInvoiceDetail = new SaleInvoiceDetail
                    {
                        SaleInvoiceDetailId = GenerateSaleInvoiceDetailId(),
                        SaleInvoiceId = saleInvoice.SaleInvoiceId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = price,
                        Specifications = cartItem.Specifications
                    };
                    _db.SaleInvoiceDetails.Add(saleInvoiceDetail);
                }

                // Xóa giỏ hàng sau khi tạo đơn hàng
                _db.CartDetails.RemoveRange(cart.CartDetails);
                _db.Carts.Remove(cart);

                _db.SaveChanges();

                return Ok(new
                {
                    message = "Đặt hàng thành công",
                    orderId = saleInvoice.SaleInvoiceId,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo đơn hàng", error = ex.Message });
            }
        }

        // Helper methods
        private string GenerateSaleInvoiceId()
        {
            var lastInvoice = _db.SaleInvoices.OrderByDescending(si => si.SaleInvoiceId).FirstOrDefault();
            if (lastInvoice == null)
            {
                return "INV001";
            }

            var match = Regex.Match(lastInvoice.SaleInvoiceId, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return $"INV{(number + 1):D3}";
            }

            return $"INV{DateTime.Now:yyyyMMddHHmmss}";
        }

        private string GenerateSaleInvoiceDetailId()
        {
            var lastDetail = _db.SaleInvoiceDetails.OrderByDescending(sid => sid.SaleInvoiceDetailId).FirstOrDefault();
            if (lastDetail == null)
            {
                return "SID001";
            }

            var match = Regex.Match(lastDetail.SaleInvoiceDetailId, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return $"SID{(number + 1):D3}";
            }

            return $"SID{DateTime.Now:yyyyMMddHHmmss}";
        }

        public class CheckoutRequest
        {
            public string CustomerId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DeliveryAddress { get; set; } = string.Empty;
            public string? PaymentMethod { get; set; }
            public decimal? DeliveryFee { get; set; }
            public string? Note { get; set; }
        }
    }
}
