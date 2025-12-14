# Cách Hoạt Động Của Thanh Toán VNPay (Chuyển Khoản Ngân Hàng)

## Tổng Quan

VNPay là cổng thanh toán trực tuyến tại Việt Nam, cho phép khách hàng thanh toán đơn hàng thông qua chuyển khoản ngân hàng. Quy trình thanh toán VNPay hoạt động theo mô hình **redirect-based payment** với các bước sau:

---

## Luồng Thanh Toán VNPay

### **Bước 1: Khách Hàng Chọn Thanh Toán VNPay**

Khi khách hàng chọn phương thức thanh toán **"Chuyển khoản ngân hàng"** và nhấn nút "Đặt hàng":

```
Frontend → API: POST /api/Checkout/create
{
  PaymentMethod: "Chuyển khoản ngân hàng",
  CustomerId, FullName, Phone, Email, DeliveryAddress,
  SelectedCartDetailIds, ...
}
```

### **Bước 2: Backend Xử Lý và Tạo Payment URL**

Backend (`CheckoutAPIController`) thực hiện:

#### 2.1. **Lưu Thông Tin Đơn Hàng Tạm (Pending Order)**
- **Lý do**: Đơn hàng chưa được tạo ngay vì chưa thanh toán thành công
- Tạo `PendingOrderWithTxnRef` chứa:
  - Thông tin khách hàng (CustomerId, FullName, Phone, Email)
  - Địa chỉ giao hàng
  - Danh sách sản phẩm (SelectedCartDetailIds)
  - Tổng tiền (TotalAmount, Discount, DeliveryFee)
  - Khuyến mại đã chọn

#### 2.2. **Tạo Mã Tham Chiếu Giao Dịch (TxnRef)**
```csharp
var tempOrderId = DateTime.Now.Ticks % 1000000; // 6 chữ số cuối
var txnRef = $"{tempOrderId}_{request.CustomerId}"; // VD: "123456_001"
```

- **TxnRef** là mã duy nhất để:
  - Phân biệt các giao dịch
  - Liên kết giữa đơn hàng tạm và kết quả thanh toán từ VNPay
  - Không được trùng lặp trong ngày

#### 2.3. **Lưu Vào Session**
```csharp
HttpContext.Session.SetString($"PendingOrder_{txnRef}", pendingOrderJson);
```

- Lưu thông tin đơn hàng tạm vào Session với key = `PendingOrder_{txnRef}`
- Sẽ được lấy lại sau khi VNPay callback về

#### 2.4. **Tạo Payment URL từ VNPay**

Backend gọi `VnPayService.CreatePaymentUrl()`:

**Các tham số gửi đến VNPay:**
- `vnp_Version`: "2.1.0" (phiên bản API)
- `vnp_Command`: "pay" (lệnh thanh toán)
- `vnp_TmnCode`: Mã merchant (từ config)
- `vnp_Amount`: Số tiền × 100 (VD: 100,000 VND → 10000000)
- `vnp_CreateDate`: Thời gian tạo (format: yyyyMMddHHmmss)
- `vnp_CurrCode`: "VND"
- `vnp_IpAddr`: IP khách hàng
- `vnp_Locale`: "vn"
- `vnp_OrderInfo`: Mô tả đơn hàng
- `vnp_OrderType`: "other"
- `vnp_ReturnUrl`: URL callback sau khi thanh toán
- `vnp_TxnRef`: Mã tham chiếu giao dịch

**Tạo Chữ Ký Bảo Mật (Secure Hash):**
1. Sắp xếp tất cả tham số theo thứ tự alphabet
2. Nối thành chuỗi query string: `key1=value1&key2=value2&...`
3. Tạo chữ ký HMAC-SHA512:
   ```csharp
   vnp_SecureHash = HmacSHA512(HashSecret, queryString)
   ```
4. Thêm `vnp_SecureHash` vào cuối URL

**Kết quả:** URL thanh toán VNPay
```
https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?
  vnp_Amount=10000000&
  vnp_Command=pay&
  vnp_CreateDate=20240101120000&
  ...&
  vnp_SecureHash=abc123def456...
```

