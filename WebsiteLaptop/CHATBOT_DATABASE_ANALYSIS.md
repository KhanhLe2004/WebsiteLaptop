# 📊 PHÂN TÍCH DATABASE ĐỂ KHAI THÁC TỐI ĐA CHATBOT

## 🎯 MỤC TIÊU
Phân tích file `test.sql` để tối ưu chatbot, tận dụng tối đa dữ liệu có sẵn trong database.

---

## 📋 CẤU TRÚC DATABASE QUAN TRỌNG CHO CHATBOT

### 1. **Bảng `Product`** - Thông tin sản phẩm chính
```sql
CREATE TABLE [dbo].[Product](
    [product_id] [nvarchar](20) NOT NULL,
    [product_name] [nvarchar](100) NULL,           -- ✅ Tên sản phẩm
    [product_model] [nvarchar](100) NULL,          -- ✅ Model (ví dụ: "16X Aurora AC2025")
    [warranty_period] [int] NULL,                   -- ✅ Thời gian bảo hành (tháng)
    [original_selling_price] [decimal](18, 2) NULL, -- ✅ Giá gốc
    [selling_price] [decimal](18, 2) NULL,          -- ✅ Giá bán
    [screen] [nvarchar](50) NULL,                   -- ✅ Màn hình (ví dụ: "16inch QHD+ 240Hz")
    [camera] [nvarchar](50) NULL,                   -- ✅ Camera (ví dụ: "1080p")
    [connect] [nvarchar](200) NULL,                 -- ✅ Cổng kết nối (chi tiết)
    [weight] [decimal](18, 2) NULL,                 -- ✅ Trọng lượng (kg)
    [pin] [nvarchar](50) NULL,                      -- ✅ Pin (ví dụ: "97Wh")
    [brand_id] [nvarchar](20) NULL,                -- ✅ FK → Brands
    [avatar] [nvarchar](100) NULL,                  -- ✅ Ảnh đại diện
    [active] [bit] NULL                             -- ✅ Trạng thái
)
```

**💡 KHAI THÁC:**
- ✅ **Tìm kiếm theo màn hình**: "Laptop màn hình 16 inch", "QHD+", "240Hz"
- ✅ **Tìm kiếm theo trọng lượng**: "Laptop nhẹ dưới 2kg", "Laptop mỏng nhẹ"
- ✅ **Tìm kiếm theo pin**: "Pin lâu", "Pin 99Wh"
- ✅ **Tính % giảm giá**: `(original_selling_price - selling_price) / original_selling_price * 100`
- ✅ **So sánh giá**: "Laptop dưới 20 triệu", "Từ 20-30 triệu"
- ✅ **Bảo hành**: "Bảo hành 36 tháng", "Bảo hành lâu"

---

### 2. **Bảng `ProductConfiguration`** - Cấu hình chi tiết
```sql
CREATE TABLE [dbo].[ProductConfiguration](
    [configuration_id] [nvarchar](20) NOT NULL,
    [cpu] [nvarchar](50) NULL,                      -- ✅ CPU (ví dụ: "Core i5-11800H")
    [ram] [nvarchar](50) NULL,                      -- ✅ RAM (ví dụ: "8GB", "16GB")
    [rom] [nvarchar](50) NULL,                     -- ✅ Ổ cứng (ví dụ: "512GB SSD", "1TB SSD")
    [card] [nvarchar](50) NULL,                    -- ✅ Card đồ họa (ví dụ: "RTX 3050 4GB")
    [price] [decimal](18, 2) NULL,                  -- ✅ Giá cấu hình (có thể khác base price)
    [product_id] [nvarchar](20) NULL,               -- ✅ FK → Product
    [quantity] [int] NULL                           -- ✅ Số lượng tồn kho
)
```

**💡 KHAI THÁC:**
- ✅ **Tìm kiếm theo CPU**: "Core i5", "i7", "i9", "Ryzen 7"
- ✅ **Tìm kiếm theo RAM**: "RAM 16GB", "RAM 32GB"
- ✅ **Tìm kiếm theo ổ cứng**: "SSD 512GB", "SSD 1TB"
- ✅ **Tìm kiếm theo card đồ họa**: "RTX 3050", "RTX 4060", "Gaming"
- ✅ **Kiểm tra tồn kho**: "Còn hàng không?", "Có sẵn không?"
- ✅ **So sánh cấu hình**: "Cấu hình nào tốt hơn?"

