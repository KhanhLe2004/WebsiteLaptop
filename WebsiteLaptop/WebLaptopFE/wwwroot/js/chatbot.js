// ============================================
// CHATBOT JAVASCRIPT - Tương tác với Backend API
// ============================================

// Config
const CHAT_API_URL = '/api/chat/ai'; // Relative URL - sẽ tự động resolve đến Backend
const BACKEND_BASE_URL = 'https://localhost:5068'; // Backend API URL
const HEALTH_CHECK_URL = '/api/chat/health'; // Health check endpoint

// DOM Elements
let chatToggleBtn, chatWindow, chatCloseBtn, chatMessages, chatInput, chatSendBtn, chatTyping;

// Cache for backend URL detection
let cachedBackendUrl = null;

// Initialize khi DOM ready
document.addEventListener('DOMContentLoaded', function() {
    initializeChatWidget();
});

function initializeChatWidget() {
    // Get DOM elements
    chatToggleBtn = document.getElementById('chatToggleBtn');
    chatWindow = document.getElementById('chatWindow');
    chatCloseBtn = document.getElementById('chatCloseBtn');
    chatMessages = document.getElementById('chatMessages');
    chatInput = document.getElementById('chatInput');
    chatSendBtn = document.getElementById('chatSendBtn');
    chatTyping = document.getElementById('chatTyping');

    if (!chatToggleBtn || !chatWindow) {
        console.error('Chat widget elements not found');
        return;
    }

    // Event listeners
    chatToggleBtn.addEventListener('click', toggleChatWindow);
    chatCloseBtn.addEventListener('click', closeChatWindow);
    chatSendBtn.addEventListener('click', sendMessage);
    chatInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    // Auto focus input khi mở chat
    chatToggleBtn.addEventListener('click', function() {
        setTimeout(() => {
            if (chatInput) chatInput.focus();
        }, 300);
    });
}

// Toggle chat window
function toggleChatWindow() {
    if (chatWindow.style.display === 'none' || chatWindow.style.display === '') {
        chatWindow.style.display = 'flex';
        chatToggleBtn.style.display = 'none';
    } else {
        closeChatWindow();
    }
}

// Close chat window
function closeChatWindow() {
    chatWindow.style.display = 'none';
    chatToggleBtn.style.display = 'flex';
}

// Send message with retry logic
async function sendMessage() {
    const message = chatInput.value.trim();
    if (!message) return;

    // Disable input và button
    chatInput.disabled = true;
    chatSendBtn.disabled = true;

    // Add user message to UI
    addMessage(message, 'user');
    chatInput.value = '';

    // Show typing indicator
    showTyping();

    // Retry logic: thử 2 lần nếu fail
    const maxRetries = 2;
    let lastError = null;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
        try {
            // Determine API URL with improved detection
            const apiUrl = await getBackendApiUrl(CHAT_API_URL);

            // Call API with shorter timeout (15 seconds) để fail fast
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 15000);

            console.log(`[Chat] Attempt ${attempt}/${maxRetries} - Calling API: ${apiUrl}`);

            const response = await fetch(apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    message: message,
                    customerId: null
                }),
                signal: controller.signal
            });

            clearTimeout(timeoutId);

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
                throw new Error(errorData.message || `API error: ${response.status} ${response.statusText}`);
            }

            const data = await response.json();

            // Hide typing
            hideTyping();

            // Add bot response
            if (data.answer) {
                addMessage(data.answer, 'bot');

                // Hiển thị sản phẩm gợi ý với link click được
                if (data.suggestedProducts && data.suggestedProducts.length > 0) {
                    renderProductSuggestions(data.suggestedProducts);
                }
            } else {
                addMessage('Xin lỗi, tôi không thể trả lời câu hỏi này. Vui lòng thử lại.', 'bot');
            }
            
            // Success - re-enable input và exit retry loop
            chatInput.disabled = false;
            chatSendBtn.disabled = false;
            chatInput.focus();
            console.log(`[Chat] Success on attempt ${attempt}`);
            return; // Exit the retry loop

        } catch (error) {
            lastError = error;
            console.error(`[Chat] Attempt ${attempt}/${maxRetries} failed:`, error);
            
            // Nếu còn retry thì thử lại sau 1 giây
            if (attempt < maxRetries) {
                console.log(`[Chat] Retrying in 1 second...`);
                await new Promise(resolve => setTimeout(resolve, 1000));
                continue;
            }
        }
    }

    // Tất cả retries đều fail
    hideTyping();
    
    // Provide detailed error messages based on error type
    let errorMessage = 'Xin lỗi, hiện tại hệ thống đang gặp sự cố. ';
    
    if (lastError) {
        if (lastError.name === 'AbortError') {
            errorMessage += 'Yêu cầu đã quá thời gian chờ. Vui lòng thử lại sau.';
        } else if (lastError.message && (lastError.message.includes('Failed to fetch') || lastError.message.includes('NetworkError'))) {
            errorMessage += 'Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng hoặc liên hệ nhân viên để được hỗ trợ.';
        } else if (lastError.message && lastError.message.includes('CORS')) {
            errorMessage += 'Lỗi cấu hình kết nối. Vui lòng liên hệ nhân viên kỹ thuật.';
        } else if (lastError.message) {
            errorMessage += `Chi tiết: ${lastError.message}`;
        } else {
            errorMessage += 'Anh/chị vui lòng thử lại sau hoặc liên hệ nhân viên để được hỗ trợ.';
        }
    } else {
        errorMessage += 'Anh/chị vui lòng thử lại sau hoặc liên hệ nhân viên để được hỗ trợ.';
    }
    
    addMessage(errorMessage, 'bot');

    // Re-enable input và button
    chatInput.disabled = false;
    chatSendBtn.disabled = false;
    chatInput.focus();
}

