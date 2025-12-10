# Script PowerShell để insert policy documents vào Qdrant
# Yêu cầu: OpenAI API Key và Qdrant đang chạy

param(
    [string]$OpenAiApiKey = "",
    [string]$QdrantUrl = "http://localhost:6333"
)

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Insert Policy Documents into Qdrant" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra OpenAI API Key
if ([string]::IsNullOrEmpty($OpenAiApiKey)) {
    $OpenAiApiKey = Read-Host "Nhập OpenAI API Key"
    if ([string]::IsNullOrEmpty($OpenAiApiKey)) {
        Write-Host "✗ OpenAI API Key là bắt buộc!" -ForegroundColor Red
        exit 1
    }
}

$collectionName = "warranty_policies"

# Policy documents mẫu
$policies = @(
    @{
        content = "Chính sách bảo hành: Tất cả sản phẩm laptop được bảo hành chính hãng từ 12 đến 24 tháng tùy theo sản phẩm. Bảo hành bao gồm lỗi phần cứng và phần mềm do nhà sản xuất. Khách hàng cần giữ hóa đơn và tem bảo hành. Thời gian xử lý bảo hành từ 3-7 ngày làm việc."
        metadata = @{
            policy_type = "warranty"
            title = "Chính sách bảo hành"
        }
    },
    @{
        content = "Chính sách đổi trả: Khách hàng có thể đổi trả sản phẩm trong vòng 7 ngày kể từ ngày mua nếu sản phẩm còn nguyên seal, chưa sử dụng, và có lỗi do nhà sản xuất. Sản phẩm đổi trả phải kèm theo hóa đơn và đầy đủ phụ kiện. Phí vận chuyển đổi trả do khách hàng chịu trừ trường hợp lỗi do nhà sản xuất."
        metadata = @{
            policy_type = "return"
            title = "Chính sách đổi trả"
        }
    },
    @{
        content = "Chính sách hoàn tiền: Hoàn tiền 100% trong vòng 3 ngày đầu nếu sản phẩm chưa sử dụng, còn nguyên seal, và có lỗi do nhà sản xuất. Sau 3 ngày, chỉ áp dụng đổi sản phẩm khác. Hoàn tiền sẽ được thực hiện qua phương thức thanh toán ban đầu trong vòng 5-7 ngày làm việc."
        metadata = @{
            policy_type = "refund"
            title = "Chính sách hoàn tiền"
        }
    }
)

Write-Host "Đang tạo embeddings và insert vào Qdrant..." -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$failCount = 0

foreach ($policy in $policies) {
    try {
        # Tạo embedding bằng OpenAI API
        Write-Host "Đang tạo embedding cho: $($policy.metadata.title)..." -ForegroundColor Yellow
        
        $embeddingBody = @{
            input = $policy.content
            model = "text-embedding-ada-002"
        } | ConvertTo-Json

        $headers = @{
            "Authorization" = "Bearer $OpenAiApiKey"
            "Content-Type" = "application/json"
        }

        $embeddingResponse = Invoke-RestMethod -Uri "https://api.openai.com/v1/embeddings" -Method POST -Body $embeddingBody -Headers $headers
        $embedding = $embeddingResponse.data[0].embedding

        # Insert vào Qdrant
        $pointId = [guid]::NewGuid().ToString()
        $pointBody = @{
            points = @(
                @{
                    id = $pointId
                    vector = $embedding
                    payload = @{
                        content = $policy.content
                        policy_type = $policy.metadata.policy_type
                        title = $policy.metadata.title
                    }
                }
            )
        } | ConvertTo-Json -Depth 10

        $qdrantResponse = Invoke-RestMethod -Uri "$QdrantUrl/collections/$collectionName/points" -Method PUT -Body $pointBody -ContentType "application/json"
        
        Write-Host "✓ Đã insert: $($policy.metadata.title)" -ForegroundColor Green
        $successCount++
    } catch {
        Write-Host "✗ Lỗi khi insert: $($policy.metadata.title) - $_" -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Hoàn tất!" -ForegroundColor Green
Write-Host "Thành công: $successCount" -ForegroundColor Green
Write-Host "Thất bại: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host "============================================" -ForegroundColor Cyan




