using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace WebLaptopBE.Services;

/// <summary>
/// Qdrant Vector Service - Mở rộng QdrantService để hỗ trợ products + policies
/// </summary>
public class QdrantVectorService : IQdrantVectorService
{
    private readonly HttpClient _qdrantClient;  // Riêng cho Qdrant
    private readonly HttpClient _openAiClient;   // Riêng cho OpenAI
    private readonly ILogger<QdrantVectorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly string _qdrantUrl;
    private readonly string _openAiApiKey;
    private readonly string _laptopsCollection;
    private readonly string _policiesCollection;
    private const int CACHE_EXPIRATION_MINUTES = 60; // Cache embeddings for 1 hour
    private const int QDRANT_TIMEOUT_SECONDS = 10;
    private const int OPENAI_TIMEOUT_SECONDS = 15;

    public QdrantVectorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QdrantVectorService> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        
        _qdrantUrl = _configuration["Qdrant:Url"] ?? "http://localhost:6333";
        _openAiApiKey = _configuration["OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI:ApiKey không được cấu hình");
        _laptopsCollection = _configuration["Qdrant:LaptopsCollection"] ?? "laptops_collection";
        _policiesCollection = _configuration["Qdrant:PoliciesCollection"] ?? "policies_collection";
        
        // Tạo HttpClient riêng cho Qdrant
        _qdrantClient = httpClientFactory.CreateClient("Qdrant");
        _qdrantClient.BaseAddress = new Uri(_qdrantUrl);
        _qdrantClient.Timeout = TimeSpan.FromSeconds(QDRANT_TIMEOUT_SECONDS);
        