// Add message to UI
function addMessage(text, type) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${type}`;

    const contentDiv = document.createElement('div');
    contentDiv.className = 'message-content';
    
    // Format text (support basic markdown-like formatting)
    const formattedText = formatMessageText(text);
    contentDiv.innerHTML = formattedText;

    const timeDiv = document.createElement('div');
    timeDiv.className = 'message-time';
    timeDiv.textContent = new Date().toLocaleTimeString('vi-VN', { 
        hour: '2-digit', 
        minute: '2-digit' 
    });

    messageDiv.appendChild(contentDiv);
    messageDiv.appendChild(timeDiv);
    chatMessages.appendChild(messageDiv);

    // Auto scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

// Format message text (basic markdown support)
function formatMessageText(text) {
    // Convert line breaks
    text = text.replace(/\n/g, '<br>');
    
    // Convert **bold**
    text = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    
    // Convert *italic*
    text = text.replace(/\*(.*?)\*/g, '<em>$1</em>');
    
    // Convert bullet points
    text = text.replace(/^•\s(.+)$/gm, '<li>$1</li>');
    text = text.replace(/(<li>.*<\/li>)/s, '<ul>$1</ul>');
    
    return text;
}

// Format price
function formatPrice(price) {
    if (!price) return 'Liên hệ';
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(price);
}

// Render product suggestions với link click được
function renderProductSuggestions(products) {
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message bot';

    const contentDiv = document.createElement('div');
    contentDiv.className = 'message-content product-suggestions';
    
    // Hiển thị tối đa 5 sản phẩm
    const displayProducts = products.slice(0, 5);
    
    let html = '<div class="suggested-products-list">';
    
    displayProducts.forEach((product, index) => {
        const productId = product.productId || product.ProductId;
        const productName = product.name || product.Name || product.productName || product.ProductName;
        const price = product.price || product.Price || product.sellingPrice || product.SellingPrice;
        const imageUrl = product.imageUrl || product.ImageUrl;
        const detailUrl = product.detailUrl || product.DetailUrl || `/Home/ProductDetail?id=${productId}`;
        
        html += `
            <div class="product-item">
                ${imageUrl ? `<img src="${imageUrl}" alt="${productName}" class="product-image" onerror="this.src='/imageProducts/default.jpg'">` : ''}
                <div class="product-info">
                    <div class="product-name">${productName}</div>
                    <div class="product-price">${formatPrice(price)}</div>
                    <a href="${detailUrl}" class="product-detail-btn" target="_blank">
                        Xem chi tiết →
                    </a>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    contentDiv.innerHTML = html;

    const timeDiv = document.createElement('div');
    timeDiv.className = 'message-time';
    timeDiv.textContent = new Date().toLocaleTimeString('vi-VN', { 
        hour: '2-digit', 
        minute: '2-digit' 
    });

    messageDiv.appendChild(contentDiv);
    messageDiv.appendChild(timeDiv);
    chatMessages.appendChild(messageDiv);

    // Auto scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

// Show typing indicator
function showTyping() {
    if (chatTyping) {
        chatTyping.style.display = 'flex';
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}

// Hide typing indicator
function hideTyping() {
    if (chatTyping) {
        chatTyping.style.display = 'none';
    }
}

// Get backend API URL with automatic detection
async function getBackendApiUrl(endpoint) {
    // Use cached URL if available
    if (cachedBackendUrl) {
        return `${cachedBackendUrl}${endpoint}`;
    }

    const currentHost = window.location.hostname;
    const currentPort = window.location.port;
    const isLocalhost = currentHost === 'localhost' || currentHost === '127.0.0.1';

    // If same origin, use relative URL
    if (!isLocalhost || currentPort === '5068') {
        cachedBackendUrl = '';
        return endpoint;
    }

    // Try to detect backend URL by testing health check
    const testUrls = [
        `${BACKEND_BASE_URL}${endpoint.replace('/api/chat/ai', HEALTH_CHECK_URL)}`,
        `http://localhost:5068${endpoint.replace('/api/chat/ai', HEALTH_CHECK_URL)}`,
        `https://localhost:5068${endpoint.replace('/api/chat/ai', HEALTH_CHECK_URL)}`
    ];

    for (const testUrl of testUrls) {
        try {
            const baseUrl = testUrl.replace(HEALTH_CHECK_URL, '');
            const response = await fetch(testUrl, {
                method: 'GET',
                mode: 'cors',
                cache: 'no-cache'
            });
            
            if (response.ok) {
                cachedBackendUrl = baseUrl;
                return `${baseUrl}${endpoint}`;
            }
        } catch (e) {
            // Continue to next URL
            console.debug(`Failed to connect to ${testUrl}:`, e);
        }
    }

    // Fallback to configured backend URL
    cachedBackendUrl = BACKEND_BASE_URL;
    return `${BACKEND_BASE_URL}${endpoint}`;
}

// Export functions for potential external use
window.Chatbot = {
    sendMessage,
    addMessage,
    toggleChatWindow,
    closeChatWindow,
    getBackendApiUrl
};

