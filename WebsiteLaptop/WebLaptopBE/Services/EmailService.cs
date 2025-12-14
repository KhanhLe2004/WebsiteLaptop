using System.Net;
using System.Net.Mail;
using System.Text;

namespace WebLaptopBE.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService()
        {
            // Cấu hình SMTP - có thể lấy từ appsettings.json
            _smtpServer = "smtp.gmail.com"; // Thay đổi theo SMTP server của bạn
            _smtpPort = 587;
            _smtpUsername = "khanhlac299@gmail.com"; // Thay đổi email của bạn
            _smtpPassword = "htfu pkmx jswy awkk"; // Thay đổi mật khẩu ứng dụng
            _fromEmail = "khanhlac299@gmail.com"; // Thay đổi email gửi
            _fromName = "TenTech"; // Tên người gửi
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(
            string toEmail,
            string customerName,
            string orderId,
            string phone,
            string email,
            string address,
            string note,
            List<OrderItem> items,
            decimal subtotal,
            decimal discount,
            decimal deliveryFee,
            decimal totalAmount)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    System.Diagnostics.Debug.WriteLine("Email người nhận không được để trống");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"Đang tạo email cho đơn hàng: {orderId}");
                System.Diagnostics.Debug.WriteLine($"Gửi đến: {toEmail}");

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = $"Thông báo đơn hàng mới #{orderId}",
                    Body = GenerateOrderConfirmationEmailHtml(
                        customerName, orderId, phone, email, address, note,
                        items, subtotal, discount, deliveryFee, totalAmount),
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(toEmail);

                System.Diagnostics.Debug.WriteLine($"Đang kết nối SMTP: {_smtpServer}:{_smtpPort}");
                System.Diagnostics.Debug.WriteLine($"SMTP Username: {_smtpUsername}");
                System.Diagnostics.Debug.WriteLine($"From Email: {_fromEmail}");
                System.Diagnostics.Debug.WriteLine($"To Email: {toEmail}");
                
                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    Timeout = 30000, // 30 giây
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                System.Diagnostics.Debug.WriteLine("Đang gửi email...");
                await smtpClient.SendMailAsync(mailMessage);
                System.Diagnostics.Debug.WriteLine($"Email đã được gửi thành công đến {toEmail}");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                System.Diagnostics.Debug.WriteLine($"SMTP Error: {smtpEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Status Code: {smtpEx.StatusCode}");
                if (smtpEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {smtpEx.InnerException.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        private string GenerateOrderConfirmationEmailHtml(
            string customerName,
            string orderId,
            string phone,
            string email,
            string address,
            string note,
            List<OrderItem> items,
            decimal subtotal,
            decimal discount,
            decimal deliveryFee,
            decimal totalAmount)
        {
            var itemsHtml = new StringBuilder();
            int index = 1;
            foreach (var item in items)
            {
                itemsHtml.Append($@"
                    <tr>
                        <td style=""text-align: center; padding: 10px; border: 1px solid #ddd;"">{index}</td>
                        <td style=""padding: 10px; border: 1px solid #ddd;"">
                            <strong>{WebUtility.HtmlEncode(item.ProductName)}</strong><br>
                            <small style=""color: #666;"">{WebUtility.HtmlEncode(item.Specifications ?? "")}</small>
                        </td>
                        <td style=""text-align: center; padding: 10px; border: 1px solid #ddd;"">{item.Quantity}</td>
                        <td style=""text-align: right; padding: 10px; border: 1px solid #ddd;"">{item.UnitPrice:N0}₫</td>
                        <td style=""text-align: right; padding: 10px; border: 1px solid #ddd;"">{item.Total:N0}₫</td>
                    </tr>");
                index++;
            }

            var maskedPhone = MaskPhone(phone);
            var maskedEmail = MaskEmail(email);

            // Chỉ hiển thị dòng giảm giá khi có khuyến mại
            var discountRow = discount;
                

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác nhận đơn hàng</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
        <!-- Header -->
        <div style=""background-color: #81C408; padding: 20px; text-align: center;"">
            <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">TenTech</h1>
        </div>

        <!-- Order Info -->
        <div style=""padding: 30px 20px; text-align: center;"">
            <h2 style=""color: #333; margin: 0 0 10px 0;"">THÔNG TIN ĐƠN HÀNG</h2>
            <p style=""color: #dc3545; font-size: 20px; font-weight: bold; margin: 0;"">SỐ {orderId}</p>
        </div>

        <!-- Customer Information -->
        <div style=""padding: 0 20px 20px 20px;"">
            <h3 style=""color: #333; border-bottom: 2px solid #81C408; padding-bottom: 10px;"">1. Thông tin người đặt hàng</h3>
            <table style=""width: 100%; border-collapse: collapse;"">
                <tr>
                    <td style=""padding: 8px 0; width: 150px; color: #666;""><strong>Họ tên:</strong></td>
                    <td style=""padding: 8px 0; color: #333;"">{WebUtility.HtmlEncode(customerName)}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666;""><strong>Điện thoại:</strong></td>
                    <td style=""padding: 8px 0; color: #333;"">{maskedPhone}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666;""><strong>Email:</strong></td>
                    <td style=""padding: 8px 0; color: #333;"">{maskedEmail}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666;""><strong>Địa chỉ:</strong></td>
                    <td style=""padding: 8px 0; color: #333;"">{WebUtility.HtmlEncode(address ?? "")}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666;""><strong>Ghi chú đặt hàng:</strong></td>
                    <td style=""padding: 8px 0; color: #333;"">{WebUtility.HtmlEncode(note ?? "")}</td>
                </tr>
            </table>
        </div>

        <!-- Products -->
        <div style=""padding: 0 20px 20px 20px;"">
            <h3 style=""color: #333; border-bottom: 2px solid #81C408; padding-bottom: 10px;"">2. Sản phẩm đặt hàng</h3>
            <table style=""width: 100%; border-collapse: collapse; border: 1px solid #ddd;"">
                <thead>
                    <tr style=""background-color: #f8f9fa;"">
                        <th style=""padding: 10px; border: 1px solid #ddd; text-align: center;"">#</th>
                        <th style=""padding: 10px; border: 1px solid #ddd;"">Tên sản phẩm</th>
                        <th style=""padding: 10px; border: 1px solid #ddd; text-align: center;"">SL</th>
                        <th style=""padding: 10px; border: 1px solid #ddd; text-align: right;"">Giá tiền</th>
                        <th style=""padding: 10px; border: 1px solid #ddd; text-align: right;"">Tổng (SLxG)</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
        </div>

        <!-- Summary -->
        <div style=""padding: 0 20px 20px 20px;"">
            <table style=""width: 100%; border-collapse: collapse;"">
                <tr>
                    <td style=""padding: 8px 0; text-align: right; color: #666;""><strong>Tạm tính:</strong></td>
                    <td style=""padding: 8px 0; text-align: right; width: 150px; color: #333;"">{subtotal:N0}₫</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; text-align: right; color: #666;""><strong>Phí giao hàng:</strong></td>
                    <td style=""padding: 8px 0; text-align: right; color: #333;"">+{deliveryFee:N0}₫</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; text-align: right; color: #666;""><strong>Giảm giá:</strong></td>
                    <td style=""padding: 8px 0; text-align: right; color: #dc3545;"">-{discount:N0}₫</td>
                </tr>
                <tr style=""border-top: 2px solid #81C408;"">
                    <td style=""padding: 12px 0; text-align: right; color: #333; font-size: 18px;""><strong>Tổng tiền thanh toán:</strong></td>
                    <td style=""padding: 12px 0; text-align: right; color: #dc3545; font-size: 18px; font-weight: bold;"">{totalAmount:N0}₫</td>
                </tr>
            </table>
        </div>

        <!-- Message -->
        <div style=""padding: 20px; background-color: #f8f9fa; text-align: center;"">
            <p style=""color: #333; margin: 0 0 10px 0;"">Cám ơn bạn đã đặt hàng. Đơn hàng đang được tiếp nhận và đang chờ xử lý.</p>
        </div>

        <!-- Footer -->
        <div style=""background-color: #81C408; padding: 20px; color: #ffffff; text-align: center;"">
            <p style=""margin: 0; font-size: 14px;"">Truy cập Website để xem khuyến mại mới nhất</p>
        </div>
    </div>
</body>
</html>";
        }

        private string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
                return phone;
            
            // Format: 03xxxx5282 (2 số đầu + xxxx + 2 số cuối)
            var length = phone.Length;
            if (length <= 4)
                return phone;
            
            var visible = phone.Substring(0, 2);
            var masked = new string('x', Math.Max(4, length - 4));
            var last = phone.Substring(Math.Max(2, length - 2));
            
            return $"{visible}{masked}{last}";
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return email;
            
            var parts = email.Split('@');
            var localPart = parts[0];
            var domain = parts[1];
            
            if (localPart.Length <= 1)
                return $"{localPart[0]}*@{domain}";
            
            // Format: ng***********4@gmail.com (1 ký tự đầu + * + 1 ký tự cuối)
            var visible = localPart.Substring(0, 1);
            var masked = new string('*', Math.Max(11, localPart.Length - 2));
            var last = localPart.Length > 1 ? localPart.Substring(localPart.Length - 1) : "";
            
            return $"{visible}{masked}{last}@{domain}";
        }
    }

    public class OrderItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Specifications { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}

