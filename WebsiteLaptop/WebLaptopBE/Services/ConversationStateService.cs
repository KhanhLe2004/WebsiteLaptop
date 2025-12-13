using Microsoft.Extensions.Caching.Memory;
using WebLaptopBE.DTOs;

namespace WebLaptopBE.Services;

/// <summary>
/// Interface cho Conversation State Service
/// </summary>
public interface IConversationStateService
{
    ConversationState GetOrCreate(string sessionId);
    void Update(ConversationState state);
    void Clear(string sessionId);
    bool Exists(string sessionId);
}

/// <summary>
/// Service để quản lý conversation state (in-memory cache)
/// Lưu trạng thái hội thoại để hỗ trợ guided conversation
/// </summary>
public class ConversationStateService : IConversationStateService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConversationStateService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

    public ConversationStateService(
        IMemoryCache cache,
        ILogger<ConversationStateService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Lấy hoặc tạo mới conversation state
    /// </summary>
    public ConversationState GetOrCreate(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
        }

        var cacheKey = GetCacheKey(sessionId);

        if (!_cache.TryGetValue(cacheKey, out ConversationState? state) || state == null)
        {
            state = new ConversationState
            {
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                CurrentStep = "menu", // Bắt đầu từ menu
                Filters = new ProductFilter(),
                MessageHistory = new List<string>()
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(DefaultExpiration)
                .SetPriority(CacheItemPriority.Normal);

            _cache.Set(cacheKey, state, cacheOptions);
            _logger.LogInformation("Created new conversation state for session {SessionId}", sessionId);
        }
        else
        {
            // Update last activity
            state.LastActivity = DateTime.UtcNow;
            _logger.LogDebug("Retrieved conversation state for session {SessionId}", sessionId);
        }

        return state;
    }

    /// <summary>
    /// Cập nhật conversation state
    /// </summary>
    public void Update(ConversationState state)
    {
        if (state == null || string.IsNullOrEmpty(state.SessionId))
        {
            _logger.LogWarning("Attempted to update null or invalid conversation state");
            return;
        }

        state.LastActivity = DateTime.UtcNow;

        var cacheKey = GetCacheKey(state.SessionId);
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(DefaultExpiration)
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(cacheKey, state, cacheOptions);
        _logger.LogDebug("Updated conversation state for session {SessionId}, step={Step}", 
            state.SessionId, state.CurrentStep);
    }

    /// <summary>
    /// Xóa conversation state
    /// </summary>
    public void Clear(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return;
        }

        var cacheKey = GetCacheKey(sessionId);
        _cache.Remove(cacheKey);
        _logger.LogInformation("Cleared conversation state for session {SessionId}", sessionId);
    }

    /// <summary>
    /// Kiểm tra xem session có tồn tại không
    /// </summary>
    public bool Exists(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return false;
        }

        var cacheKey = GetCacheKey(sessionId);
        return _cache.TryGetValue(cacheKey, out _);
    }

    /// <summary>
    /// Tạo cache key từ session ID
    /// </summary>
    private static string GetCacheKey(string sessionId)
    {
        return $"ConversationState:{sessionId}";
    }
}



