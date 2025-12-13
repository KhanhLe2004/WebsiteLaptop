# 📚 Giải Thích Cách Hoạt Động Của Chatbox AI

## 🎯 Tổng Quan

Chatbox AI của bạn sử dụng công nghệ **RAG (Retrieval-Augmented Generation)** - một kỹ thuật AI hiện đại kết hợp:
- **Vector Search** (Qdrant) để tìm kiếm thông tin liên quan
- **Large Language Model** (OpenAI GPT) để tạo phản hồi tự nhiên
- **SQL Database** để lấy thông tin chi tiết sản phẩm

---

## 🔄 Flow Hoạt Động Tổng Thể

```
┌─────────────┐
│   USER      │
│  (Frontend) │
└──────┬──────┘
       │ 1. User gửi message
       │    "Tôi cần laptop gaming"
       ▼
┌─────────────────────────────────────┐
│   Frontend (chatbot.js)              │
│   - Validate input                   │
│   - Gửi POST /api/chat/ai            │
│   - Hiển thị typing indicator       │
└──────┬──────────────────────────────┘
       │ 2. HTTP Request
       ▼
┌─────────────────────────────────────┐
│   ChatController.cs                 │
│   - Validate request                │
│   - Gọi RAGChatService              │
└──────┬──────────────────────────────┘
       │ 3. Process Message
       ▼
┌─────────────────────────────────────┐
│   RAGChatService.cs                 │
│   (Core Logic)                      │
└──────┬──────────────────────────────┘
       │
       ├─► 4a. Input Validation
       │   └─► InputValidationService
       │
       ├─► 4b. Vector Search (Song song)
       │   ├─► QdrantVectorService.SearchProductsAsync()
       │   │   └─► Tìm sản phẩm liên quan
       │   └─► QdrantVectorService.SearchPoliciesAsync()
       │       └─► Tìm chính sách liên quan
       │
       ├─► 4c. Build Context
       │   ├─► BuildProductContext() - Format thông tin sản phẩm
       │   └─► BuildPolicyContext() - Format thông tin chính sách
       │
       ├─► 4d. Generate Response
       │   └─► SemanticKernelService.GenerateResponseAsync()
       │       └─► Gọi OpenAI GPT với prompt đầy đủ
       │
       └─► 4e. Parse Products
           └─► ProductService.GetProductsByIdsAsync()
               └─► Lấy thông tin chi tiết từ SQL Database
       │
       ▼
┌─────────────────────────────────────┐
│   Response                          │
│   - Answer: Text response từ AI     │
│   - SuggestedProducts: Danh sách   │
│     sản phẩm với ảnh, giá, link    │
└──────┬──────────────────────────────┘
       │ 5. HTTP Response
       ▼
┌─────────────────────────────────────┐
│   Frontend (chatbot.js)             │
│   - Hiển thị answer                 │
│   - Render product suggestions      │
│   - Hiển thị ảnh, giá, link         │
└─────────────────────────────────────┘
```

---

## 🧩 Các Thành Phần Chính

### 1. **Frontend (JavaScript)**

**File:** `WebsiteLaptop/WebLaptopFE/wwwroot/js/chatbot.js`

**Chức năng:**
- Quản lý UI của chatbox (mở/đóng, hiển thị tin nhắn)
- Gửi request đến backend API
- Xử lý retry logic (thử lại 2 lần nếu fail)
- Hiển thị typing indicator
- Render sản phẩm gợi ý với ảnh, giá, link

**Flow:**
```javascript
User nhập message
  ↓
sendMessage()
  ↓
Validate input
  ↓
POST /api/chat/ai với { message, customerId }
  ↓
Nhận response { answer, suggestedProducts }
  ↓
Hiển thị answer + renderProductSuggestions()
```

---

### 2. **Backend Controller**

**File:** `WebsiteLaptop/WebLaptopBE/Controllers/ChatController.cs`

**Endpoint chính:** `POST /api/chat/ai`

**Chức năng:**
- Validate request (message không rỗng, độ dài < 1000 ký tự)
- Gọi `RAGChatService.ProcessUserMessageAsync()`
- Xử lý lỗi và trả về response

**Code:**
```csharp
[HttpPost("ai")]
public async Task<ActionResult<RAGChatResponse>> ChatAI([FromBody] RAGChatRequest request)
{
    // Validate
    if (string.IsNullOrWhiteSpace(request.Message))
        return BadRequest(...);
    
    // Xử lý bằng RAG
    var response = await _ragChatService.ProcessUserMessageAsync(
        request.Message, 
        request.CustomerId
    );
    
    return Ok(response);
}
```