---

### 3. **Bảng `Brands`** - Thương hiệu
```sql
CREATE TABLE [dbo].[Brands](
    [brand_id] [nvarchar](20) NOT NULL,
    [brand_name] [nvarchar](50) NULL,              -- ✅ Tên thương hiệu
    [active] [bit] NULL                            -- ✅ Trạng thái
)
```

**Dữ liệu mẫu:**
- `B001`: Dell
- `B002`: Lenovo
- `B003`: HP
- `B004`: ASUS

**💡 KHAI THÁC:**
- ✅ **Tìm kiếm theo hãng**: "Laptop Dell", "HP", "Lenovo"
- ✅ **So sánh hãng**: "Dell vs HP", "Hãng nào tốt hơn?"
- ✅ **Gợi ý hãng**: "Em có laptop hãng nào?"

---

### 4. **Bảng `ProductReview`** - Đánh giá sản phẩm
```sql
CREATE TABLE [dbo].[ProductReview](
    [productReview_id] [nvarchar](20) NOT NULL,
    [content_detail] [nvarchar](max) NULL,         -- ✅ Nội dung đánh giá
    [rate] [int] NULL,                             -- ✅ Điểm đánh giá (1-5)
    [customer_id] [nvarchar](20) NULL,             -- ✅ FK → Customer
    [time] [datetime] NULL,                        -- ✅ Thời gian đánh giá
    [product_id] [nvarchar](20) NULL               -- ✅ FK → Product
)
```

**💡 KHAI THÁC:**
- ✅ **Hiển thị đánh giá**: "Sản phẩm này được đánh giá 4.5/5 sao"
- ✅ **Trích dẫn review**: "Khách hàng nói: 'Sản phẩm rất tốt...'"
- ✅ **Sắp xếp theo rating**: "Sản phẩm được đánh giá cao nhất"
- ✅ **Phân tích sentiment**: Tích cực/Tiêu cực từ content_detail

---

### 5. **Bảng `Promotion`** - Khuyến mãi
```sql
CREATE TABLE [dbo].[Promotion](
    [promotion_id] [nvarchar](20) NOT NULL,
    [product_id] [nvarchar](20) NULL,              -- ✅ FK → Product
    [type] [nvarchar](50) NULL,                    -- ✅ Loại KM (ví dụ: "Giảm giá", "Freeship")
    [content_detail] [nvarchar](200) NULL          -- ✅ Chi tiết KM
)
```

**Dữ liệu mẫu:**
- `KM001`: P001 - "Giảm giá 10%"
- `KM002`: P003 - "Freeship"

**💡 KHAI THÁC:**
- ✅ **Thông báo khuyến mãi**: "Sản phẩm này đang có khuyến mãi: Giảm giá 10%"
- ✅ **Tìm sản phẩm có KM**: "Laptop nào đang giảm giá?"
- ✅ **Tổng hợp KM**: "Hiện có 5 sản phẩm đang khuyến mãi"

---

### 6. **Bảng `ProductImage`** - Ảnh sản phẩm
```sql
CREATE TABLE [dbo].[ProductImage](
    [image_id] [nvarchar](20) NOT NULL,
    [product_id] [nvarchar](20) NULL               -- ✅ FK → Product
)
```

**💡 KHAI THÁC:**
- ✅ **Hiển thị nhiều ảnh**: "Xem thêm ảnh sản phẩm"
- ✅ **Gallery**: Carousel ảnh trong chat

---

## 🚀 ĐỀ XUẤT CẢI TIẾN CHATBOT

### **1. Tìm kiếm nâng cao theo đặc điểm sản phẩm**

#### **A. Tìm kiếm theo màn hình:**
```csharp
// Thêm vào GuidedChatService hoặc ProductService
public async Task<List<Product>> SearchByScreenAsync(string screenQuery)
{
    // Parse: "16 inch", "QHD+", "240Hz", "OLED"
    var query = _dbContext.Products
        .Where(p => p.Screen != null && 
                   (p.Screen.Contains("16") || 
                    p.Screen.Contains("QHD") || 
                    p.Screen.Contains("240Hz")))
        .ToListAsync();
    return query;
}
```

