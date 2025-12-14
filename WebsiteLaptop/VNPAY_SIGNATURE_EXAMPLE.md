# Ví Dụ Cụ Thể: Tạo và Xác Thực Chữ Ký VNPay

## Tình Huống

Khách hàng đặt hàng với:
- **Số tiền**: 500,000 VND
- **Mã đơn hàng**: 789012_001
- **Thời gian**: 2024-01-15 14:30:00

---

## PHẦN 1: TẠO CHỮ KÝ (REQUEST)

### Bước 1: Thu Thập Tham Số

```csharp
// Các tham số cần gửi đến VNPay
vnp_Version = "2.1.0"
vnp_Command = "pay"
vnp_TmnCode = "XPN3KK8O"
vnp_Amount = "50000000"  // 500,000 VND × 100
vnp_CreateDate = "20240115143000"
vnp_CurrCode = "VND"
vnp_IpAddr = "192.168.1.100"
vnp_Locale = "vn"
vnp_OrderInfo = "Thanh toán cho đơn hàng:789012"
vnp_OrderType = "other"
vnp_ReturnUrl = "http://localhost:5068/api/Checkout/vnpay-callback"
vnp_TxnRef = "789012_001"
```

### Bước 2: Sắp Xếp Theo Alphabet

Sau khi sắp xếp (tự động bởi SortedList):

```
1. vnp_Amount
2. vnp_Command
3. vnp_CreateDate
4. vnp_CurrCode
5. vnp_IpAddr
6. vnp_Locale
7. vnp_OrderInfo
8. vnp_OrderType
9. vnp_ReturnUrl
10. vnp_TmnCode
11. vnp_TxnRef
12. vnp_Version
```

### Bước 3: URL Encode và Tạo Query String

**Trước khi encode:**
```
vnp_Amount=50000000
vnp_Command=pay
vnp_CreateDate=20240115143000
vnp_CurrCode=VND
vnp_IpAddr=192.168.1.100
vnp_Locale=vn
vnp_OrderInfo=Thanh toán cho đơn hàng:789012
vnp_OrderType=other
vnp_ReturnUrl=http://localhost:5068/api/Checkout/vnpay-callback
vnp_TmnCode=XPN3KK8O
vnp_TxnRef=789012_001
vnp_Version=2.1.0
```

**Sau khi URL encode (theo chuẩn VNPay):**
```
vnp_Amount=50000000
vnp_Command=pay
vnp_CreateDate=20240115143000
vnp_CurrCode=VND
vnp_IpAddr=192.168.1.100
vnp_Locale=vn
vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012
vnp_OrderType=other
vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5068%2Fapi%2FCheckout%2Fvnpay-callback
vnp_TmnCode=XPN3KK8O
vnp_TxnRef=789012_001
vnp_Version=2.1.0
```

**Query String (với &):**
```
vnp_Amount=50000000&
vnp_Command=pay&
vnp_CreateDate=20240115143000&
vnp_CurrCode=VND&
vnp_IpAddr=192.168.1.100&
vnp_Locale=vn&
vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012&
vnp_OrderType=other&
vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5068%2Fapi%2FCheckout%2Fvnpay-callback&
vnp_TmnCode=XPN3KK8O&
vnp_TxnRef=789012_001&
vnp_Version=2.1.0&
```

### Bước 4: Loại Bỏ '&' Cuối Cùng

**Chuỗi để ký (signData):**
```
vnp_Amount=50000000&vnp_Command=pay&vnp_CreateDate=20240115143000&vnp_CurrCode=VND&vnp_IpAddr=192.168.1.100&vnp_Locale=vn&vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012&vnp_OrderType=other&vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5068%2Fapi%2FCheckout%2Fvnpay-callback&vnp_TmnCode=XPN3KK8O&vnp_TxnRef=789012_001&vnp_Version=2.1.0
```

### Bước 5: Tính HMAC-SHA512

**Input:**
```
HashSecret = "T47ZU2IYO4I38U1GNLGKRQLH0W8B40JI"
signData = "vnp_Amount=50000000&vnp_Command=pay&vnp_CreateDate=20240115143000&vnp_CurrCode=VND&vnp_IpAddr=192.168.1.100&vnp_Locale=vn&vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012&vnp_OrderType=other&vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5068%2Fapi%2FCheckout%2Fvnpay-callback&vnp_TmnCode=XPN3KK8O&vnp_TxnRef=789012_001&vnp_Version=2.1.0"
```

**Quy trình:**
1. Chuyển HashSecret sang bytes (UTF-8)
2. Chuyển signData sang bytes (UTF-8)
3. Tính HMAC-SHA512
4. Chuyển kết quả sang hex string (128 ký tự)

**Kết quả chữ ký (ví dụ - thực tế sẽ khác):**
```
a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890
```

### Bước 6: Tạo URL Cuối Cùng

