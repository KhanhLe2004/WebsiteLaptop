using System.Globalization;
using System.Net;
using System.Security.Cryptography;
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
            string signData;
            if (querystring.Length > 0 && querystring.EndsWith("&"))
            {
                signData = querystring.Substring(0, querystring.Length - 1);
            }
            else
            {
                signData = querystring;
            }

            // Tạo chữ ký HMAC SHA512
            var vnpSecureHash = Utils.HmacSHA512(vnpHashSecret, signData);
            
            // Tạo URL cuối cùng
            var finalUrl = baseUrl + "?" + querystring + "vnp_SecureHash=" + vnpSecureHash;

            return finalUrl;
        }
        #endregion

        #region Response process
        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rspRaw = GetResponseData();
            var myChecksum = Utils.HmacSHA512(secretKey, rspRaw);
            var isValid = myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);

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
                data.Length--;
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
