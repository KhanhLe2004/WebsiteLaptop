using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebLaptopBE.Services;

/// <summary>
/// Service để làm việc với Qdrant (vector database)
/// Qdrant dùng để lưu trữ và tìm kiếm policy documents bằng semantic search
/// </summary>
public class QdrantService : IQdrantService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QdrantService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _qdrantUrl;
    private readonly string _openAiApiKey;

    /// <summary>
    /// Constructor - Nhận các dependencies từ Dependency Injection
    /// </summary>
    public QdrantService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QdrantService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
        _logger = logger;
        
        // Lấy URL của Qdrant từ config (mặc định: http://localhost:6333)
        _qdrantUrl = _configuration["Qdrant:Url"] ?? "http://localhost:6333";
        
        // Lấy OpenAI API Key từ config (cần thiết để tạo embeddings)
        _openAiApiKey = _configuration["OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI:ApiKey không được cấu hình trong appsettings.json");
        
        _httpClient.BaseAddress = new Uri(_qdrantUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Kiểm tra xem collection đã tồn tại chưa
    /// </summary>
    public async Task<bool> CollectionExistsAsync(string collectionName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/collections/{collectionName}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking collection existence: {CollectionName}", collectionName);
            return false;
        }
    }

    /// <summary>
    /// Tạo collection mới trong Qdrant
    /// Collection giống như một bảng trong database, nhưng dùng để lưu vectors
    /// </summary>
    public async Task CreateCollectionAsync(string collectionName)
    {
        try
        {
            // Kiểm tra xem collection đã tồn tại chưa
            if (await CollectionExistsAsync(collectionName))
            {
                _logger.LogInformation("Collection {CollectionName} đã tồn tại", collectionName);
                return;
            }

            // Tạo request body để tạo collection
            var requestBody = new
            {
                vectors = new
                {
                    size = 1536, // OpenAI ada-002 embedding size (1536 dimensions)
                    distance = "Cosine" // Dùng cosine similarity để so sánh vectors
                }
            };

            var response = await _httpClient.PutAsJsonAsync($"/collections/{collectionName}", requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Đã tạo collection: {CollectionName}", collectionName);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Lỗi khi tạo collection: {Error}", errorContent);
                throw new Exception($"Không thể tạo collection: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection: {CollectionName}", collectionName);
            throw;
        }
    }

    /// <summary>
    /// Thêm policy document vào Qdrant
    /// Bước 1: Tạo embedding từ text bằng OpenAI
    /// Bước 2: Lưu embedding + metadata vào Qdrant
    /// </summary>
    public async Task InsertPolicyAsync(string collectionName, string policyText, Dictionary<string, object> metadata)
    {
        try
        {
            // Bước 1: Tạo embedding từ text
            var embedding = await GenerateEmbeddingAsync(policyText);
            
            // Bước 2: Tạo point ID (unique identifier)
            var pointId = Guid.NewGuid().ToString();
            
            // Bước 3: Chuẩn bị payload (metadata + content)
            var payload = new Dictionary<string, object>(metadata)
            {
                ["content"] = policyText // Lưu cả text gốc để có thể đọc lại
            };

            // Bước 4: Tạo request body để insert vào Qdrant
            var requestBody = new
            {
                points = new[]
                {
                    new
                    {
                        id = pointId,
                        vector = embedding,
                        payload = payload
                    }
                }
            };

            // Bước 5: Gửi request đến Qdrant
            var response = await _httpClient.PutAsJsonAsync($"/collections/{collectionName}/points", requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Đã thêm policy vào Qdrant: {PointId}", pointId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Lỗi khi thêm policy: {Error}", errorContent);
                throw new Exception($"Không thể thêm policy: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting policy into Qdrant");
            throw;
        }
    }

    /// <summary>
    /// Tìm kiếm policy documents dựa trên câu hỏi của user
    /// Bước 1: Tạo embedding từ câu hỏi
    /// Bước 2: Tìm kiếm vectors tương tự trong Qdrant
    /// Bước 3: Trả về top K results
    /// </summary>
    public async Task<List<PolicySearchResult>> SearchPoliciesAsync(string collectionName, string query, int limit = 3)
    {
        try
        {
            // Bước 1: Tạo embedding từ câu hỏi
            var queryEmbedding = await GenerateEmbeddingAsync(query);
            
            // Bước 2: Tạo request body để search
            var requestBody = new
            {
                vector = queryEmbedding,
                limit = limit,
                with_payload = true // Lấy cả metadata
            };

            // Bước 3: Gửi request đến Qdrant
            var response = await _httpClient.PostAsJsonAsync($"/collections/{collectionName}/points/search", requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Lỗi khi tìm kiếm: {Error}", errorContent);
                return new List<PolicySearchResult>();
            }

            // Bước 4: Parse kết quả
            var result = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>();
            
            if (result?.Result == null)
            {
                return new List<PolicySearchResult>();
            }

            // Bước 5: Chuyển đổi sang PolicySearchResult
            var searchResults = new List<PolicySearchResult>();
            foreach (var item in result.Result)
            {
                string content = string.Empty;
                var metadata = new Dictionary<string, object>();
                
                if (item.Payload.HasValue)
                {
                    var payloadElement = item.Payload.Value;
                    
                    // Lấy content
                    if (payloadElement.ValueKind == JsonValueKind.Object && 
                        payloadElement.TryGetProperty("content", out var contentElement))
                    {
                        content = contentElement.GetString() ?? string.Empty;
                    }
                    
                    // Lấy metadata (tất cả trừ content)
                    if (payloadElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in payloadElement.EnumerateObject())
                        {
                            if (prop.Name != "content")
                            {
                                // Convert JsonElement sang object
                                if (prop.Value.ValueKind == JsonValueKind.String)
                                {
                                    metadata[prop.Name] = prop.Value.GetString() ?? string.Empty;
                                }
                                else if (prop.Value.ValueKind == JsonValueKind.Number)
                                {
                                    metadata[prop.Name] = prop.Value.GetDecimal();
                                }
                                else
                                {
                                    metadata[prop.Name] = prop.Value.ToString();
                                }
                            }
                        }
                    }
                }

                searchResults.Add(new PolicySearchResult
                {
                    Content = content,
                    Score = item.Score,
                    Metadata = metadata
                });
            }

            _logger.LogInformation("Tìm thấy {Count} policy documents", searchResults.Count);
            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching policies in Qdrant");
            return new List<PolicySearchResult>();
        }
    }

    /// <summary>
    /// Tạo embedding từ text bằng OpenAI API
    /// Embedding là một mảng số (vector) đại diện cho ý nghĩa của text
    /// </summary>
    private async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            // Tạo request đến OpenAI Embedding API
            var request = new
            {
                input = text,
                model = "text-embedding-ada-002" // Model của OpenAI để tạo embeddings
            };

            // Set header với API key
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");
            _httpClient.BaseAddress = new Uri("https://api.openai.com");

            // Gửi request
            var response = await _httpClient.PostAsJsonAsync("/v1/embeddings", request);
            response.EnsureSuccessStatusCode();

            // Parse response
            var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();
            
            if (result?.Data == null || result.Data.Count == 0)
            {
                throw new Exception("Không nhận được embedding từ OpenAI");
            }

            // Trả về embedding vector
            return result.Data[0].Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
        finally
        {
            // Reset base address về Qdrant
            _httpClient.BaseAddress = new Uri(_qdrantUrl);
        }
    }

    // Classes để parse JSON response từ OpenAI và Qdrant
    private class OpenAIEmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = new();
    }

    private class EmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    private class QdrantSearchResponse
    {
        [JsonPropertyName("result")]
        public List<QdrantSearchResult>? Result { get; set; }
    }

    private class QdrantSearchResult
    {
        [JsonPropertyName("score")]
        public float Score { get; set; }
        
        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; set; }
    }
}