---

### 3. **RAG Chat Service** (Core Logic)

**File:** `WebsiteLaptop/WebLaptopBE/Services/RAGChatService.cs`

Đây là **trái tim** của hệ thống, thực hiện RAG pipeline:

#### **Bước 0: Input Validation**
```csharp
var validationResult = _inputValidationService.ValidateUserInput(userMessage);
```
- Kiểm tra input có hợp lệ không (không spam, không chứa ký tự đặc biệt)
- Nếu không hợp lệ → trả về message cảnh báo

#### **Bước 1 & 2: Vector Search (Song song)**
```csharp
// Chạy song song để tối ưu thời gian
var productSearchTask = SearchProductsWithFallbackAsync(userMessage);
var policySearchTask = _qdrantVectorService.SearchPoliciesAsync(userMessage, topK: 3);

// Đợi cả 2 hoàn thành (timeout 8 giây)
var combinedTask = Task.WhenAll(productSearchTask, policySearchTask);
```

**Vector Search hoạt động như thế nào:**
1. Convert user message → **Embedding vector** (dùng OpenAI API)
2. Search trong Qdrant (vector database) để tìm:
   - **Sản phẩm** có embedding gần nhất với câu hỏi
   - **Chính sách** có embedding gần nhất với câu hỏi
3. Trả về top K kết quả (topK: 5 cho products, topK: 3 cho policies)

**Fallback mechanism:**
- Nếu Qdrant fail → Search từ SQL Database
- Nếu không tìm thấy policies → Dùng fallback policies từ `PolicyData`

#### **Bước 3: Build Context**
```csharp
var productContext = BuildProductContext(productResults);
var policyContext = BuildPolicyContext(policyResults);
```

**BuildProductContext()** format thông tin sản phẩm:
```
Tìm thấy 3 sản phẩm liên quan:

1. **Laptop Gaming ASUS ROG**
   Thương hiệu: ASUS
   Giá: 25,000,000 VND
   Phân khúc: Cao cấp, phù hợp gaming và đồ họa
   Cấu hình:
     • CPU: Intel Core i7-12700H
     • RAM: 16GB DDR4
     • Ổ cứng: 512GB SSD
     • Card đồ họa: RTX 3060
   Bảo hành: 12 tháng
   Điểm nổi bật: CPU mạnh, Card đồ họa rời, gaming tốt
```

**BuildPolicyContext()** format thông tin chính sách (FULL TEXT):
```
=== THÔNG TIN CHÍNH SÁCH (FULL TEXT) ===

CHÍNH SÁCH BẢO HÀNH TẠI TENTECH
...
```

#### **Bước 4: Generate Response với LLM**
```csharp
var systemPrompt = BuildSystemPrompt(); // Hướng dẫn AI cách trả lời
var userPrompt = BuildUserPrompt(userMessage, productContext, policyContext);

var response = await _semanticKernelService.GenerateResponseAsync(fullPrompt);
```

**System Prompt** định nghĩa:
- Vai trò: Nhân viên tư vấn bán laptop chuyên nghiệp
- Phong cách: Thân thiện, chuyên nghiệp, xưng "em" với khách
- Quy tắc: Luôn hỏi rõ nhu cầu, đề xuất 2-3 sản phẩm, so sánh khách quan

**User Prompt** chứa:
- Câu hỏi của khách hàng
- Thông tin sản phẩm tìm được (productContext)
- Thông tin chính sách (policyContext)
- Hướng dẫn trả lời dựa trên intent (product_search, policy_inquiry, ...)

**LLM (OpenAI GPT-4o-mini)** sẽ:
- Đọc context (sản phẩm + chính sách)
- Tạo phản hồi tự nhiên, phù hợp với vai trò nhân viên tư vấn
- Trả về text response

#### **Bước 5: Parse Suggested Products**
```csharp
var productDTOs = await ParseSuggestedProductsAsync(productResults);
// Lấy product IDs từ vector search results
// Query SQL Database để lấy thông tin chi tiết (ảnh, giá, link)
```

**ConvertToProductSuggestions()** build:
- **ImageUrl**: `{backendUrl}/imageProducts/{avatar hoặc ImageId}`
- **DetailUrl**: `{frontendUrl}/Home/ProductDetail?id={productId}`
- **Price, Name, Brand, Cpu, Ram, Storage**

