-- Script đơn giản để thêm cột sender_type vào bảng Chat
-- Copy và chạy toàn bộ script này trong SQL Server Management Studio

USE testlaptop35;
GO

-- Thêm cột sender_type
ALTER TABLE [dbo].[Chat]
ADD [sender_type] NVARCHAR(20) NULL;
GO

-- Cập nhật dữ liệu cũ: nếu employee_id có giá trị thì là employee, ngược lại là customer
UPDATE [dbo].[Chat]
SET [sender_type] = CASE 
    WHEN [employee_id] IS NOT NULL THEN 'employee'
    ELSE 'customer'
END
WHERE [sender_type] IS NULL;
GO

-- Kiểm tra kết quả
SELECT TOP 10 [chat_id], [sender_type], [employee_id], [customer_id], [content_detail]
FROM [dbo].[Chat]
ORDER BY [time] DESC;
GO

PRINT 'Đã thêm cột sender_type thành công!';
GO

