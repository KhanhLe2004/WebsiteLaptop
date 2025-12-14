# Giải Thích Chi Tiết Về Chữ Ký VNPay (Signature)

## 1. Chữ Ký Là Gì?

**Chữ ký (Signature)** trong VNPay là một chuỗi mã hóa được tạo từ dữ liệu giao dịch và một khóa bí mật (HashSecret). Nó đóng vai trò như một "con dấu điện tử" để:

- ✅ **Xác thực nguồn gốc**: Đảm bảo dữ liệu thực sự đến từ VNPay (không phải giả mạo)
- ✅ **Đảm bảo tính toàn vẹn**: Đảm bảo dữ liệu không bị thay đổi trên đường truyền
- ✅ **Chống tấn công**: Ngăn chặn các cuộc tấn công man-in-the-middle, replay attack

---

## 2. Thuật Toán: HMAC-SHA512

VNPay sử dụng thuật toán **HMAC-SHA512** để tạo chữ ký:

- **HMAC** (Hash-based Message Authentication Code): Thuật toán tạo mã xác thực dựa trên hash
- **SHA512**: Hàm băm tạo ra chuỗi 512-bit (128 ký tự hex)

### Cách Hoạt Động:

```
Chữ ký = HMAC-SHA512(HashSecret, Dữ liệu cần ký)
```

**Ví dụ:**
```csharp
HashSecret = "T47ZU2IYO4I38U1GNLGKRQLH0W8B40JI"
Dữ liệu = "vnp_Amount=10000000&vnp_Command=pay&vnp_CreateDate=20240101120000&..."
Chữ ký = "a1b2c3d4e5f6..." (128 ký tự hex)
```

---

## 3. Quy Trình Tạo Chữ Ký Cho REQUEST (Khi Gửi Đến VNPay)

### Bước 1: Thu Thập Tất Cả Tham Số

Backend thu thập các tham số cần gửi đến VNPay:

```csharp
vnpay.AddRequestData("vnp_Version", "2.1.0");
vnpay.AddRequestData("vnp_Command", "pay");
vnpay.AddRequestData("vnp_TmnCode", "XPN3KK8O");
vnpay.AddRequestData("vnp_Amount", "10000000");  // 100,000 VND × 100
vnpay.AddRequestData("vnp_CreateDate", "20240101120000");
vnpay.AddRequestData("vnp_CurrCode", "VND");
vnpay.AddRequestData("vnp_IpAddr", "192.168.1.1");
vnpay.AddRequestData("vnp_Locale", "vn");
vnpay.AddRequestData("vnp_OrderInfo", "Thanh toán cho đơn hàng:123456");
vnpay.AddRequestData("vnp_OrderType", "other");
vnpay.AddRequestData("vnp_ReturnUrl", "http://localhost:5068/api/Checkout/vnpay-callback");
vnpay.AddRequestData("vnp_TxnRef", "123456_001");
```

**Lưu ý:** Các tham số được lưu trong `SortedList` với `VnPayCompare`, tự động sắp xếp theo thứ tự alphabet.

### Bước 2: Sắp Xếp Theo Thứ Tự Alphabet

Các tham số được sắp xếp tự động bởi `SortedList`:

```
vnp_Amount
vnp_Command
vnp_CreateDate
vnp_CurrCode
vnp_IpAddr
vnp_Locale
vnp_OrderInfo
vnp_OrderType
vnp_ReturnUrl
vnp_TmnCode
vnp_TxnRef
vnp_Version
```

### Bước 3: URL Encode và Tạo Query String

Mỗi key và value được URL encode (theo chuẩn VNPay: thay `%20` thành `+`):

```csharp
var encodedKey = WebUtility.UrlEncode(key).Replace("%20", "+");
var encodedValue = WebUtility.UrlEncode(value).Replace("%20", "+");
data.Append(encodedKey + "=" + encodedValue + "&");
```