**Ví dụ chatbot:**
- User: "Laptop màn hình 16 inch"
- Bot: "Em có các laptop màn hình 16 inch: Dell Inspiron 14 Slim, Lenovo ThinkPad X16..."

#### **B. Tìm kiếm theo trọng lượng:**
```csharp
public async Task<List<Product>> SearchByWeightAsync(decimal maxWeight)
{
    return await _dbContext.Products
        .Where(p => p.Weight != null && p.Weight <= maxWeight)
        .OrderBy(p => p.Weight)
        .ToListAsync();
}
```

**Ví dụ chatbot:**
- User: "Laptop nhẹ dưới 2kg"
- Bot: "Em có các laptop nhẹ: Dell XPS 14 Carbon (1.17kg), Lenovo ThinkPad T14 (1.70kg)..."

#### **C. Tìm kiếm theo pin:**
```csharp
public async Task<List<Product>> SearchByBatteryAsync(string batteryQuery)
{
    // Parse: "lâu", "99Wh", "pin tốt"
    return await _dbContext.Products
        .Where(p => p.Pin != null && 
                   (p.Pin.Contains("99") || p.Pin.Contains("100")))
        .OrderByDescending(p => p.Pin)
        .ToListAsync();
}
```

---

### **2. Tính toán và hiển thị % giảm giá**

```csharp
public class ProductWithDiscount
{
    public Product Product { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
}

public List<ProductWithDiscount> CalculateDiscounts(List<Product> products)
{
    return products.Select(p => new ProductWithDiscount
    {
        Product = p,
        DiscountPercent = p.OriginalSellingPrice > 0 
            ? ((p.OriginalSellingPrice.Value - p.SellingPrice.Value) / p.OriginalSellingPrice.Value) * 100
            : 0,
        DiscountAmount = p.OriginalSellingPrice.HasValue && p.SellingPrice.HasValue
            ? p.OriginalSellingPrice.Value - p.SellingPrice.Value
            : 0
    })
    .Where(p => p.DiscountPercent > 0)
    .OrderByDescending(p => p.DiscountPercent)
    .ToList();
}
```

**Ví dụ chatbot:**
- Bot: "**Dell Alienware 16X Aurora** - Giá gốc: 72.000.000đ, Giá bán: 68.990.000đ"
- Bot: "💰 **Tiết kiệm 4.2%** (3.010.000đ) - Đây là deal tốt!"

---

### **3. Tích hợp đánh giá sản phẩm**

```csharp
public class ProductWithRating
{
    public Product Product { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? TopReview { get; set; }
}

public async Task<ProductWithRating> GetProductWithRatingAsync(string productId)
{
    var product = await _dbContext.Products
        .Include(p => p.ProductReviews)
        .FirstOrDefaultAsync(p => p.ProductId == productId);

    if (product == null) return null;

    var reviews = product.ProductReviews?.ToList() ?? new List<ProductReview>();
    var avgRating = reviews.Any() 
        ? reviews.Average(r => r.Rate ?? 0) 
        : 0;
    var topReview = reviews
        .OrderByDescending(r => r.Rate)
        .FirstOrDefault()?.ContentDetail;

    return new ProductWithRating
    {
        Product = product,
        AverageRating = avgRating,
        ReviewCount = reviews.Count,
        TopReview = topReview
    };
}
```

**Ví dụ chatbot:**
- Bot: "**Dell Alienware 16X Aurora** ⭐ **4.5/5** (12 đánh giá)"
- Bot: "💬 *Khách hàng nói: 'Laptop gaming rất mạnh, màn hình đẹp, pin tốt. Đáng giá tiền!'*"

---

### **4. Thông báo khuyến mãi tự động**

```csharp
public async Task<List<Product>> GetProductsWithPromotionAsync()
{
    return await _dbContext.Products
        .Include(p => p.Promotions)
        .Where(p => p.Promotions != null && p.Promotions.Any())
        .ToListAsync();
}

// Trong chatbot response
if (product.Promotions?.Any() == true)
{
    var promotion = product.Promotions.First();
    response += $"\n🎉 **KHUYẾN MÃI**: {promotion.Type} - {promotion.ContentDetail}";
}
```