### **Bước 3: Chuyển Hướng Đến Trang Thanh Toán VNPay**

Backend trả về:
```json
{
  "paymentUrl": "https://sandbox.vnpayment.vn/...",
  "requiresPayment": true,
  "message": "Chuyển hướng đến VNPay để thanh toán..."
}
```

Frontend tự động redirect khách hàng đến URL này:
```javascript
window.location.href = response.paymentUrl;
```

### **Bước 4: Khách Hàng Thanh Toán Trên VNPay**

Trên trang VNPay, khách hàng:
1. Chọn ngân hàng
2. Nhập thông tin tài khoản
3. Xác nhận thanh toán
4. VNPay xử lý giao dịch với ngân hàng

### **Bước 5: VNPay Callback Về Backend**

Sau khi thanh toán xong (thành công hoặc thất bại), VNPay redirect về:
```
GET /api/Checkout/vnpay-callback?
  vnp_Amount=10000000&
  vnp_BankCode=NCB&
  vnp_ResponseCode=00&
  vnp_TxnRef=123456_001&
  vnp_TransactionNo=12345678&
  vnp_SecureHash=xyz789...
```

### **Bước 6: Backend Xác Thực và Xử Lý Callback**

#### 6.1. **Xác Thực Chữ Ký (Validate Signature)**

Backend gọi `VnPayService.PaymentExecute()`:

1. **Thu thập tất cả tham số** từ query string (bắt đầu bằng `vnp_`)
2. **Loại bỏ** `vnp_SecureHash` và `vnp_SecureHashType` khỏi danh sách
3. **Sắp xếp** các tham số còn lại theo alphabet
4. **Tạo chuỗi query** giống như khi tạo request
5. **Tính lại chữ ký**:
   ```csharp
   myChecksum = HmacSHA512(HashSecret, queryString)
   ```
6. **So sánh** với `vnp_SecureHash` từ VNPay:
   - Nếu khác → **Lỗi bảo mật** → Từ chối giao dịch
   - Nếu giống → **Xác thực thành công** → Tiếp tục xử lý

**Tại sao cần xác thực chữ ký?**
- Đảm bảo dữ liệu không bị giả mạo
- Xác nhận callback thực sự đến từ VNPay
- Bảo vệ khỏi các cuộc tấn công man-in-the-middle

#### 6.2. **Kiểm Tra Mã Phản Hồi (Response Code)**

- `vnp_ResponseCode = "00"` → **Thanh toán thành công**
- Các mã khác → **Thanh toán thất bại** (VD: "07", "09", "10", "11", "75", "79"...)

### **Bước 7: Xử Lý Kết Quả Thanh Toán**

#### **Trường Hợp 1: Thanh Toán Thành Công (ResponseCode = "00")**

1. **Lấy TxnRef từ callback:**
   ```csharp
   var txnRef = response.OrderId; // VD: "123456_001"
   ```

2. **Lấy thông tin đơn hàng tạm từ Session:**
   ```csharp
   var sessionKey = $"PendingOrder_{txnRef}";
   var pendingOrderJson = HttpContext.Session.GetString(sessionKey);
   var pendingOrder = JsonSerializer.Deserialize<PendingOrderWithTxnRef>(pendingOrderJson);
   ```

3. **Tạo đơn hàng thực sự:**
   - Tạo `SaleInvoice` với:
     - `PaymentMethod = "Chuyển khoản ngân hàng"`
     - `Status = "Chờ xử lý"`
     - Tất cả thông tin từ `pendingOrder`
   - Tạo `SaleInvoiceDetails` cho từng sản phẩm
   - Lưu vào database

4. **Xóa giỏ hàng:**
   - Xóa các `CartDetail` đã thanh toán
   - Xóa `Cart` nếu không còn sản phẩm

5. **Xóa thông tin tạm:**
   ```csharp
   HttpContext.Session.Remove($"PendingOrder_{txnRef}");
   ```

6. **Gửi email xác nhận đơn hàng** (bất đồng bộ, không chặn luồng)