**Kết quả:**
```
vnp_Amount=10000000&
vnp_Command=pay&
vnp_CreateDate=20240101120000&
vnp_CurrCode=VND&
vnp_IpAddr=192.168.1.1&
vnp_Locale=vn&
vnp_OrderInfo=Thanh+toan+cho+don+hang%3A123456&
vnp_OrderType=other&
vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5068%2Fapi%2FCheckout%2Fvnpay-callback&
vnp_TmnCode=XPN3KK8O&
vnp_TxnRef=123456_001&
vnp_Version=2.1.0&
```

### Bước 4: Loại Bỏ Ký Tự '&' Cuối Cùng

```csharp
string signData = querystring.Substring(0, querystring.Length - 1);
```

**Chuỗi để ký (signData):**
```
vnp_Amount=10000000&vnp_Command=pay&vnp_CreateDate=20240101120000&vnp_CurrCode=VND&vnp_IpAddr=192.168.1.1&vnp_Locale=vn&vnp_OrderInfo=Thanh+toan+cho+don+hang%3A123456&vnp_OrderType=other&vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5068%2Fapi%2FCheckout%2Fvnpay-callback&vnp_TmnCode=XPN3KK8O&vnp_TxnRef=123456_001&vnp_Version=2.1.0
```

### Bước 5: Tạo Chữ Ký HMAC-SHA512

```csharp
var vnpSecureHash = Utils.HmacSHA512(vnpHashSecret, signData);
```

**Quy trình bên trong:**

1. **Chuyển đổi sang bytes:**
   ```csharp
   var keyBytes = Encoding.UTF8.GetBytes("T47ZU2IYO4I38U1GNLGKRQLH0W8B40JI");
   var inputBytes = Encoding.UTF8.GetBytes(signData);
   ```

2. **Tính HMAC-SHA512:**
   ```csharp
   using (var hmac = new HMACSHA512(keyBytes))
   {
       var hashValue = hmac.ComputeHash(inputBytes);
   }
   ```

3. **Chuyển đổi sang hex string:**
   ```csharp
   foreach (var theByte in hashValue)
   {
       hash.Append(theByte.ToString("x2")); // x2 = hex 2 chữ số
   }
   ```

**Kết quả chữ ký (ví dụ):**
```
a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890
```
(128 ký tự hex)

### Bước 6: Thêm Chữ Ký Vào URL

```csharp
var finalUrl = baseUrl + "?" + querystring + "vnp_SecureHash=" + vnpSecureHash;
```

**URL cuối cùng:**
```
https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?
  vnp_Amount=10000000&
  vnp_Command=pay&
  vnp_CreateDate=20240101120000&
  vnp_CurrCode=VND&
  vnp_IpAddr=192.168.1.1&
  vnp_Locale=vn&
  vnp_OrderInfo=Thanh+toan+cho+don+hang%3A123456&
  vnp_OrderType=other&
  vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5068%2Fapi%2FCheckout%2Fvnpay-callback&
  vnp_TmnCode=XPN3KK8O&
  vnp_TxnRef=123456_001&
  vnp_Version=2.1.0&
  vnp_SecureHash=a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890
```

---

## 4. Quy Trình Xác Thực Chữ Ký Cho RESPONSE (Khi VNPay Callback)

### Bước 1: Nhận Callback Từ VNPay

VNPay gửi callback về với query string:

```
GET /api/Checkout/vnpay-callback?
  vnp_Amount=10000000&
  vnp_BankCode=NCB&
  vnp_CardType=ATM&
  vnp_OrderInfo=Thanh+toan+cho+don+hang%3A123456&
  vnp_PayDate=20240101120530&
  vnp_ResponseCode=00&
  vnp_TmnCode=XPN3KK8O&
  vnp_TransactionNo=12345678&
  vnp_TransactionStatus=00&
  vnp_TxnRef=123456_001&
  vnp_SecureHash=xyz789...&
  vnp_SecureHashType=SHA512
```

### Bước 2: Thu Thập Tất Cả Tham Số (Bắt Đầu Bằng `vnp_`)

```csharp
foreach (var (key, value) in collections)
{
    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
    {
        vnpay.AddResponseData(key, value.ToString());
    }
}
```