**Ví dụ chatbot:**
- Bot: "**Dell Alienware 16X Aurora** - 68.990.000đ"
- Bot: "🎉 **KHUYẾN MÃI**: Giảm giá 10% + Freeship toàn quốc!"

---

### **5. Tìm kiếm thông minh theo use case**

```csharp
public class UseCaseRecommendation
{
    public string UseCase { get; set; } // "gaming", "office", "design", "student"
    public List<string> RequiredSpecs { get; set; }
}

public async Task<List<Product>> RecommendByUseCaseAsync(string useCase)
{
    var query = _dbContext.Products
        .Include(p => p.ProductConfigurations)
        .AsQueryable();

    switch (useCase.ToLower())
    {
        case "gaming":
            query = query.Where(p => 
                p.ProductConfigurations.Any(c => 
                    c.Card != null && 
                    (c.Card.Contains("RTX") || c.Card.Contains("GTX"))));
            break;
        case "office":
        case "văn phòng":
            query = query.Where(p => 
                p.Weight <= 2.0 && 
                p.ProductConfigurations.Any(c => 
                    c.Ram != null && c.Ram.Contains("8GB")));
            break;
        case "design":
        case "đồ họa":
            query = query.Where(p => 
                p.Screen != null && 
                (p.Screen.Contains("4K") || p.Screen.Contains("OLED")) &&
                p.ProductConfigurations.Any(c => 
                    c.Ram != null && c.Ram.Contains("16GB")));
            break;
        case "student":
        case "học sinh":
            query = query.Where(p => 
                p.SellingPrice <= 20000000 && 
                p.Weight <= 2.0);
            break;
    }

    return await query.Take(5).ToListAsync();
}
```

**Ví dụ chatbot:**
- User: "Laptop cho gaming"
- Bot: "Em gợi ý các laptop gaming tốt nhất:"
- Bot: "1. **Dell Alienware 16X Aurora** - RTX 3050, 16 inch QHD+ 240Hz"
- Bot: "2. **Lenovo Legion 7 Pro** - RTX 4060, 14 inch OLED"

---

### **6. So sánh sản phẩm**

```csharp
public class ProductComparison
{
    public Product Product1 { get; set; }
    public Product Product2 { get; set; }
    public Dictionary<string, string> Differences { get; set; }
}

public ProductComparison CompareProducts(string productId1, string productId2)
{
    var p1 = _dbContext.Products
        .Include(p => p.ProductConfigurations)
        .FirstOrDefault(p => p.ProductId == productId1);
    var p2 = _dbContext.Products
        .Include(p => p.ProductConfigurations)
        .FirstOrDefault(p => p.ProductId == productId2);

    var differences = new Dictionary<string, string>();
    
    if (p1.SellingPrice != p2.SellingPrice)
        differences["Giá"] = $"{p1.SellingPrice:N0}đ vs {p2.SellingPrice:N0}đ";
    
    if (p1.Screen != p2.Screen)
        differences["Màn hình"] = $"{p1.Screen} vs {p2.Screen}";
    
    // ... so sánh CPU, RAM, Card từ ProductConfiguration

    return new ProductComparison
    {
        Product1 = p1,
        Product2 = p2,
        Differences = differences
    };
}
```

**Ví dụ chatbot:**
- User: "So sánh Dell Alienware vs Lenovo Legion"
- Bot: "**So sánh Dell Alienware 16X vs Lenovo Legion 7 Pro:**"
- Bot: "💰 **Giá**: 68.990.000đ vs 51.990.000đ"
- Bot: "🖥️ **Màn hình**: 16 inch QHD+ 240Hz vs 14 inch OLED"
- Bot: "⚡ **CPU**: Core i5-11800H vs Core i7-11800H"
- Bot: "💾 **RAM**: 8GB vs 16GB"

---

### **7. Kiểm tra tồn kho thời gian thực**

```csharp
public async Task<bool> CheckStockAsync(string productId, string? specifications = null)
{
    var query = _dbContext.ProductConfigurations
        .Where(c => c.ProductId == productId);

    if (!string.IsNullOrEmpty(specifications))
    {
        query = query.Where(c => c.ConfigurationId == specifications);
    }

    var config = await query.FirstOrDefaultAsync();
    return config != null && config.Quantity > 0;
}
```

