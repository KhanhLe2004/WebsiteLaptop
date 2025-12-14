# Hướng dẫn Debug Chat

## Các vấn đề thường gặp và cách khắc phục

### 1. Không thể gửi tin nhắn

**Kiểm tra:**
- Mở Developer Tools (F12) trong trình duyệt
- Xem tab Console để kiểm tra lỗi
- Xem tab Network để kiểm tra kết nối SignalR

**Các lỗi thường gặp:**

#### a) Connection không được thiết lập
```
Error: Failed to start the connection
```
**Giải pháp:**
- Kiểm tra backend đang chạy (http://localhost:5068)
- Kiểm tra CORS settings trong Program.cs
- Kiểm tra SignalR Hub đã được map: `app.MapHub<ChatHub>("/chathub")`

#### b) CORS Error
```
Access to fetch at 'http://localhost:5068/chathub' from origin 'http://localhost:5253' has been blocked by CORS policy
```
**Giải pháp:**
- Đảm bảo CORS được cấu hình đúng trong Program.cs
- Frontend URL phải được thêm vào `WithOrigins`

#### c) CustomerId/EmployeeId null
```
Error: Vui lòng đăng nhập
```
**Giải pháp:**
- Kiểm tra sessionStorage có chứa thông tin customer/employee
- Đảm bảo đã đăng nhập trước khi vào trang chat

### 2. Tin nhắn không hiển thị

**Kiểm tra:**
- Xem Console log để kiểm tra có nhận được message từ SignalR
- Kiểm tra hàm `displayMessage` có được gọi không
- Kiểm tra `messagesList` element có tồn tại không

### 3. Tin nhắn không được lưu vào database

**Kiểm tra:**
- Xem log của backend
- Kiểm tra connection string database
- Kiểm tra bảng Chat có tồn tại trong database

### 4. Cách test thủ công

1. **Test Connection:**
   - Mở browser console
   - Gõ: `connection.state` (phải là "Connected")

2. **Test Send Message:**
   - Mở browser console
   - Gõ: `connection.invoke("SendMessage", "CUSTOMER_ID", null, "Test message", "customer")`

3. **Test Receive Message:**
   - Kiểm tra có event listener: `connection.on("ReceiveMessage", ...)`

### 5. Debug Steps

1. Kiểm tra backend đang chạy
2. Kiểm tra frontend đang chạy
3. Mở Developer Tools
4. Kiểm tra Console logs
5. Kiểm tra Network tab - tìm request đến `/chathub`
6. Kiểm tra WebSocket connection status

### 6. Common Fixes

- **Restart backend:** Đôi khi cần restart để SignalR hoạt động đúng
- **Clear browser cache:** Xóa cache và reload trang
- **Check ports:** Đảm bảo ports 5068 (backend) và 5253 (frontend) không bị conflict
- **Check firewall:** Đảm bảo firewall không chặn WebSocket connections