### Bước 3: Loại Bỏ Các Tham Số Không Cần Thiết

**QUAN TRỌNG:** Phải loại bỏ `vnp_SecureHash` và `vnp_SecureHashType` trước khi tính lại chữ ký:

```csharp
if (responseDataCopy.ContainsKey("vnp_SecureHashType"))
{
    responseDataCopy.Remove("vnp_SecureHashType");
}

if (responseDataCopy.ContainsKey("vnp_SecureHash"))
{
    responseDataCopy.Remove("vnp_SecureHash");
}
```

**Lý do:** Chữ ký không thể tự ký chính nó!

### Bước 4: Sắp Xếp và Tạo Query String (Giống Như Request)

Các tham số còn lại được sắp xếp theo alphabet và tạo query string:

```
vnp_Amount=10000000&
vnp_BankCode=NCB&
vnp_CardType=ATM&
vnp_OrderInfo=Thanh+toan+cho+don+hang%3A123456&
vnp_PayDate=20240101120530&
vnp_ResponseCode=00&
vnp_TmnCode=XPN3KK8O&
vnp_TransactionNo=12345678&
vnp_TransactionStatus=00&
vnp_TxnRef=123456_001
```

**Chuỗi để xác thực:**
```
vnp_Amount=10000000&vnp_BankCode=NCB&vnp_CardType=ATM&vnp_OrderInfo=Thanh+toan+cho+don+hang%3A123456&vnp_PayDate=20240101120530&vnp_ResponseCode=00&vnp_TmnCode=XPN3KK8O&vnp_TransactionNo=12345678&vnp_TransactionStatus=00&vnp_TxnRef=123456_001
```

### Bước 5: Tính Lại Chữ Ký

```csharp
var myChecksum = Utils.HmacSHA512(secretKey, rspRaw);
```

Sử dụng cùng `HashSecret` và cùng thuật toán HMAC-SHA512.

### Bước 6: So Sánh Chữ Ký

```csharp
var isValid = myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
```

- **Nếu giống nhau** → ✅ Chữ ký hợp lệ → Dữ liệu đáng tin cậy
- **Nếu khác nhau** → ❌ Chữ ký không hợp lệ → Từ chối giao dịch

---

## 5. Ví Dụ Cụ Thể

### Ví Dụ 1: Tạo Chữ Ký Request

**Input:**
```
HashSecret = "T47ZU2IYO4I38U1GNLGKRQLH0W8B40JI"
vnp_Amount = "10000000"
vnp_Command = "pay"
vnp_TmnCode = "XPN3KK8O"
vnp_TxnRef = "123456_001"
```

**Bước 1-4: Tạo Query String**
```
vnp_Amount=10000000&vnp_Command=pay&vnp_TmnCode=XPN3KK8O&vnp_TxnRef=123456_001
```

**Bước 5: Tính HMAC-SHA512**
```
Chữ ký = HMAC-SHA512("T47ZU2IYO4I38U1GNLGKRQLH0W8B40JI", 
                     "vnp_Amount=10000000&vnp_Command=pay&vnp_TmnCode=XPN3KK8O&vnp_TxnRef=123456_001")
```

**Kết quả (ví dụ):**
```
a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890
```

### Ví Dụ 2: Xác Thực Chữ Ký Response

**Input từ VNPay:**
```
vnp_Amount=10000000
vnp_ResponseCode=00
vnp_TxnRef=123456_001
vnp_SecureHash=xyz789... (từ VNPay)
```

**Bước 1-3: Loại bỏ vnp_SecureHash**
```
Còn lại: vnp_Amount=10000000, vnp_ResponseCode=00, vnp_TxnRef=123456_001
```

**Bước 4: Tạo Query String**
```
vnp_Amount=10000000&vnp_ResponseCode=00&vnp_TxnRef=123456_001
```

**Bước 5: Tính lại chữ ký**
```
myChecksum = HMAC-SHA512("T47ZU2IYO4I38U1GNLGKRQLH0W8B40JI",
                         "vnp_Amount=10000000&vnp_ResponseCode=00&vnp_TxnRef=123456_001")
```

