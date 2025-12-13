# 🚀 HƯỚNG DẪN TRIỂN KHAI NHANH - ENHANCED CHATBOT

## 📋 TÓM TẮT

Đã phân tích database `test.sql` và tạo:
1. ✅ **Document phân tích**: `CHATBOT_DATABASE_ANALYSIS.md`
2. ✅ **Service code mẫu**: `EnhancedProductService.cs`

---

## 🎯 CÁC TÍNH NĂNG ĐÃ SẴN SÀNG

### **1. Tìm kiếm nâng cao:**
- ✅ Tìm theo màn hình (`SearchByScreenAsync`)
- ✅ Tìm theo trọng lượng (`SearchByWeightAsync`)
- ✅ Tìm theo pin (`SearchByBatteryAsync`)
- ✅ Tìm theo bảo hành (`SearchByWarrantyAsync`)

### **2. Tư vấn theo use case:**
- ✅ Gaming laptop
- ✅ Văn phòng
- ✅ Đồ họa/Design
- ✅ Học sinh/Sinh viên
- ✅ Lập trình

### **3. Tính toán giá trị:**
- ✅ Tính % giảm giá (`GetProductsWithDiscountAsync`)
- ✅ Hiển thị số tiền tiết kiệm

### **4. Đánh giá và khuyến mãi:**
- ✅ Lấy rating trung bình (`GetProductWithRatingAsync`)
- ✅ Top review
- ✅ Sản phẩm có khuyến mãi (`GetProductsWithPromotionAsync`)

### **5. So sánh và gợi ý:**
- ✅ So sánh 2 sản phẩm (`CompareProductsAsync`)
- ✅ Sản phẩm tương tự (`GetSimilarProductsAsync`)

### **6. Kiểm tra tồn kho:**
- ✅ Kiểm tra còn hàng (`CheckStockAsync`)
- ✅ Lấy số lượng tồn kho (`GetAvailableQuantityAsync`)

---

## 🔧 CÁCH TRIỂN KHAI

### **Bước 1: Đăng ký Service trong `Program.cs`**

```csharp
// Thêm vào Program.cs
builder.Services.AddScoped<IEnhancedProductService, EnhancedProductService>();
```

### **Bước 2: Inject vào `GuidedChatService` hoặc `RAGChatService`**

```csharp
public class GuidedChatService : IGuidedChatService
{
    private readonly IEnhancedProductService _enhancedProductService;
    
    public GuidedChatService(
        // ... existing services
        IEnhancedProductService enhancedProductService)
    {
        // ...
        _enhancedProductService = enhancedProductService;
    }
}
```

### **Bước 3: Sử dụng trong chatbot**

#### **Ví dụ 1: Tìm kiếm theo màn hình**
```csharp
// Trong HandleTextInputAsync hoặc ProcessMessageAsync
if (messageLower.Contains("màn hình") || messageLower.Contains("screen"))
{
    var screenQuery = ExtractScreenQuery(message); // "16 inch", "QHD+", etc.
    var products = await _enhancedProductService.SearchByScreenAsync(screenQuery);
    
    return new RAGChatResponse
    {
        Answer = $"Em tìm thấy {products.Count} laptop màn hình {screenQuery}:",
        SuggestedProducts = ConvertToSuggestions(products),
        // ...
    };
}
```

#### **Ví dụ 2: Tư vấn theo use case**
```csharp
if (messageLower.Contains("gaming") || messageLower.Contains("game"))
{
    var products = await _enhancedProductService.RecommendByUseCaseAsync("gaming");
    
    return new RAGChatResponse
    {
        Answer = "Em gợi ý các laptop gaming tốt nhất:",
        SuggestedProducts = ConvertToSuggestions(products),
        // ...
    };
}
```

#### **Ví dụ 3: Hiển thị giảm giá**
```csharp
var productsWithDiscount = await _enhancedProductService.GetProductsWithDiscountAsync();
var topDiscount = productsWithDiscount.FirstOrDefault();

if (topDiscount != null)
{
    answer += $"\n💰 **{topDiscount.Product.ProductName}** - " +
              $"Giảm {topDiscount.DiscountPercent:F1}% " +
              $"({topDiscount.DiscountAmount:N0}đ)";
}
```

#### **Ví dụ 4: Hiển thị đánh giá**
```csharp
var productWithRating = await _enhancedProductService.GetProductWithRatingAsync(productId);

if (productWithRating != null && productWithRating.ReviewCount > 0)
{
    answer += $"\n⭐ **Đánh giá**: {productWithRating.AverageRating:F1}/5 " +
              $"({productWithRating.ReviewCount} đánh giá)";
    
    if (!string.IsNullOrEmpty(productWithRating.TopReview))
    {
        answer += $"\n💬 *\"{productWithRating.TopReview.Substring(0, Math.Min(100, productWithRating.TopReview.Length))}...\"*";
    }
}
```

#### **Ví dụ 5: So sánh sản phẩm**
```csharp
if (messageLower.Contains("so sánh") || messageLower.Contains("compare"))
{
    var productIds = ExtractProductIds(message); // Parse từ message
    var comparison = await _enhancedProductService.CompareProductsAsync(productIds[0], productIds[1]);
    
    if (comparison != null)
    {
        var answer = $"**So sánh {comparison.Product1.ProductName} vs {comparison.Product2.ProductName}:**\n\n";
        foreach (var diff in comparison.Differences)
        {
            answer += $"• **{diff.Key}**: {diff.Value}\n";
        }
        
        return new RAGChatResponse { Answer = answer, /* ... */ };
    }
}
```