**Ví dụ chatbot:**
- User: "Dell Alienware còn hàng không?"
- Bot: "✅ **Còn hàng!** Hiện có 2 sản phẩm trong kho."
- Bot: "⚠️ **Hết hàng!** Sản phẩm này đã hết, em có thể đặt trước không?"

---

### **8. Gợi ý sản phẩm tương tự**

```csharp
public async Task<List<Product>> GetSimilarProductsAsync(string productId, int count = 5)
{
    var product = await _dbContext.Products
        .Include(p => p.ProductConfigurations)
        .FirstOrDefaultAsync(p => p.ProductId == productId);

    if (product == null) return new List<Product>();

    var brandId = product.BrandId;
    var priceRange = product.SellingPrice ?? 0;
    var minPrice = priceRange * 0.8m;
    var maxPrice = priceRange * 1.2m;

    return await _dbContext.Products
        .Where(p => p.ProductId != productId &&
                   p.BrandId == brandId &&
                   p.SellingPrice >= minPrice &&
                   p.SellingPrice <= maxPrice &&
                   p.Active == true)
        .Take(count)
        .ToListAsync();
}
```

**Ví dụ chatbot:**
- Bot: "**Sản phẩm tương tự:**"
- Bot: "1. Dell Alienware M17 R8 Pro - 81.990.000đ"
- Bot: "2. Dell Alienware X17 Phantom - 86.990.000đ"

---

## 📝 IMPLEMENTATION CHECKLIST

### **Phase 1: Tìm kiếm cơ bản (Đã có)**
- [x] Tìm theo Brand
- [x] Tìm theo CPU
- [x] Tìm theo RAM
- [x] Tìm theo Storage
- [x] Tìm theo khoảng giá

### **Phase 2: Tìm kiếm nâng cao (Cần thêm)**
- [ ] Tìm theo màn hình (screen)
- [ ] Tìm theo trọng lượng (weight)
- [ ] Tìm theo pin (battery)
- [ ] Tìm theo bảo hành (warranty_period)
- [ ] Tìm theo cổng kết nối (connect)

### **Phase 3: Tính năng thông minh (Cần thêm)**
- [ ] Tính % giảm giá
- [ ] Hiển thị đánh giá (rating)
- [ ] Thông báo khuyến mãi
- [ ] Kiểm tra tồn kho
- [ ] Gợi ý sản phẩm tương tự

### **Phase 4: Tư vấn theo use case (Cần thêm)**
- [ ] Gaming laptop
- [ ] Văn phòng
- [ ] Đồ họa/Design
- [ ] Học sinh/Sinh viên
- [ ] Lập trình

### **Phase 5: So sánh sản phẩm (Cần thêm)**
- [ ] So sánh 2 sản phẩm
- [ ] So sánh nhiều sản phẩm
- [ ] Bảng so sánh chi tiết

---

## 🎯 KẾT LUẬN

Database có **rất nhiều dữ liệu phong phú** chưa được khai thác:
- ✅ **Product**: screen, weight, pin, connect, warranty
- ✅ **ProductConfiguration**: CPU, RAM, ROM, Card, Quantity
- ✅ **ProductReview**: Rating, Content
- ✅ **Promotion**: Type, Content

**Chatbot hiện tại chỉ dùng ~30% dữ liệu!**

Với các cải tiến trên, chatbot sẽ:
- 🚀 **Tư vấn chính xác hơn** (theo use case)
- 💰 **Hiển thị giá trị tốt hơn** (% giảm giá)
- ⭐ **Tin cậy hơn** (đánh giá khách hàng)
- 🎁 **Hấp dẫn hơn** (khuyến mãi)
- 📊 **So sánh thông minh** (nhiều tiêu chí)

---

**📌 Ưu tiên triển khai:**
1. **Tính % giảm giá** (dễ, impact cao)
2. **Tìm kiếm theo màn hình/trọng lượng** (dễ, hữu ích)
3. **Tư vấn theo use case** (trung bình, rất hữu ích)
4. **Tích hợp đánh giá** (trung bình, tăng trust)
5. **So sánh sản phẩm** (khó, nhưng rất giá trị)