7. **Redirect về trang thành công:**
   ```
   /User/Account?success=true&message=Đặt hàng thành công!&orderId=SI001
   ```

#### **Trường Hợp 2: Thanh Toán Thất Bại**

1. **Lấy mã lỗi:**
   ```csharp
   var errorCode = response.VnPayResponseCode; // VD: "07", "09", "11"...
   ```

2. **Lấy thông báo lỗi tương ứng:**
   ```csharp
   var errorMessage = GetVnPayErrorMessage(errorCode);
   // VD: "Giao dịch không thành công do: Đã hết hạn chờ thanh toán..."
   ```

3. **Redirect về trang checkout với thông báo lỗi:**
   ```
   /Cart/Checkout?error=payment-failed&code=11&message=...
   ```

4. **Thông tin đơn hàng tạm vẫn còn trong Session** (có thể thử lại)

---

## Các Mã Lỗi VNPay Thường Gặp

| Mã | Ý Nghĩa |
|---|---|
| **00** | Giao dịch thành công |
| **07** | Trừ tiền thành công, giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường) |
| **09** | Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking |
| **10** | Xác thực thông tin thẻ/tài khoản không đúng. Quá 3 lần |
| **11** | Đã hết hạn chờ thanh toán. Xin vui lòng thực hiện lại giao dịch |
| **12** | Thẻ/Tài khoản bị khóa |
| **51** | Tài khoản không đủ số dư để thực hiện giao dịch |
| **75** | Ngân hàng thanh toán đang bảo trì |
| **79** | Nhập sai mật khẩu thanh toán quá số lần quy định |

---

## Bảo Mật

### **1. Chữ Ký HMAC-SHA512**
- Tất cả request/response đều có chữ ký
- Backend luôn xác thực chữ ký trước khi xử lý
- HashSecret được lưu trong config, không công khai

### **2. TxnRef Duy Nhất**
- Mỗi giao dịch có TxnRef riêng
- Kết hợp với CustomerId để tránh trùng lặp
- Sử dụng timestamp để đảm bảo tính duy nhất

### **3. Session Management**
- Thông tin đơn hàng tạm chỉ lưu trong Session
- Tự động xóa sau khi thanh toán thành công
- Có thể expire sau một thời gian

### **4. IP Address Tracking**
- Backend gửi IP khách hàng đến VNPay
- VNPay có thể sử dụng để phát hiện bất thường

---

## So Sánh Với COD (Thanh Toán Khi Nhận Hàng)

| Tiêu Chí | VNPay | COD |
|---|---|---|
| **Thời điểm tạo đơn hàng** | Sau khi thanh toán thành công | Ngay khi đặt hàng |
| **Lưu trữ tạm** | Có (Session) | Không |
| **Xác thực thanh toán** | Có (chữ ký VNPay) | Không cần |
| **Rủi ro** | Thấp (đã thanh toán) | Cao (có thể hủy) |
| **Trải nghiệm** | Phức tạp hơn (redirect) | Đơn giản hơn |

---

## Tóm Tắt Luồng

```
1. Khách hàng chọn VNPay → Frontend gửi request
2. Backend lưu đơn hàng tạm + tạo Payment URL
3. Frontend redirect đến VNPay
4. Khách hàng thanh toán trên VNPay
5. VNPay callback về Backend với kết quả
6. Backend xác thực chữ ký + kiểm tra ResponseCode
7a. Thành công → Tạo đơn hàng thực + xóa giỏ hàng + email
7b. Thất bại → Redirect về checkout với thông báo lỗi
```

---

## Lưu Ý Khi Phát Triển

1. **Luôn xác thực chữ ký** trước khi xử lý callback
2. **Kiểm tra ResponseCode** trước khi tạo đơn hàng
3. **Xử lý timeout**: Nếu khách hàng không thanh toán, thông tin tạm có thể bị mất
4. **Logging**: Ghi log tất cả callback để debug
5. **Idempotency**: Đảm bảo không tạo đơn hàng trùng lặp nếu callback bị gọi nhiều lần
6. **Error handling**: Xử lý tất cả trường hợp lỗi có thể xảy ra