**Bước 6: So sánh**
```
myChecksum == "xyz789..." ? ✅ Hợp lệ : ❌ Không hợp lệ
```

---

## 6. Tại Sao Phải Sắp Xếp Theo Alphabet?

**Lý do:** Đảm bảo tính nhất quán giữa Backend và VNPay.

- Nếu không sắp xếp, cùng một bộ dữ liệu có thể tạo ra nhiều query string khác nhau:
  ```
  vnp_Amount=100&vnp_Command=pay  ✅
  vnp_Command=pay&vnp_Amount=100  ❌ (khác chữ ký!)
  ```

- Với sắp xếp alphabet, luôn có một thứ tự duy nhất:
  ```
  vnp_Amount=100&vnp_Command=pay  ✅ (luôn luôn)
  ```

---

## 7. Tại Sao Phải Loại Bỏ vnp_SecureHash?

**Lý do:** Chữ ký không thể tự ký chính nó.

Nếu không loại bỏ `vnp_SecureHash`:
```
Chuỗi để ký = "...&vnp_SecureHash=abc123"
Chữ ký tính được = HMAC-SHA512(secret, "...&vnp_SecureHash=abc123") = "xyz789"
```

Nhưng `vnp_SecureHash` từ VNPay = "xyz789", nên:
```
Chuỗi để ký = "...&vnp_SecureHash=xyz789"  ← Thay đổi!
Chữ ký tính lại = HMAC-SHA512(secret, "...&vnp_SecureHash=xyz789") = "def456"  ← Khác!
```

→ **Vòng lặp vô tận!** Không thể xác thực được.

**Giải pháp:** Loại bỏ `vnp_SecureHash` trước khi tính chữ ký.

---

## 8. Code Implementation Chi Tiết

### Tạo Chữ Ký (Request)

```csharp
public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
{
    var data = new StringBuilder();
    
    // Bước 1-2: Sắp xếp tự động bởi SortedList
    foreach (var (key, value) in _requestData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
    {
        // Bước 3: URL encode
        var encodedKey = WebUtility.UrlEncode(key).Replace("%20", "+");
        var encodedValue = WebUtility.UrlEncode(value).Replace("%20", "+");
        data.Append(encodedKey + "=" + encodedValue + "&");
    }

    var querystring = data.ToString();
    
    // Bước 4: Loại bỏ '&' cuối
    string signData = querystring.EndsWith("&") 
        ? querystring.Substring(0, querystring.Length - 1) 
        : querystring;

    // Bước 5: Tạo chữ ký
    var vnpSecureHash = Utils.HmacSHA512(vnpHashSecret, signData);
    
    // Bước 6: Thêm vào URL
    return baseUrl + "?" + querystring + "vnp_SecureHash=" + vnpSecureHash;
}
```

### Xác Thực Chữ Ký (Response)

```csharp
public bool ValidateSignature(string inputHash, string secretKey)
{
    // Bước 1-3: Lấy dữ liệu (đã loại bỏ vnp_SecureHash)
    var rspRaw = GetResponseData();
    
    // Bước 5: Tính lại chữ ký
    var myChecksum = Utils.HmacSHA512(secretKey, rspRaw);
    
    // Bước 6: So sánh
    return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
}

private string GetResponseData()
{
    var data = new StringBuilder();
    var responseDataCopy = new SortedList<string, string>(new VnPayCompare());
    
    // Copy dữ liệu
    foreach (var kvp in _responseData)
    {
        responseDataCopy.Add(kvp.Key, kvp.Value);
    }
    
    // Loại bỏ vnp_SecureHash và vnp_SecureHashType
    responseDataCopy.Remove("vnp_SecureHashType");
    responseDataCopy.Remove("vnp_SecureHash");
    
    // Tạo query string
    foreach (var (key, value) in responseDataCopy.Where(kv => !string.IsNullOrEmpty(kv.Value)))
    {
        var encodedKey = WebUtility.UrlEncode(key).Replace("%20", "+");
        var encodedValue = WebUtility.UrlEncode(value).Replace("%20", "+");
        data.Append(encodedKey + "=" + encodedValue + "&");
    }
    
    // Loại bỏ '&' cuối
    if (data.Length > 0) data.Length--;
    
    return data.ToString();
}
```

