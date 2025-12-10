# Script PowerShell để setup Qdrant collection và insert policy documents
# Chạy script này sau khi Qdrant đã được khởi động

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Setup Qdrant Collection for Chatbot" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$qdrantUrl = "http://localhost:6333"
$collectionName = "warranty_policies"

# Kiểm tra Qdrant có đang chạy không
Write-Host "Đang kiểm tra Qdrant..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$qdrantUrl/health" -Method GET -TimeoutSec 5
    Write-Host "✓ Qdrant đang chạy" -ForegroundColor Green
} catch {
    Write-Host "✗ Qdrant không chạy! Vui lòng chạy start-qdrant.bat trước." -ForegroundColor Red
    exit 1
}

# Tạo collection
Write-Host ""
Write-Host "Đang tạo collection: $collectionName..." -ForegroundColor Yellow
$createCollectionBody = @{
    vectors = @{
        size = 1536
        distance = "Cosine"
    }
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$qdrantUrl/collections/$collectionName" -Method PUT -Body $createCollectionBody -ContentType "application/json"
    Write-Host "✓ Collection đã được tạo" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Host "✓ Collection đã tồn tại" -ForegroundColor Green
    } else {
        Write-Host "✗ Lỗi khi tạo collection: $_" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Setup hoàn tất!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Lưu ý: Bạn cần insert policy documents vào collection." -ForegroundColor Yellow
Write-Host "Sử dụng API endpoint hoặc script insert-policies.ps1" -ForegroundColor Yellow
Write-Host ""