        // Tạo HttpClient riêng cho OpenAI - KHÔNG dùng chung để tránh race condition
        _openAiClient = httpClientFactory.CreateClient("OpenAI");
        _openAiClient.BaseAddress = new Uri("https://api.openai.com");
        _openAiClient.Timeout = TimeSpan.FromSeconds(OPENAI_TIMEOUT_SECONDS);
        _openAiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");
    }

    public async Task<bool> CollectionExistsAsync(string collectionName)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _qdrantClient.GetAsync($"/collections/{collectionName}", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout checking collection: {CollectionName}", collectionName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking collection: {CollectionName}", collectionName);
            return false;
        }
    }

    public async Task CreateCollectionAsync(string collectionName)
    {
        try
        {
            if (await CollectionExistsAsync(collectionName))
            {
                _logger.LogInformation("Collection {CollectionName} đã tồn tại", collectionName);
                return;
            }

            var requestBody = new
            {
                vectors = new
                {
                    size = 1536,
                    distance = "Cosine"
                }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _qdrantClient.PutAsJsonAsync($"/collections/{collectionName}", requestBody, cts.Token);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Đã tạo collection: {CollectionName}", collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection: {CollectionName}", collectionName);
            throw;
        }
    }

    public async Task UpsertProductAsync(ProductEmbedding product)
    {
        try
        {
            var pointBody = new
            {
                points = new[]
                {
                    new
                    {
                        id = product.ProductId,
                        vector = product.Embedding,
                        payload = new Dictionary<string, object>(product.Metadata)
                        {
                            ["name"] = product.Name,
                            ["description"] = product.Description,
                            ["productId"] = product.ProductId
                        }
                    }
                }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _qdrantClient.PutAsJsonAsync($"/collections/{_laptopsCollection}/points", pointBody, cts.Token);
            response.EnsureSuccessStatusCode();
            
            _logger.LogDebug("Upserted product: {ProductId}", product.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting product: {ProductId}", product.ProductId);
            throw;
        }
    }

    public async Task UpsertPolicyAsync(PolicyEmbedding policy)
    {
        try
        {
            var pointBody = new
            {
                points = new[]
                {
                    new
                    {
                        id = policy.PolicyId,
                        vector = policy.Embedding,
                        payload = new Dictionary<string, object>(policy.Metadata)
                        {
                            ["content"] = policy.Content,
                            ["policyId"] = policy.PolicyId
                        }
                    }
                }
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await _qdrantClient.PutAsJsonAsync($"/collections/{_policiesCollection}/points", pointBody, cts.Token);
            response.EnsureSuccessStatusCode();
            
            _logger.LogDebug("Upserted policy: {PolicyId}", policy.PolicyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting policy: {PolicyId}", policy.PolicyId);
            throw;
        }
    }

    public async Task<List<VectorSearchResult>> SearchProductsAsync(string query, int topK = 5)
    {
        return await SearchAsync(_laptopsCollection, query, topK);
    }

    public async Task<List<VectorSearchResult>> SearchPoliciesAsync(string query, int topK = 3)
    {
        return await SearchAsync(_policiesCollection, query, topK);
    }

    private async Task<List<VectorSearchResult>> SearchAsync(string collectionName, string query, int topK)
    {
        try
        {
            _logger.LogDebug("Starting search in {CollectionName} for query: {Query}", collectionName, query);
            
            // Tạo embedding từ query với timeout riêng
            float[] queryEmbedding;
            try
            {
                queryEmbedding = await GenerateEmbeddingAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding, returning empty results");
                return new List<VectorSearchResult>();
            }
            
            // Search trong Qdrant với timeout riêng
            var requestBody = new
            {
                vector = queryEmbedding,
                limit = topK,
                with_payload = true
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(QDRANT_TIMEOUT_SECONDS));
            var response = await _qdrantClient.PostAsJsonAsync($"/collections/{collectionName}/points/search", requestBody, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Qdrant search failed: {StatusCode}", response.StatusCode);
                return new List<VectorSearchResult>();
            }

            var result = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken: cts.Token);
            
            if (result?.Result == null)
            {
                return new List<VectorSearchResult>();
            }

            // Parse results
            var searchResults = new List<VectorSearchResult>();
            foreach (var item in result.Result)
            {
                string content = string.Empty;
                var metadata = new Dictionary<string, object>();
                
                if (item.Payload.HasValue)
                {
                    var payloadElement = item.Payload.Value;
                    
                    if (payloadElement.ValueKind == JsonValueKind.Object)
                    {
                        if (payloadElement.TryGetProperty("content", out var contentElement))
                        {
                            content = contentElement.GetString() ?? string.Empty;
                        }
                        else if (payloadElement.TryGetProperty("description", out var descElement))
                        {
                            content = descElement.GetString() ?? string.Empty;
                        }
                        
                        foreach (var prop in payloadElement.EnumerateObject())
                        {
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

                searchResults.Add(new VectorSearchResult
                {
                    Content = content,
                    Score = item.Score,
                    Metadata = metadata
                });
            }

            _logger.LogDebug("Search completed in {CollectionName}, found {Count} results", collectionName, searchResults.Count);
            return searchResults;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Search timeout in Qdrant: {CollectionName}", collectionName);
            return new List<VectorSearchResult>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error searching in Qdrant (Qdrant may not be running): {CollectionName}", collectionName);
            return new List<VectorSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching in Qdrant: {CollectionName}", collectionName);
            return new List<VectorSearchResult>();
        }
    }

    private async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        // Tạo cache key từ text (sử dụng hash để tránh key quá dài)
        var cacheKey = $"embedding_{GetTextHash(text)}";
        
        // Kiểm tra cache trước
        if (_cache.TryGetValue(cacheKey, out float[]? cachedEmbedding) && cachedEmbedding != null)
        {
            _logger.LogDebug("Using cached embedding for text hash: {Hash}", cacheKey);
            return cachedEmbedding;
        }

        try
        {
            _logger.LogDebug("Generating embedding for text (length: {Length})", text.Length);
            
            var request = new
            {
                input = text,
                model = "text-embedding-ada-002"
            };

            // Sử dụng _openAiClient riêng - KHÔNG thay đổi BaseAddress để tránh race condition
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(OPENAI_TIMEOUT_SECONDS));
            var response = await _openAiClient.PostAsJsonAsync("/v1/embeddings", request, cts.Token);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(cancellationToken: cts.Token);
            
            if (result?.Data == null || result.Data.Count == 0)
            {
                throw new Exception("Không nhận được embedding từ OpenAI");
            }

            var embedding = result.Data[0].Embedding;
            
            // Cache embedding để dùng lại
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES),
                Size = 1 // Each embedding counts as 1 unit
            };
            _cache.Set(cacheKey, embedding, cacheOptions);
            
            _logger.LogDebug("Generated and cached embedding for text hash: {Hash}", cacheKey);
            
            return embedding;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("OpenAI embedding request timeout");
            throw new TimeoutException("OpenAI embedding request timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling OpenAI API");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }

    /// <summary>
    /// Tạo hash từ text để dùng làm cache key
    /// </summary>
    private string GetTextHash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(hashBytes).Replace("/", "_").Replace("+", "-").Substring(0, 16);
    }

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