### HMAC-SHA512 Implementation

```csharp
public static string HmacSHA512(string key, string inputData)
{
    var hash = new StringBuilder();
    
    // Chuyển đổi sang bytes
    var keyBytes = Encoding.UTF8.GetBytes(key);
    var inputBytes = Encoding.UTF8.GetBytes(inputData);
    
    // Tính HMAC-SHA512
    using (var hmac = new HMACSHA512(keyBytes))
    {
        var hashValue = hmac.ComputeHash(inputBytes);
        
        // Chuyển đổi sang hex string
        foreach (var theByte in hashValue)
        {
            hash.Append(theByte.ToString("x2")); // x2 = hex 2 chữ số (00-ff)
        }
    }
    
    return hash.ToString(); // 128 ký tự hex
}
```

---

## 9. Các Lỗi Thường Gặp

### ❌ Lỗi 1: Không Sắp Xếp Theo Alphabet

**Sai:**
```csharp
// Thêm tham số theo thứ tự bất kỳ
data.Append("vnp_Command=pay&");
data.Append("vnp_Amount=10000000&");
```

**Đúng:**
```csharp
// Sử dụng SortedList để tự động sắp xếp
var sortedList = new SortedList<string, string>(new VnPayCompare());
sortedList.Add("vnp_Command", "pay");
sortedList.Add("vnp_Amount", "10000000");
```

### ❌ Lỗi 2: Không Loại Bỏ vnp_SecureHash

**Sai:**
```csharp
// Tính chữ ký với vnp_SecureHash
var signData = "...&vnp_SecureHash=abc123";
```

**Đúng:**
```csharp
// Loại bỏ vnp_SecureHash trước
responseDataCopy.Remove("vnp_SecureHash");
var signData = "..."; // Không có vnp_SecureHash
```

### ❌ Lỗi 3: URL Encode Không Đúng

**Sai:**
```csharp
var encoded = Uri.EscapeDataString(value); // Không thay %20 thành +
```

**Đúng:**
```csharp
var encoded = WebUtility.UrlEncode(value).Replace("%20", "+"); // Theo chuẩn VNPay
```

### ❌ Lỗi 4: So Sánh Phân Biệt Hoa Thường

**Sai:**
```csharp
if (myChecksum == inputHash) // Phân biệt hoa thường
```

**Đúng:**
```csharp
if (myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase)) // Không phân biệt
```

---

## 10. Tóm Tắt

### Quy Trình Tạo Chữ Ký (Request):
1. ✅ Thu thập tất cả tham số
2. ✅ Sắp xếp theo alphabet
3. ✅ URL encode (thay %20 thành +)
4. ✅ Tạo query string (loại bỏ & cuối)
5. ✅ Tính HMAC-SHA512(HashSecret, queryString)
6. ✅ Thêm vnp_SecureHash vào URL

### Quy Trình Xác Thực Chữ Ký (Response):
1. ✅ Thu thập tất cả tham số (bắt đầu bằng vnp_)
2. ✅ Loại bỏ vnp_SecureHash và vnp_SecureHashType
3. ✅ Sắp xếp theo alphabet
4. ✅ URL encode (thay %20 thành +)
5. ✅ Tạo query string (loại bỏ & cuối)
6. ✅ Tính lại HMAC-SHA512(HashSecret, queryString)
7. ✅ So sánh với vnp_SecureHash từ VNPay

### Điểm Quan Trọng:
- 🔐 **HashSecret** phải giữ bí mật, không công khai
- 📝 **Thứ tự sắp xếp** phải nhất quán (alphabet)
- 🚫 **Loại bỏ vnp_SecureHash** trước khi tính lại
- ✅ **Luôn xác thực chữ ký** trước khi xử lý callback

