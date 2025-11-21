using System.Globalization;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;

namespace WebLaptopBE.Models.VnPay
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        #region Request
        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var data = new StringBuilder();
            
            // Sắp xếp tham số theo thứ tự alphabet (đã được sắp xếp bởi SortedList với VnPayCompare)
            foreach (var (key, value) in _requestData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                // Encode theo chuẩn VNPay: thay %20 thành + như trong NodeJS demo
                var encodedKey = WebUtility.UrlEncode(key).Replace("%20", "+");
                var encodedValue = WebUtility.UrlEncode(value).Replace("%20", "+");
                data.Append(encodedKey + "=" + encodedValue + "&");
            }

            var querystring = data.ToString();
            
            // Tạo chuỗi để ký (loại bỏ ký tự '&' cuối cùng)
            var signData = querystring;
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            // Tạo chữ ký HMAC SHA512
            var vnpSecureHash = Utils.HmacSHA512(vnpHashSecret, signData);
            
            // Tạo URL cuối cùng
            var finalUrl = baseUrl + "?" + querystring + "vnp_SecureHash=" + vnpSecureHash;

            // Log để debug (chỉ trong development)
            #if DEBUG
            VnPayLogger.LogPaymentRequest(_requestData, signData, vnpSecureHash, finalUrl);
            #endif

            return finalUrl;
        }
        #endregion

        #region Response process
        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rspRaw = GetResponseData();
            var myChecksum = Utils.HmacSHA512(secretKey, rspRaw);
            var isValid = myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);

            // Log để debug (chỉ trong development)
            #if DEBUG
            VnPayLogger.LogPaymentResponse(_responseData, rspRaw, inputHash, myChecksum, isValid);
            if (!isValid)
            {
                VnPayLogger.CompareSignatures(inputHash, myChecksum, "Received", "Calculated");
            }
            #endif

            return isValid;
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            
            // Tạo bản sao để không ảnh hưởng đến dữ liệu gốc
            var responseDataCopy = new SortedList<string, string>(new VnPayCompare());
            foreach (var kvp in _responseData)
            {
                responseDataCopy.Add(kvp.Key, kvp.Value);
            }
            
            // Loại bỏ các tham số không cần thiết cho việc xác thực chữ ký
            if (responseDataCopy.ContainsKey("vnp_SecureHashType"))
            {
                responseDataCopy.Remove("vnp_SecureHashType");
            }

            if (responseDataCopy.ContainsKey("vnp_SecureHash"))
            {
                responseDataCopy.Remove("vnp_SecureHash");
            }

            // Tạo chuỗi query đã được sắp xếp theo thứ tự alphabet
            foreach (var (key, value) in responseDataCopy.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                // Encode theo chuẩn VNPay: thay %20 thành + như trong NodeJS demo
                var encodedKey = WebUtility.UrlEncode(key).Replace("%20", "+");
                var encodedValue = WebUtility.UrlEncode(value).Replace("%20", "+");
                data.Append(encodedKey + "=" + encodedValue + "&");
            }

            // Loại bỏ ký tự '&' cuối cùng
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }
        #endregion

    }

    public class Utils
    {
        public static string HmacSHA512(string key, string inputData)
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


        // Cải thiện để đảm bảo IPv4 theo chuẩn VNPay
        public static string GetIpAddress(HttpContext context)
        {
            try
            {
                // Kiểm tra header X-Forwarded-For trước (cho trường hợp có proxy/load balancer)
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    var firstIp = forwardedFor.Split(',')[0].Trim();
                    if (IsValidIPv4(firstIp))
                    {
                        return firstIp;
                    }
                }

                // Kiểm tra X-Real-IP header
                var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp) && IsValidIPv4(realIp))
                {
                    return realIp;
                }

                // Lấy từ RemoteIpAddress
                var remoteIpAddress = context.Connection.RemoteIpAddress;
                if (remoteIpAddress != null)
                {
                    // Nếu là IPv6, cố gắng chuyển đổi sang IPv4
                    if (remoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // Nếu là IPv6 mapped IPv4 (::ffff:192.168.1.1)
                        if (remoteIpAddress.IsIPv4MappedToIPv6)
                        {
                            return remoteIpAddress.MapToIPv4().ToString();
                        }

                        // Cố gắng resolve sang IPv4
                        try
                        {
                            var hostEntry = Dns.GetHostEntry(remoteIpAddress);
                            var ipv4Address = hostEntry.AddressList
                                .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                            
                            if (ipv4Address != null)
                            {
                                return ipv4Address.ToString();
                            }
                        }
                        catch
                        {
                            // Ignore DNS resolution errors
                        }
                    }
                    else if (remoteIpAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ipString = remoteIpAddress.ToString();
                        if (IsValidIPv4(ipString))
                        {
                            return ipString;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting IP address: {ex.Message}");
            }

            // Fallback to localhost IPv4
            return "127.0.0.1";
        }

        // Helper method để validate IPv4
        private static bool IsValidIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
                return false;

            return IPAddress.TryParse(ipString, out IPAddress address) && 
                   address.AddressFamily == AddressFamily.InterNetwork;
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }


}
