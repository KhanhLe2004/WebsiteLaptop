@echo off
echo ============================================
echo Starting Qdrant Docker Container
echo ============================================
echo.

REM Kiểm tra xem Docker có đang chạy không
docker info >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker không đang chạy!
    echo Vui lòng khởi động Docker Desktop và thử lại.
    pause
    exit /b 1
)

REM Kiểm tra xem container đã tồn tại chưa
docker ps -a --filter "name=qdrant" --format "{{.Names}}" | findstr /C:"qdrant" >nul
if errorlevel 1 (
    echo Container chưa tồn tại, đang tạo mới...
    docker run -d --name qdrant -p 6333:6333 -p 6334:6334 qdrant/qdrant
    if errorlevel 1 (
        echo ERROR: Không thể tạo container!
        pause
        exit /b 1
    )
    echo Container đã được tạo và khởi động.
) else (
    echo Container đã tồn tại, đang khởi động...
    docker start qdrant
    if errorlevel 1 (
        echo ERROR: Không thể khởi động container!
        pause
        exit /b 1
    )
    echo Container đã được khởi động.
)

echo.
echo ============================================
echo Qdrant đang chạy tại: http://localhost:6333
echo Dashboard: http://localhost:6333/dashboard
echo ============================================
echo.
echo Nhấn phím bất kỳ để đóng cửa sổ này...
pause >nul