---

### 4. **Qdrant Vector Service**

**File:** `WebsiteLaptop/WebLaptopBE/Services/QdrantVectorService.cs`

**Chức năng:**
- Quản lý vector database (Qdrant)
- Tạo embeddings từ text (dùng OpenAI API)
- Search vectors tương tự

**Collections:**
- `laptops_collection`: Chứa embeddings của sản phẩm
- `policies_collection`: Chứa embeddings của chính sách

**Flow Search:**
```
User message: "Laptop gaming giá rẻ"
  ↓
CreateEmbeddingAsync() → OpenAI API
  ↓
Vector: [0.123, -0.456, 0.789, ...] (1536 dimensions)
  ↓
Search trong Qdrant (cosine similarity)
  ↓
Top 5 sản phẩm có embedding gần nhất
```

---

### 5. **Semantic Kernel Service**

**File:** `WebsiteLaptop/WebLaptopBE/AI/SemanticKernel/SemanticKernelService.cs`

**Chức năng:**
- Quản lý kết nối với OpenAI
- Gọi LLM để generate response

**Setup:**
```csharp
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: "gpt-4o-mini",
    apiKey: openAiApiKey
);
_kernel = builder.Build();
```

**Generate Response:**
```csharp
var result = await _kernel.InvokePromptAsync(prompt, arguments);
return result.ToString();
```

---

### 6. **Product Service**

**File:** `WebsiteLaptop/WebLaptopBE/Services/ProductService.cs`

**Chức năng:**
- Query SQL Database để lấy thông tin sản phẩm
- Search products theo nhiều tiêu chí (brand, price, CPU, RAM, ...)
- Load ProductImages vào DTO

**Methods:**
- `SearchProductsAsync()`: Tìm kiếm với criteria
- `GetProductByIdAsync()`: Lấy 1 sản phẩm
- `GetProductsByIdsAsync()`: Lấy nhiều sản phẩm (batch query)

---

## 🎨 Intent Detection

Hệ thống tự động phát hiện **intent** (mục đích) của câu hỏi:

```csharp
private string DetectIntent(string message)
{
    if (message.Contains("so sánh")) → "comparison"
    if (message.Contains("bảo hành")) → "policy_inquiry"
    if (message.Contains("tư vấn")) → "consultation"
    if (message.Contains("giá")) → "price_inquiry"
    else → "product_search"
}
```

Dựa vào intent, system prompt sẽ có hướng dẫn cụ thể:
- **product_search**: Đề xuất 2-3 sản phẩm, giải thích lý do
- **comparison**: So sánh khách quan, nêu điểm mạnh/yếu
- **policy_inquiry**: Hiển thị FULL TEXT chính sách

---

## 🔧 Fallback Mechanisms

Hệ thống có nhiều lớp fallback để đảm bảo luôn hoạt động:

### 1. **Qdrant Fallback**
```
Qdrant search fail
  ↓
Search từ SQL Database
  ↓
Convert kết quả sang VectorSearchResult format
```

### 2. **Policy Fallback**
```
Qdrant không tìm thấy policies
  ↓
Dùng PolicyData.SearchPolicies() (hardcoded policies)
```

### 3. **LLM Fallback**
```
OpenAI API fail hoặc timeout
  ↓
BuildFallbackResponse() từ data có sẵn
  ↓
Vẫn trả về thông tin sản phẩm/chính sách
```

### 4. **Timeout Protection**
- Vector search: 8 giây timeout
- LLM generation: 10 giây timeout
- Frontend request: 15 giây timeout

---

## 📊 Data Flow Chi Tiết

### Ví dụ: User hỏi "Laptop Dell giá dưới 20 triệu"

