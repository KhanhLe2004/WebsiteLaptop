using System.Text;
using System.Security.Cryptography;

namespace WebLaptopBE.Models.VnPay
{
    /// <summary>
    /// Helper class cho VNPay với các phương thức tiện ích
    /// </summary>
    public static class VnPayHelper
    {
        /// <summary>
        /// Tạo chữ ký HMAC SHA512 theo chuẩn VNPay
        /// </summary>
        /// <param name="key">Secret key</param>
        /// <param name="inputData">Dữ liệu cần ký</param>
        /// <returns>Chữ ký hex string</returns>
        public static string CreateSignature(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        /// <summary>
        /// Xác thực chữ ký VNPay
        /// </summary>
        /// <param name="inputHash">Chữ ký từ VNPay</param>
        /// <param name="secretKey">Secret key</param>
        /// <param name="queryString">Query string để xác thực</param>
        /// <returns>True nếu chữ ký hợp lệ</returns>
        public static bool ValidateSignature(string inputHash, string secretKey, string queryString)
        {
            var myChecksum = CreateSignature(secretKey, queryString);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Tạo mã giao dịch unique
        /// </summary>
        /// <param name="orderId">ID đơn hàng</param>
        /// <returns>Mã giao dịch unique</returns>
        public static string GenerateTransactionRef(int orderId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"{orderId}_{timestamp}";
        }

        /// <summary>
        /// URL Encode theo chuẩn VNPay (thay %20 thành +)
        /// </summary>
        /// <param name="value">Giá trị cần encode</param>
        /// <returns>Giá trị đã encode</returns>
        public static string VnPayUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
                
            return System.Web.HttpUtility.UrlEncode(value, System.Text.Encoding.UTF8)
                .Replace("%20", "+");
        }

        /// <summary>
        /// Tạo query string theo chuẩn VNPay
        /// </summary>
        /// <param name="parameters">Dictionary tham số</param>
        /// <returns>Query string đã encode</returns>
        public static string BuildQueryString(SortedDictionary<string, string> parameters)
        {
            var queryParts = new List<string>();
            
            foreach (var kvp in parameters.Where(p => !string.IsNullOrEmpty(p.Value)))
            {
                var encodedKey = VnPayUrlEncode(kvp.Key);
                var encodedValue = VnPayUrlEncode(kvp.Value);
                queryParts.Add($"{encodedKey}={encodedValue}");
            }
            
            return string.Join("&", queryParts);
        }

        /// <summary>
        /// Parse OrderId từ TxnRef
        /// </summary>
        /// <param name="txnRef">Transaction reference từ VNPay</param>
        /// <returns>OrderId</returns>
        public static int ParseOrderIdFromTxnRef(string txnRef)
        {
            if (string.IsNullOrEmpty(txnRef))
                return 0;

            var parts = txnRef.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[0], out int orderId))
            {
                return orderId;
            }

            return 0;
        }

        /// <summary>
        /// Lấy thông báo lỗi VNPay theo mã
        /// </summary>
        /// <param name="responseCode">Mã phản hồi từ VNPay</param>
        /// <returns>Thông báo lỗi</returns>
        public static string GetResponseMessage(string responseCode)
        {
            return responseCode switch
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

        /// <summary>
        /// Kiểm tra xem response code có thành công không
        /// </summary>
        /// <param name="responseCode">Mã phản hồi từ VNPay</param>
        /// <returns>True nếu thành công</returns>
        public static bool IsSuccessResponse(string responseCode)
        {
            return responseCode == "00";
        }

        /// <summary>
        /// Format số tiền theo chuẩn VNPay (nhân 100, không có dấu thập phân)
        /// </summary>
        /// <param name="amount">Số tiền VND</param>
        /// <returns>Số tiền đã format</returns>
        public static long FormatAmount(decimal amount)
        {
            return (long)(amount * 100);
        }

        /// <summary>
        /// Parse số tiền từ VNPay về decimal
        /// </summary>
        /// <param name="vnpAmount">Số tiền từ VNPay</param>
        /// <returns>Số tiền decimal</returns>
        public static decimal ParseAmount(string vnpAmount)
        {
            if (long.TryParse(vnpAmount, out long amount))
            {
                return amount / 100m;
            }
            return 0;
        }
    }
}