**URL thanh toán VNPay:**
```
https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?
  vnp_Amount=50000000&
  vnp_Command=pay&
  vnp_CreateDate=20240115143000&
  vnp_CurrCode=VND&
  vnp_IpAddr=192.168.1.100&
  vnp_Locale=vn&
  vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012&
  vnp_OrderType=other&
  vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5068%2Fapi%2FCheckout%2Fvnpay-callback&
  vnp_TmnCode=XPN3KK8O&
  vnp_TxnRef=789012_001&
  vnp_Version=2.1.0&
  vnp_SecureHash=a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890
```

---

## PHẦN 2: XÁC THỰC CHỮ KÝ (RESPONSE)

### Bước 1: Nhận Callback Từ VNPay

VNPay gửi callback về với query string:

```
GET /api/Checkout/vnpay-callback?
  vnp_Amount=50000000&
  vnp_BankCode=NCB&
  vnp_CardType=ATM&
  vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012&
  vnp_PayDate=20240115143500&
  vnp_ResponseCode=00&
  vnp_TmnCode=XPN3KK8O&
  vnp_TransactionNo=87654321&
  vnp_TransactionStatus=00&
  vnp_TxnRef=789012_001&
  vnp_SecureHash=xyz789abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890&
  vnp_SecureHashType=SHA512
```

### Bước 2: Thu Thập Tất Cả Tham Số

```csharp
// Thu thập tất cả tham số bắt đầu bằng "vnp_"
vnp_Amount = "50000000"
vnp_BankCode = "NCB"
vnp_CardType = "ATM"
vnp_OrderInfo = "Thanh+toan+cho+don+hang%3A789012"
vnp_PayDate = "20240115143500"
vnp_ResponseCode = "00"
vnp_TmnCode = "XPN3KK8O"
vnp_TransactionNo = "87654321"
vnp_TransactionStatus = "00"
vnp_TxnRef = "789012_001"
vnp_SecureHash = "xyz789..." (từ VNPay)
vnp_SecureHashType = "SHA512"
```

### Bước 3: Loại Bỏ vnp_SecureHash và vnp_SecureHashType

**QUAN TRỌNG:** Phải loại bỏ trước khi tính lại chữ ký!

**Sau khi loại bỏ:**
```
vnp_Amount = "50000000"
vnp_BankCode = "NCB"
vnp_CardType = "ATM"
vnp_OrderInfo = "Thanh+toan+cho+don+hang%3A789012"
vnp_PayDate = "20240115143500"
vnp_ResponseCode = "00"
vnp_TmnCode = "XPN3KK8O"
vnp_TransactionNo = "87654321"
vnp_TransactionStatus = "00"
vnp_TxnRef = "789012_001"
```

### Bước 4: Sắp Xếp Theo Alphabet

```
1. vnp_Amount
2. vnp_BankCode
3. vnp_CardType
4. vnp_OrderInfo
5. vnp_PayDate
6. vnp_ResponseCode
7. vnp_TmnCode
8. vnp_TransactionNo
9. vnp_TransactionStatus
10. vnp_TxnRef
```

### Bước 5: URL Encode và Tạo Query String

**Query String:**
```
vnp_Amount=50000000&
vnp_BankCode=NCB&
vnp_CardType=ATM&
vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012&
vnp_PayDate=20240115143500&
vnp_ResponseCode=00&
vnp_TmnCode=XPN3KK8O&
vnp_TransactionNo=87654321&
vnp_TransactionStatus=00&
vnp_TxnRef=789012_001
```

**Chuỗi để xác thực (rspRaw):**
```
vnp_Amount=50000000&vnp_BankCode=NCB&vnp_CardType=ATM&vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012&vnp_PayDate=20240115143500&vnp_ResponseCode=00&vnp_TmnCode=XPN3KK8O&vnp_TransactionNo=87654321&vnp_TransactionStatus=00&vnp_TxnRef=789012_001
```

### Bước 6: Tính Lại Chữ Ký

**Input:**
```
HashSecret = "T47ZU2IYO4I38U1GNLGKRQLH0W8B40JI"
rspRaw = "vnp_Amount=50000000&vnp_BankCode=NCB&vnp_CardType=ATM&vnp_OrderInfo=Thanh+toan+cho+don+hang%3A789012&vnp_PayDate=20240115143500&vnp_ResponseCode=00&vnp_TmnCode=XPN3KK8O&vnp_TransactionNo=87654321&vnp_TransactionStatus=00&vnp_TxnRef=789012_001"
```

**Tính HMAC-SHA512:**
```csharp
myChecksum = Utils.HmacSHA512("T47ZU2IYO4I38U1GNLGKRQLH0W8B40JI", rspRaw);
```

**Kết quả (ví dụ):**
```
xyz789abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890
```

### Bước 7: So Sánh Chữ Ký

**Chữ ký từ VNPay:**
```
vnp_SecureHash = "xyz789abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890"
```

**Chữ ký tính lại:**
```
myChecksum = "xyz789abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890"
```

**So sánh:**
```csharp
bool isValid = myChecksum.Equals(
    "xyz789abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
    StringComparison.InvariantCultureIgnoreCase
);
// Kết quả: true ✅
```

**Kết luận:** Chữ ký hợp lệ → Dữ liệu đáng tin cậy → Tiếp tục xử lý đơn hàng

---

## PHẦN 3: TRƯỜNG HỢP CHỮ KÝ KHÔNG HỢP LỆ

