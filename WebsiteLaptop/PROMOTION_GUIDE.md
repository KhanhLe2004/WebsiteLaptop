# Hướng dẫn sử dụng tính năng Khuyến mại

## Tổng quan
Tính năng khuyến mại cho phép áp dụng các loại giảm giá và ưu đãi khác nhau cho sản phẩm trong giỏ hàng khi checkout.

## Quy tắc khuyến mại

### 1. Phân loại khuyến mại
- **Khuyến mại giảm giá**: Giảm giá theo phần trăm cho sản phẩm cụ thể
- **Khuyến mại Freeship**: Miễn phí vận chuyển cho toàn bộ đơn hàng

### 2. Quy tắc áp dụng
- **Được chọn cả 2 loại**: Có thể chọn cả khuyến mại giảm giá và freeship cùng lúc
- **Cùng loại chỉ chọn 1**: Trong mỗi loại khuyến mại chỉ được chọn 1 (ví dụ: chỉ chọn 1 trong các khuyến mại giảm 10%, 15%, 20%)
- **Chỉ hiển thị khuyến mại của sản phẩm được chọn**: Chỉ hiển thị khuyến mại cho các sản phẩm có trong giỏ hàng đã chọn thanh toán

### 3. Cách hiển thị khuyến mại
- **Giảm giá**: Hiển thị "Giảm giá X% - Tên sản phẩm" (ví dụ: "Giảm giá 10% - ASUS ExpertBook")
- **Freeship**: Chỉ hiển thị "Freeship" (không hiển thị tên sản phẩm)

### 4. Cách tính khuyến mại
- **Giảm giá**: Lấy phần trăm từ trường "Nội dung chi tiết" để tính giảm giá
- **Freeship**: Khi có bất kỳ sản phẩm nào có khuyến mại freeship thì áp dụng cho toàn bộ đơn hàng

## Các loại khuyến mại được hỗ trợ

### 1. Khuyến mại giảm giá theo phần trăm
- Phần trăm giảm giá được lấy từ trường "ContentDetail"
- Ví dụ: "Giảm 15% giá bán sản phẩm" → Giảm 15%
- Chỉ áp dụng cho sản phẩm cụ thể có khuyến mại

### 2. Khuyến mại miễn phí vận chuyển
- **Freeship**: Miễn phí vận chuyển cho toàn bộ đơn hàng
- Chỉ cần 1 sản phẩm có freeship là áp dụng cho cả đơn hàng

## Cách sử dụng

### 1. Thêm dữ liệu khuyến mại
Chạy script SQL trong file `sample_promotions.sql` để thêm dữ liệu mẫu:
```sql
-- Thay thế ProductId bằng các ID sản phẩm thực tế
INSERT INTO Promotions (PromotionId, ProductId, Type, ContentDetail) VALUES
('KM001', 'P001', 'Khuyến mại 10%', 'Giảm 10% giá bán sản phẩm');
```

### 2. Sử dụng trên giao diện Checkout
1. Chọn sản phẩm trong giỏ hàng
2. Chọn khuyến mại từ 2 dropdown riêng biệt:
   - **Dropdown 1**: Khuyến mại giảm giá (chỉ chọn 1)
   - **Dropdown 2**: Khuyến mại freeship (chỉ chọn 1)
3. Có thể chọn cả 2 loại khuyến mại cùng lúc
4. Nhấn nút "Áp dụng khuyến mại"
5. Hệ thống sẽ tự động tính toán và hiển thị:
   - **Tạm tính**: Số tiền gốc của đơn hàng (không áp dụng khuyến mại)
   - **Vận chuyển**: Phí vận chuyển (= 0 nếu có freeship)
   - **Giảm giá**: Tổng số tiền được giảm (chỉ hiển thị khi có khuyến mại)
   - **TỔNG**: Số tiền cuối cùng sau khi áp dụng khuyến mại
   - Chi tiết khuyến mại:
     - **Sản phẩm được giảm giá**: "Giảm giá X% - Tên sản phẩm: -Số tiền"
     - **Khuyến mại vận chuyển**: "Freeship cho toàn bộ đơn hàng: -Số tiền"

### 3. API Endpoints

#### Lấy danh sách khuyến mại theo sản phẩm
```
GET /api/Checkout/promotions?customerId={customerId}&selectedCartDetailIds={id1}&selectedCartDetailIds={id2}

Response:
{
    "discountPromotions": [
        {
            "promotionId": "string",
            "productId": "string", 
            "productName": "string",
            "type": "string",
            "contentDetail": "string",
            "displayText": "string"
        }
    ],
    "freeshipPromotions": [...]
}
```

#### Áp dụng khuyến mại
```
POST /api/Checkout/apply-promotion
Content-Type: application/json

{
    "customerId": "string",
    "selectedDiscountPromotions": ["promotionId1"],
    "selectedFreeshipPromotions": ["promotionId2"],
    "selectedCartDetailIds": ["string"],
    "deliveryFee": number
}
```

## Lưu ý kỹ thuật

### Backend (CheckoutAPIController.cs)
- Thêm 2 API endpoints mới: `/promotions` và `/apply-promotion`
- Cập nhật `CheckoutRequest` model với các trường khuyến mại
- Logic tính toán khuyến mại trong quá trình tạo đơn hàng
- Hỗ trợ lưu thông tin khuyến mại trong `PendingOrder` cho VNPay

### Frontend (Checkout.cshtml)
- Thay thế input text bằng dropdown select
- Thêm logic JavaScript để load và áp dụng khuyến mại
- Cập nhật hiển thị tổng tiền theo thời gian thực
- Hiển thị thông tin chi tiết về khuyến mại đã áp dụng

## Mở rộng

### Thêm loại khuyến mại mới
1. Thêm dữ liệu vào bảng `Promotions` với `Type` mới
2. Cập nhật logic trong các hàm:
   - `ApplyPromotion` API
   - Logic tính toán trong `CreateOrder`
   - Frontend JavaScript nếu cần

### Validation
- Kiểm tra sản phẩm có khuyến mại không
- Kiểm tra khách hàng đã đăng nhập
- Kiểm tra giỏ hàng không rỗng
- Hiển thị thông báo lỗi chi tiết

## Test
1. Thêm dữ liệu khuyến mại mẫu
2. Thêm sản phẩm vào giỏ hàng
3. Vào trang checkout
4. Chọn khuyến mại và áp dụng
5. Kiểm tra tính toán đúng
6. Hoàn tất đặt hàng
