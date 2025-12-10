-- Migration script to add SenderType column to Chat table
-- Run this script in your SQL Server database

USE testlaptop35; -- Change to your database name if different
GO

-- Add SenderType column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Chat]') 
    AND name = 'sender_type'
)
BEGIN
    ALTER TABLE [dbo].[Chat]
    ADD [sender_type] NVARCHAR(20) NULL;
    
    PRINT 'Column sender_type added successfully';
    
    -- Update existing records: if EmployeeId is not null, it was sent by employee
    -- Otherwise, it was sent by customer
    UPDATE [dbo].[Chat]
    SET [sender_type] = CASE 
        WHEN [employee_id] IS NOT NULL THEN 'employee'
        ELSE 'customer'
    END
    WHERE [sender_type] IS NULL;
    
    PRINT 'Existing records updated with sender_type';
END
ELSE
BEGIN
    PRINT 'Column sender_type already exists';
END
GO