### Tình Huống: Dữ Liệu Bị Thay Đổi

**Giả sử hacker cố gắng thay đổi số tiền:**

**Callback gốc từ VNPay:**
```
vnp_Amount=50000000
vnp_ResponseCode=00
vnp_SecureHash=xyz789...
```

**Hacker thay đổi:**
```
vnp_Amount=1000000  ← Thay đổi từ 500,000 VND thành 10,000 VND
vnp_ResponseCode=00
vnp_SecureHash=xyz789...  ← Giữ nguyên chữ ký cũ
```

### Quy Trình Xác Thực:

**Bước 1-5: Tạo Query String Mới**
```
rspRaw = "vnp_Amount=1000000&vnp_ResponseCode=00&..."
```

**Bước 6: Tính Lại Chữ Ký**
```
myChecksum = HMAC-SHA512(secret, "vnp_Amount=1000000&vnp_ResponseCode=00&...")
// Kết quả: "abc123..." (KHÁC với chữ ký cũ)
```

**Bước 7: So Sánh**
```
myChecksum = "abc123..."
vnp_SecureHash = "xyz789..."  ← Từ VNPay (cho số tiền 500,000)
```

**Kết quả:**
```csharp
bool isValid = "abc123...".Equals("xyz789...", ...);
// Kết quả: false ❌
```

**Hành động:**
```csharp
if (!checkSignature)
{
    return new VnPaymentResponseModel
    {
        Success = false  // Từ chối giao dịch
    };
}
```

**Kết luận:** Chữ ký không hợp lệ → Dữ liệu đã bị thay đổi → Từ chối giao dịch để bảo vệ hệ thống

---

## PHẦN 4: SO SÁNH REQUEST VÀ RESPONSE

| Tiêu Chí | REQUEST (Tạo URL) | RESPONSE (Xác thực) |
|---|---|---|
| **Mục đích** | Tạo URL thanh toán | Xác thực callback |
| **Dữ liệu** | Tham số đơn hàng | Kết quả thanh toán |
| **Tham số** | vnp_Amount, vnp_Command, vnp_TxnRef... | vnp_Amount, vnp_ResponseCode, vnp_TransactionNo... |
| **Loại bỏ** | Không cần | Phải loại bỏ vnp_SecureHash |
| **Kết quả** | URL với vnp_SecureHash | So sánh chữ ký |
| **Khi nào** | Khi khách hàng đặt hàng | Khi VNPay callback về |

---

## PHẦN 5: CHECKLIST KHI IMPLEMENT

### ✅ Tạo Chữ Ký (Request)
- [ ] Sắp xếp tham số theo alphabet
- [ ] URL encode đúng (thay %20 thành +)
- [ ] Loại bỏ '&' cuối cùng trước khi tính chữ ký
- [ ] Sử dụng HMAC-SHA512
- [ ] Thêm vnp_SecureHash vào URL

### ✅ Xác Thực Chữ Ký (Response)
- [ ] Thu thập tất cả tham số bắt đầu bằng "vnp_"
- [ ] **Loại bỏ vnp_SecureHash và vnp_SecureHashType**
- [ ] Sắp xếp tham số theo alphabet
- [ ] URL encode đúng (thay %20 thành +)
- [ ] Loại bỏ '&' cuối cùng
- [ ] Tính lại chữ ký với cùng HashSecret
- [ ] So sánh không phân biệt hoa thường
- [ ] **Từ chối giao dịch nếu chữ ký không hợp lệ**

---

## PHẦN 6: DEBUG TIPS

### Nếu Chữ Ký Không Khớp:

1. **Kiểm tra HashSecret:**
   ```csharp
   // Đảm bảo HashSecret đúng
   var hashSecret = _config["VnPay:HashSecret"];
   Console.WriteLine($"HashSecret: {hashSecret}");
   ```

2. **In ra Query String:**
   ```csharp
   var signData = GetResponseData();
   Console.WriteLine($"SignData: {signData}");
   ```

3. **In ra Chữ Ký:**
   ```csharp
   var myChecksum = Utils.HmacSHA512(secretKey, signData);
   Console.WriteLine($"MyChecksum: {myChecksum}");
   Console.WriteLine($"VNPayHash: {inputHash}");
   ```

4. **Kiểm tra Thứ Tự:**
   - Đảm bảo sắp xếp theo alphabet
   - Kiểm tra VnPayCompare hoạt động đúng

5. **Kiểm tra URL Encode:**
   - Đảm bảo thay %20 thành +
   - Kiểm tra các ký tự đặc biệt được encode đúng

---

## KẾT LUẬN

Chữ ký VNPay là cơ chế bảo mật quan trọng để:
- ✅ Xác thực nguồn gốc dữ liệu
- ✅ Đảm bảo tính toàn vẹn
- ✅ Chống tấn công giả mạo

**Luôn nhớ:**
1. Sắp xếp theo alphabet
2. Loại bỏ vnp_SecureHash trước khi tính lại
3. URL encode đúng chuẩn VNPay
4. Từ chối giao dịch nếu chữ ký không hợp lệ