```
1. Frontend gửi:
   POST /api/chat/ai
   { "message": "Laptop Dell giá dưới 20 triệu", "customerId": null }

2. RAGChatService.ProcessUserMessageAsync():
   
   a) Input Validation ✅
   
   b) Vector Search (song song):
      - SearchProductsAsync("Laptop Dell giá dưới 20 triệu")
        → Qdrant tìm 5 sản phẩm Dell có embedding gần nhất
        → Kết quả: [Product1, Product2, Product3, ...]
      
      - SearchPoliciesAsync(...)
        → Không tìm thấy policies liên quan
        → Kết quả: []
   
   c) Build Context:
      productContext = """
      Tìm thấy 3 sản phẩm liên quan:
      1. **Dell Inspiron 15 3520**
         Giá: 15,900,000 VND
         ...
      """
      
      policyContext = "Không tìm thấy thông tin chính sách liên quan."
   
   d) Build Prompts:
      systemPrompt = "Bạn là nhân viên tư vấn..."
      userPrompt = """
      Câu hỏi: Laptop Dell giá dưới 20 triệu
      THÔNG TIN SẢN PHẨM:
      [productContext]
      ...
      """
   
   e) Generate Response:
      → Gọi OpenAI GPT-4o-mini
      → LLM đọc context và tạo response:
      "Chào anh/chị! Em có một số laptop Dell phù hợp với ngân sách dưới 20 triệu:
      
      • **Dell Inspiron 15 3520** - 15,900,000 VND
        Cấu hình: Intel Core i5, 8GB RAM, 256GB SSD
        Phù hợp: Văn phòng, học tập
      
      • **Dell Vostro 15 3510** - 18,500,000 VND
        ...
      "
   
   f) Parse Products:
      → GetProductsByIdsAsync(["DELL001", "DELL002", ...])
      → Lấy thông tin chi tiết từ SQL
      → ConvertToProductSuggestions()
      → Build ImageUrl, DetailUrl

3. Response trả về:
   {
     "answer": "Chào anh/chị! Em có một số laptop Dell...",
     "suggestedProducts": [
       {
         "productId": "DELL001",
         "name": "Dell Inspiron 15 3520",
         "price": 15900000,
         "imageUrl": "http://localhost:5068/imageProducts/dell001.jpg",
         "detailUrl": "http://localhost:5253/Home/ProductDetail?id=DELL001",
         ...
       },
       ...
     ]
   }

4. Frontend hiển thị:
   - Text response từ AI
   - Danh sách sản phẩm với ảnh, giá, link "Xem chi tiết"
```

---

## 🚀 Tối Ưu Hóa

### 1. **Parallel Processing**
- Products search và Policies search chạy **song song** (Task.WhenAll)
- Giảm thời gian từ ~6s xuống ~3s

### 2. **Caching**
- Embeddings được cache trong MemoryCache (60 phút)
- Tránh gọi OpenAI API nhiều lần cho cùng 1 text

### 3. **Batch Query**
- `GetProductsByIdsAsync()` query 1 lần thay vì N lần
- Giảm số lượng database queries

### 4. **Timeout Protection**
- Mỗi bước có timeout riêng
- Tránh user phải đợi quá lâu

---

## 🔐 Security & Validation

1. **Input Validation:**
   - Kiểm tra độ dài message (< 1000 ký tự)
   - Filter spam, ký tự đặc biệt

2. **Error Handling:**
   - Không expose internal errors ra client
   - Graceful degradation khi service fail

3. **CORS Configuration:**
   - Chỉ cho phép origins được cấu hình
   - Hỗ trợ credentials cho SignalR

---

## 📝 Tóm Tắt

**Chatbox AI hoạt động theo mô hình RAG:**

1. **Retrieval**: Tìm kiếm thông tin liên quan từ vector database (Qdrant) và SQL
2. **Augmentation**: Kết hợp thông tin tìm được vào prompt
3. **Generation**: Dùng LLM (GPT) để tạo phản hồi tự nhiên dựa trên context

**Ưu điểm:**
- ✅ Trả lời chính xác dựa trên dữ liệu thực tế
- ✅ Tự nhiên, như nhân viên tư vấn thật
- ✅ Có thể gợi ý sản phẩm với ảnh, giá, link
- ✅ Có fallback mechanisms để đảm bảo luôn hoạt động

**Công nghệ sử dụng:**
- **Frontend**: JavaScript (Vanilla JS)
- **Backend**: ASP.NET Core (C#)
- **Vector DB**: Qdrant
- **LLM**: OpenAI GPT-4o-mini (qua Semantic Kernel)
- **Database**: SQL Server

---

## 🎓 Tài Liệu Tham Khảo

- **RAG (Retrieval-Augmented Generation)**: https://arxiv.org/abs/2005.11401
- **Semantic Kernel**: https://learn.microsoft.com/en-us/semantic-kernel/
- **Qdrant**: https://qdrant.tech/documentation/
- **OpenAI Embeddings**: https://platform.openai.com/docs/guides/embeddings