---

## 📝 VÍ DỤ CÂU HỎI CỦA USER

### **Tìm kiếm nâng cao:**
- "Laptop màn hình 16 inch"
- "Laptop nhẹ dưới 2kg"
- "Laptop pin lâu"
- "Laptop bảo hành 36 tháng"

### **Tư vấn theo use case:**
- "Laptop cho gaming"
- "Laptop văn phòng"
- "Laptop đồ họa"
- "Laptop học sinh"
- "Laptop lập trình"

### **So sánh:**
- "So sánh Dell Alienware vs Lenovo Legion"
- "Dell vs HP cái nào tốt hơn?"

### **Kiểm tra:**
- "Dell Alienware còn hàng không?"
- "Có bao nhiêu cái trong kho?"

---

## 🎨 TÍCH HỢP VÀO CHATBOT RESPONSE

### **Template response với đầy đủ thông tin:**

```csharp
private string BuildEnhancedProductResponse(ProductDTO product, ProductWithRatingDTO? rating = null)
{
    var sb = new StringBuilder();
    
    // Tên và giá
    sb.AppendLine($"**{product.ProductName}** - {product.SellingPrice:N0}đ");
    
    // Giảm giá (nếu có)
    if (product.OriginalSellingPrice > product.SellingPrice)
    {
        var discount = ((product.OriginalSellingPrice.Value - product.SellingPrice.Value) / product.OriginalSellingPrice.Value) * 100;
        sb.AppendLine($"💰 Giảm {discount:F1}% (Tiết kiệm {product.OriginalSellingPrice.Value - product.SellingPrice.Value:N0}đ)");
    }
    
    // Đánh giá
    if (rating != null && rating.ReviewCount > 0)
    {
        sb.AppendLine($"⭐ {rating.AverageRating:F1}/5 ({rating.ReviewCount} đánh giá)");
    }
    
    // Cấu hình
    var config = product.Configurations?.FirstOrDefault();
    if (config != null)
    {
        sb.AppendLine($"⚡ CPU: {config.Cpu}");
        sb.AppendLine($"💾 RAM: {config.Ram} | Ổ cứng: {config.Rom}");
        if (!string.IsNullOrEmpty(config.Card))
            sb.AppendLine($"🎮 Card: {config.Card}");
    }
    
    // Đặc điểm
    if (!string.IsNullOrEmpty(product.Screen))
        sb.AppendLine($"🖥️ Màn hình: {product.Screen}");
    if (product.Weight.HasValue)
        sb.AppendLine($"⚖️ Trọng lượng: {product.Weight}kg");
    if (!string.IsNullOrEmpty(product.Pin))
        sb.AppendLine($"🔋 Pin: {product.Pin}");
    if (product.WarrantyPeriod.HasValue)
        sb.AppendLine($"🛡️ Bảo hành: {product.WarrantyPeriod} tháng");
    
    return sb.ToString();
}
```

---

## ✅ CHECKLIST TRIỂN KHAI

### **Phase 1: Setup (5 phút)**
- [ ] Đăng ký `IEnhancedProductService` trong `Program.cs`
- [ ] Inject vào `GuidedChatService` hoặc `RAGChatService`
- [ ] Build và test không lỗi

### **Phase 2: Tìm kiếm nâng cao (30 phút)**
- [ ] Thêm intent detection cho "màn hình", "trọng lượng", "pin", "bảo hành"
- [ ] Gọi `SearchByScreenAsync`, `SearchByWeightAsync`, etc.
- [ ] Test với các câu hỏi mẫu

### **Phase 3: Use case recommendations (30 phút)**
- [ ] Thêm intent detection cho "gaming", "văn phòng", "đồ họa", etc.
- [ ] Gọi `RecommendByUseCaseAsync`
- [ ] Test với các use case khác nhau

### **Phase 4: Giá trị và đánh giá (20 phút)**
- [ ] Hiển thị % giảm giá trong response
- [ ] Hiển thị rating khi có
- [ ] Test với sản phẩm có discount và rating

### **Phase 5: So sánh (20 phút)**
- [ ] Parse 2 product IDs từ message
- [ ] Gọi `CompareProductsAsync`
- [ ] Format response dạng bảng so sánh

---

## 🎯 KẾT QUẢ MONG ĐỢI

Sau khi triển khai, chatbot sẽ:

✅ **Tư vấn chính xác hơn** - Hiểu nhu cầu cụ thể (gaming, văn phòng, etc.)  
✅ **Hiển thị giá trị tốt hơn** - % giảm giá, tiết kiệm bao nhiêu  
✅ **Tin cậy hơn** - Đánh giá từ khách hàng thật  
✅ **Hấp dẫn hơn** - Thông báo khuyến mãi tự động  
✅ **So sánh thông minh** - So sánh nhiều tiêu chí cùng lúc  

---

## 📚 TÀI LIỆU THAM KHẢO

- **Phân tích chi tiết**: `CHATBOT_DATABASE_ANALYSIS.md`
- **Code service**: `WebLaptopBE/Services/EnhancedProductService.cs`
- **Database schema**: `test.sql`

---

**🚀 Bắt đầu từ Phase 1 và triển khai từng bước!**

