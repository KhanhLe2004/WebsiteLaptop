--create database [testlaptop38]
--use testlaptop38
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Role](
	[role_id] [nvarchar](20) NOT NULL,
	[role_name] [nvarchar](50) NULL,
 CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED 
(
	[role_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
------------------------
--SET ANSI_NULLS ON
--GO
--SET QUOTED_IDENTIFIER ON
--GO
--CREATE TABLE [dbo].[Branches](
--	[branches_id] [nvarchar](20) NOT NULL,
--	[branches_name] [nvarchar](100) NULL,
--	[address] [nvarchar](1000) NULL,
--	[phone_number] [nvarchar](20) NULL,
-- CONSTRAINT [PK_Branches] PRIMARY KEY CLUSTERED 
--(
--	[branches_id] ASC
--)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
--) ON [PRIMARY]
--GO
-------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Employee](
	[employee_id] [nvarchar](20) NOT NULL,
	[employee_name] [nvarchar](50) NULL,
	[date_of_birth] [date] NULL,
	[phone_number] [nvarchar](20) NULL,
	[email] [nvarchar](100) NULL,
	[address] [nvarchar](1000) NULL,
	[role_id] [nvarchar](20) NULL,
	[avatar] [nvarchar](100) NULL,
	[username] [nvarchar](100) NULL,
	[password] [nvarchar](20) NULL,
	[active] [bit] NULL,
 CONSTRAINT [PK_Employee] PRIMARY KEY CLUSTERED 
(
	[employee_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
----------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Brands](
	[brand_id] [nvarchar](20) NOT NULL,
	[brand_name] [nvarchar](50) NULL,
	[active] [bit] NULL,
 CONSTRAINT [PK_Brands] PRIMARY KEY CLUSTERED 
(
	[brand_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product](
	[product_id] [nvarchar](20) NOT NULL,
	[product_name] [nvarchar](100) NULL,
	[product_model] [nvarchar](100) NULL,
	[warranty_period] [int] NULL,
	[original_selling_price] [decimal](18, 2) NULL,
	[selling_price] [decimal](18, 2) NULL,
	[screen] [nvarchar](50) NULL,
	[camera] [nvarchar](50) NULL,
	[connect] [nvarchar](200) NULL,
	[weight] [decimal](18, 2) NULL,
	[pin] [nvarchar](50) NULL,
	[brand_id] [nvarchar](20) NULL,
	[avatar] [nvarchar](100) NULL,
	[active] [bit] NULL,
 CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED 
(
	[product_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductImage](
	[image_id] [nvarchar](20) NOT NULL,
	[product_id] [nvarchar](20) NULL,
 CONSTRAINT [PK_ProductImage] PRIMARY KEY CLUSTERED 
(
	[image_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
--SET ANSI_NULLS ON
--GO
--SET QUOTED_IDENTIFIER ON
--GO
--CREATE TABLE [dbo].[ProductColor](
--	[color_id] [nvarchar](20) NOT NULL,
--	[color_name] [nvarchar](50) NULL,
--	[product_id] [nvarchar](20) NULL,
-- CONSTRAINT [PK_ProductColor] PRIMARY KEY CLUSTERED 
--(
--	[color_id] ASC
--)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
--) ON [PRIMARY]
--GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductConfiguration](
	[configuration_id] [nvarchar](20) NOT NULL,
	[cpu] [nvarchar](50) NULL,
	[ram] [nvarchar](50) NULL,
	[rom] [nvarchar](50) NULL,
	[card] [nvarchar](50) NULL,
	[price] [decimal](18, 2) NULL,
	[product_id] [nvarchar](20) NULL,
	[quantity] [int] NULL,
 CONSTRAINT [PK_ProductConfiguration] PRIMARY KEY CLUSTERED 
(
	[configuration_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE ProductSerial (
    [serial_id] [nvarchar](20) NOT NULL,
    [product_id] [nvarchar](20) NULL,
    [specifications] [nvarchar](100) NULL,
	[stockExportDetail_id] [nvarchar](20) NULL,
    [status] [nvarchar](20) NULL,
    [import_date] [datetime] NULL,
	[export_date] [datetime] NULL,
    [warranty_start_date] [datetime] NULL,
    [warranty_end_date] [datetime] NULL,
    [note] [nvarchar](200) NULL,
 CONSTRAINT [PK_ProductSerial] PRIMARY KEY CLUSTERED 
(
	[serial_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
--SET ANSI_NULLS ON
--GO
--SET QUOTED_IDENTIFIER ON
--GO
--CREATE TABLE [dbo].[Account](
--	[username] [nvarchar](20) NOT NULL,
--	[password] [nvarchar](20) NULL,
--	[account_type] [nvarchar](50) NULL,
-- CONSTRAINT [PK_Account] PRIMARY KEY CLUSTERED 
--(
--	[username] ASC
--)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
--) ON [PRIMARY]
--GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Supplier](
	[supplier_id] [nvarchar](20) NOT NULL,
	[supplier_name] [nvarchar](50) NULL,
	[phone_number] [nvarchar](20) NULL,
	[address] [nvarchar](1000) NULL,
	[email] [nvarchar](100) NULL,
	[active] [bit] NULL,
 CONSTRAINT [PK_Supplier] PRIMARY KEY CLUSTERED 
(
	[supplier_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SaleInvoice](
	[saleInvoice_id] [nvarchar](20) NOT NULL,
	[payment_method] [nvarchar](50) NULL,
	[total_amount] [decimal](18, 2) NULL,
	[time_create] [datetime] NULL,
	[status] [nvarchar](50) NULL,
	[delivery_fee] [decimal](18, 2) NULL,
	[discount] [decimal](18, 2) NULL,
	[phone] [nvarchar](20) NULL,
	[delivery_address] [nvarchar](1000) NULL,
	[employee_id] [nvarchar](20) NULL,
	[customer_id] [nvarchar](20) NULL,
	[employee_ship] [nvarchar](20) NULL,
	[time_ship] [datetime] NULL,

 CONSTRAINT [PK_SaleInvoice] PRIMARY KEY CLUSTERED 
(
	[saleInvoice_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SaleInvoiceDetail](
	[saleInvoiceDetail_id] [nvarchar](20) NOT NULL,
	[saleInvoice_id] [nvarchar](20) NULL,
	[quantity] [int] NULL,
	[unit_price] [decimal](18, 2) NULL,
	[product_id] [nvarchar](20) NULL,
	[specifications] [nvarchar](100) NULL,
 CONSTRAINT [PK_SaleInvoiceDetail] PRIMARY KEY CLUSTERED 
(
	[saleInvoiceDetail_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cart](
	[cart_id] [nvarchar](20) NOT NULL,
	[total_amount] [decimal](18, 2) NULL,
	[customer_id] [nvarchar](20) NULL,
 CONSTRAINT [PK_Cart] PRIMARY KEY CLUSTERED 
(
	[cart_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CartDetail](
	[cartDetail_id] [nvarchar](20) NOT NULL,
	[quantity] [int] NULL,
	[specifications] [nvarchar](100) NULL,
	[cart_id] [nvarchar](20) NULL,
	[product_id] [nvarchar](20) NULL,

 CONSTRAINT [PK_CartDetail] PRIMARY KEY CLUSTERED 
(
	[cartDetail_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductReview](
	[productReview_id] [nvarchar](20) NOT NULL,
	[content_detail] [nvarchar](max) NULL,
	[rate] [int] NULL,
	[customer_id] [nvarchar](20) NULL,
	[time] [datetime] NULL,
	[product_id] [nvarchar](20) NULL,
 CONSTRAINT [PK_ProductReview] PRIMARY KEY CLUSTERED 
(
	[productReview_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[History](
	[history_id] [nvarchar](20) NOT NULL,
	[activity_type] [nvarchar](200) NULL,
	[employee_id] [nvarchar](20) NULL,
	[time] [datetime] NULL,

 CONSTRAINT [PK_History] PRIMARY KEY CLUSTERED 
(
	[history_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StockImport](
	[stockImport_id] [nvarchar](20) NOT NULL,
	[supplier_id] [nvarchar](20) NULL,
	[employee_id] [nvarchar](20) NULL,
	[time] [datetime] NULL,
	[total_amount] [decimal](18, 2) NULL,
 CONSTRAINT [PK_StockImport] PRIMARY KEY CLUSTERED 
(
	[stockImport_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StockImportDetail](
	[stockImportDetail_id] [nvarchar](20) NOT NULL,
	[stockImport_id] [nvarchar](20) NULL,
	[product_id] [nvarchar](20) NULL,
	[specifications] [nvarchar](100) NULL,
	[quantity] [int] NULL,
	[price] [decimal](18, 2) NULL,
 CONSTRAINT [PK_StockImportDetail] PRIMARY KEY CLUSTERED 
(
	[stockImportDetail_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StockExport](
	[stockExport_id] [nvarchar](20) NOT NULL,
	[employee_id] [nvarchar](20) NULL,
	[saleInvoice_id] [nvarchar](20) NULL,
	[status] [nvarchar](20) NULL,
	[time] [datetime] NULL,
	
 CONSTRAINT [PK_StockExport] PRIMARY KEY CLUSTERED 
(
	[stockExport_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StockExportDetail](
	[stockExportDetail_id] [nvarchar](20) NOT NULL,
	[stockExport_id] [nvarchar](20) NULL,
	[product_id] [nvarchar](20) NULL,
	[specifications] [nvarchar](100) NULL,
	[quantity] [int] NULL,
 CONSTRAINT [PK_StockExportDetail] PRIMARY KEY CLUSTERED 
(
	[stockExportDetail_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Warranty](
	[warranty_id] [nvarchar](20) NOT NULL,
	[customer_id] [nvarchar](20) NULL,
	[phone_number] [nvarchar](20) NULL,
	[serial_id] [nvarchar](20) NULL,
	[employee_id] [nvarchar](20) NULL,
	[type] [nvarchar](50) NULL,
	[content_detail] [nvarchar](200) NULL,
	[status] [nvarchar](50) NULL,
	[time] [datetime] NULL,
	[total_amount] [decimal](18, 2) NULL,
 CONSTRAINT [PK_Warranty] PRIMARY KEY CLUSTERED 
(
	[warranty_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Promotion](
	[promotion_id] [nvarchar](20) NOT NULL,
	[product_id] [nvarchar](20) NULL,
	[type] [nvarchar](50) NULL,
	[content_detail] [nvarchar](200) NULL,
 CONSTRAINT [PK_Promotion] PRIMARY KEY CLUSTERED 
(
	[promotion_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Chat](
	[chat_id] [nvarchar](20) NOT NULL,
	[content_detail] [nvarchar](max) NULL,
	[time] [datetime] NULL,
	[status] [nvarchar](50) NULL,
	[customer_id] [nvarchar](20) NULL,
	[employee_id] [nvarchar](20) NULL,
	[sender_type] [nvarchar](20) NULL
 CONSTRAINT [PK_Chat] PRIMARY KEY CLUSTERED 
(
	[chat_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
---------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Customer](
	[customer_id] [nvarchar](20) NOT NULL,
	[customer_name] [nvarchar](100) NULL,
	[date_of_birth] [date] NULL,
	[phone_number] [nvarchar](20) NULL,
	[address] [nvarchar](1000) NULL,
	[email] [nvarchar](100) NULL,
	[avatar] [nvarchar](500) NULL,
	[username] [nvarchar](100) NULL,
	[password] [nvarchar](20) NULL,
	[active] [bit] NULL,
 CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED 
(
	[customer_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

----------------------------------------------------------------------

---------------------- FOREIGN KEYS ----------------------
-- Employee → Role, Account
ALTER TABLE [dbo].[Employee]
ADD CONSTRAINT FK_Employee_Role FOREIGN KEY ([role_id]) REFERENCES [dbo].[Role]([role_id]);

-- Product → Brands
ALTER TABLE [dbo].[Product]
ADD CONSTRAINT FK_Product_Brands FOREIGN KEY ([brand_id]) REFERENCES [dbo].[Brands]([brand_id]);

-- ProductImage → Product
ALTER TABLE [dbo].[ProductImage]
ADD CONSTRAINT FK_ProductImage_Product FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]);

-- ProductConfiguration → Product
ALTER TABLE [dbo].[ProductConfiguration]
ADD CONSTRAINT FK_ProductConfiguration_Product FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]);

-- ProductSerial → Product
ALTER TABLE [dbo].[ProductSerial]
ADD CONSTRAINT FK_ProductSerial_Product FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]);

ALTER TABLE [dbo].[ProductSerial]
ADD CONSTRAINT FK_ProductSerial_StockExportDetail FOREIGN KEY ([stockExportDetail_id]) REFERENCES [dbo].[StockExportDetail]([stockExportDetail_id]);


-- SaleInvoice → Employee, Customer
ALTER TABLE [dbo].[SaleInvoice]
ADD CONSTRAINT FK_SaleInvoice_Employee FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee]([employee_id]);

ALTER TABLE [dbo].[SaleInvoice]
ADD CONSTRAINT FK_SaleInvoice_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer]([customer_id]);

-- SaleInvoiceDetail → SaleInvoice, Product
ALTER TABLE [dbo].[SaleInvoiceDetail]
ADD CONSTRAINT FK_SaleInvoiceDetail_SaleInvoice FOREIGN KEY ([saleInvoice_id]) REFERENCES [dbo].[SaleInvoice]([saleInvoice_id]);

ALTER TABLE [dbo].[SaleInvoiceDetail]
ADD CONSTRAINT FK_SaleInvoiceDetail_Product FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]);

-- Cart → Account
ALTER TABLE [dbo].[Cart]
ADD CONSTRAINT FK_Cart_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer]([customer_id]);

-- CartDetail → Cart, Product
ALTER TABLE [dbo].[CartDetail]
ADD CONSTRAINT FK_CartDetail_Cart FOREIGN KEY ([cart_id]) REFERENCES [dbo].[Cart]([cart_id]);

ALTER TABLE [dbo].[CartDetail]
ADD CONSTRAINT FK_CartDetail_Product FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]);

-- ProductReview → Product, Account
ALTER TABLE [dbo].[ProductReview]
ADD CONSTRAINT FK_ProductReview_Product FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]);

ALTER TABLE [dbo].[ProductReview]
ADD CONSTRAINT FK_ProductReview_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer]([customer_id]);

-- History → Account
ALTER TABLE [dbo].[History]
ADD CONSTRAINT FK_History_Employee FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee]([employee_id]);

-- StockImport → Supplier, Employee
ALTER TABLE [dbo].[StockImport]
ADD CONSTRAINT FK_StockImport_Supplier FOREIGN KEY ([supplier_id]) REFERENCES [dbo].[Supplier]([supplier_id]);

ALTER TABLE [dbo].[StockImport]
ADD CONSTRAINT FK_StockImport_Employee FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee]([employee_id]);

-- StockImportDetail → StockImport, Product
ALTER TABLE [dbo].[StockImportDetail]
ADD CONSTRAINT FK_StockImportDetail_StockImport FOREIGN KEY ([stockImport_id]) REFERENCES [dbo].[StockImport]([stockImport_id]);

ALTER TABLE [dbo].[StockImportDetail]
ADD CONSTRAINT FK_StockImportDetail_Product FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]);

-- StockExport → Employee
ALTER TABLE [dbo].[StockExport]
ADD CONSTRAINT FK_StockExport_Employee FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee]([employee_id]);


ALTER TABLE [dbo].[StockExport]
ADD CONSTRAINT FK_StockExport_SaleInvoice FOREIGN KEY ([saleInvoice_id]) REFERENCES [dbo].[SaleInvoice]([saleInvoice_id]);

-- StockExportDetail → StockExport, Product
ALTER TABLE [dbo].[StockExportDetail]
ADD CONSTRAINT FK_StockExportDetail_StockExport FOREIGN KEY ([stockExport_id]) REFERENCES [dbo].[StockExport]([stockExport_id]);


-- Warranty → Customer, ProductSerial, Employee
ALTER TABLE [dbo].[Warranty]
ADD CONSTRAINT FK_Warranty_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer]([customer_id]);

ALTER TABLE [dbo].[Warranty]
ADD CONSTRAINT FK_Warranty_ProductSerial FOREIGN KEY ([serial_id]) REFERENCES [dbo].[ProductSerial]([serial_id]);

ALTER TABLE [dbo].[Warranty]
ADD CONSTRAINT FK_Warranty_Employee FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee]([employee_id]);

-- Promotion → Product
ALTER TABLE [dbo].[Promotion]
ADD CONSTRAINT FK_Promotion_Product FOREIGN KEY ([product_id]) REFERENCES [dbo].[Product]([product_id]);

-- Chat → Customer, Employee
ALTER TABLE [dbo].[Chat]
ADD CONSTRAINT FK_Chat_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer]([customer_id]);

ALTER TABLE [dbo].[Chat]
ADD CONSTRAINT FK_Chat_Employee FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee]([employee_id]);
GO


-- =====================================
-- INSERT DỮ LIỆU MẪU
-- ====================================

-- ========== ROLE ==========
INSERT INTO Role (role_id, role_name) VALUES (N'ADM', N'Quản trị viên');
INSERT INTO Role (role_id, role_name) VALUES (N'CCH', N'Chủ cửa hàng');
INSERT INTO Role (role_id, role_name) VALUES (N'SL', N'Nhân viên bán hàng');
INSERT INTO Role (role_id, role_name) VALUES (N'ST', N'Nhân viên kho');
INSERT INTO Role (role_id, role_name) VALUES (N'TE', N'Kỹ thuật viên');

-- ========== BRANDS ==========
INSERT INTO Brands (brand_id, brand_name, active) VALUES (N'B001', N'Dell', 1);
INSERT INTO Brands (brand_id, brand_name, active) VALUES (N'B002', N'Lenovo', 1);
INSERT INTO Brands (brand_id, brand_name, active) VALUES (N'B003', N'HP', 1);
INSERT INTO Brands (brand_id, brand_name, active) VALUES (N'B004', N'ASUS', 1);


-- ========== SUPPLIER ==========
INSERT INTO Supplier (supplier_id, supplier_name, phone_number, address, email, active) VALUES (N'SUP001', N'Công ty TNHH ASUS Việt Nam', N'0281234567', N'Quận 7, TP.HCM', N'contact@asus.com', 1);
INSERT INTO Supplier (supplier_id, supplier_name, phone_number, address, email, active) VALUES (N'SUP002', N'Công ty TNHH Dell Việt Nam', N'0282345678', N'Quận 1, TP.HCM', N'support@dell.vn', 1);
INSERT INTO Supplier (supplier_id, supplier_name, phone_number, address, email, active) VALUES (N'SUP003', N'Công ty TNHH HP Việt Nam', N'0243456789', N'Cầu Giấy, Hà Nội', N'hp@hp.com', 1);

-- ========== EMPLOYEE ==========
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E001', N'Nguyễn Văn An', CAST(N'1990-05-12' AS Date), N'0912345678', N'annguyen@gmail.com', N'55 Châu Quỳ, Phường Long Biên, Thành phố Hà Nội', N'ADM', N'e001.jpg', N'annguyen@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E002', N'Trần Thị Bình', CAST(N'1993-08-20' AS Date), N'0934567890', N'binhtran@gmail.com', N'381 Nguyễn Khang, Phường Cầu Giấy, Thành phố Hà Nội', N'TE', N'e002.jpg', N'binhtran@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E003', N'Lê Hồng Phúc', CAST(N'1995-02-10' AS Date), N'0978123999', N'phucle@gmail.com', N'79 Cầu Giấy, Phường Cầu Giấy, Thành phố Hà Nội', N'ST', N'e003.jpg', N'phucle@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E004', N'Phạm Quang Dũng', CAST(N'1994-04-18' AS Date), N'0911223344', N'dungpham@gmail.com', N'Thôn Mỹ Đà, Xã Hoằng Hóa, Tỉnh Thanh Hóa', N'ST', N'e004.jpg', N'dungpham@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E005', N'Hoàng Thị Mai', CAST(N'1996-09-07' AS Date), N'0933445566', N'maihoang@gmail.com', N'1194 Láng, Phường Láng, Thành phố Hà Nội', N'SL', N'e005.jpg', N'maihoang@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E006', N'Đỗ Minh Huy', CAST(N'1997-11-23' AS Date), N'0977888999', N'huydo@gmail.com', N'Thôn 5, Xã Tiên Lữ, Tỉnh Hưng Yên', N'SL', N'e006.jpg', N'huydo@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E007', N'Vũ Ngọc Lan', CAST(N'1995-01-15' AS Date), N'0922334455', N'lanvu@gmail.com', N'thôn Cự Đà, Xã Hoằng Hóa, Tỉnh Thanh Hóa', N'TE', N'e007.jpg', N'lanvu@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E008', N'Nguyễn Thanh Hương', CAST(N'1998-03-03' AS Date), N'0919988776', N'huongnguyen@gmail.com', N'Thôn 1, Xã Kim Sơn, Tỉnh Ninh Bình', N'ST', N'e008.jpg', N'huongnguyen@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E009', N'Phan Anh Tuấn', CAST(N'1992-10-10' AS Date), N'0944112233', N'tuanphan@gmail.com', N'30 Nguyễn Phong Sắc, Phường Cầu Giấy, Thành phố Hà Nội', N'TE', N'e009.jpg', N'tuanphan@gmail.com', N'123456', 1);
INSERT INTO Employee (employee_id, employee_name, date_of_birth, phone_number, email, address, role_id, avatar, username, password, active)
VALUES (N'E010', N'Lý Hồng Phát', CAST(N'1988-08-08' AS Date), N'0905001122', N'phatly@gmail.com', N'400 Xã Đàn, Phường Đống Đa, Thành phố Hà Nội', N'SL', N'e010.jpg', N'phatly@gmail.com', N'123456', 1);

-- ========== CUSTOMER ==========
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C001', N'Nguyễn Minh Tuấn', CAST(N'1999-09-10' AS Date), N'0905123456', N'39 Châu Quỳ, Phường Long Biên, Thành phố Hà Nội', N'tuannguyen@gmail.com', N'c001.jpg', N'tuannguyen@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C002', N'Trần Hoài Nam', CAST(N'1998-12-22' AS Date), N'0906554321', N'77 Giang Văn Minh, Phường An Phú, Thành phố Hồ Chí Minh', N'namtran@gmail.com', N'c002.jpg', N'namtran@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C003', N'Lê Quốc Cường', CAST(N'1997-05-15' AS Date), N'0904111222', N'68 Cầu Giấy, Phường Cầu Giấy, Thành phố Hà Nội', N'cuongle@gmail.com', N'c003.jpg', N'cuongle@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C004', N'Phạm Anh Thư', CAST(N'2000-01-20' AS Date), N'0933555666', N'33 Thành Thái, Phường Bà Rịa, Thành phố Hồ Chí Minh', N'thupham@gmail.com', N'c004.jpg', N'thupham@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C005', N'Nguyễn Đức Hòa', CAST(N'1995-03-03' AS Date), N'0911999333', N'382 Duy Tân, Phường An Hải, Thành phố Đà Nẵng', N'hoaduc@gmail.com', N'c005.jpg', N'hoaduc@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C006', N'Vũ Quỳnh Chi', CAST(N'1999-06-12' AS Date), N'0977123456', N'30 Hàng Bông, Phường Hoàn Kiếm, Thành phố Hà Nội', N'chivu@gmail.com', N'c006.jpg', N'chivu@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C007', N'Hoàng Nhật Nam', CAST(N'1998-09-22' AS Date), N'0939333444', N'Thôn Nội Tý, Xã Hoằng Hóa, Tỉnh Thanh Hóa', N'namhoang@gmail.com', N'c007.jpg', N'namhoang@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C008', N'Phan Minh Hiếu', CAST(N'2001-04-10' AS Date), N'0919888777', N'Thôn 5, Xã Kim Sơn, Tỉnh Ninh Bình', N'hieuphan@gmail.com', N'c008.jpg', N'hieuphan@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C009', N'Lý Thanh Hằng', CAST(N'2002-12-25' AS Date), N'0908777666', N'Thôn 2, Xã Tiên Lữ, Tỉnh Hưng Yên', N'hangly@gmail.com', N'c009.jpg', N'hangly@gmail.com', N'123456', 1);
INSERT INTO Customer (customer_id, customer_name, date_of_birth, phone_number, address, email, avatar, username, password, active)
VALUES (N'C010', N'Đặng Hải Long', CAST(N'1994-10-30' AS Date), N'0905777888', N'1174 Láng, Phường Láng, Thành phố Hà Nội', N'longdang@gmail.com', N'c010.jpg', N'longdang@gmail.com', N'123456', 1);

-- ========== PRODUCT ==========
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P001', N'Dell Alienware', N'16X Aurora AC2025', 36, CAST(72000000.00 AS Decimal(18, 2)), CAST(68990000.00 AS Decimal(18, 2)), N'16inch QHD+ 240Hz', N'1080p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(2.70 AS Decimal(18, 2)), N'97Wh', N'B001', N'p001.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P002', N'Dell Alienware', N'M17 R8 Pro', 24, CAST(85000000.00 AS Decimal(18, 2)), CAST(81990000.00 AS Decimal(18, 2)), N'18inch QHD+ 240Hz', N'1080p', N'2x USB 3.2 Gen 2 Type-A, 1x USB 3.2 Gen 2 Type-C®, 1x Thunderbolt™ 4, 1x HDMI 2.1, 1x RJ45 LAN, 1x Jack tai nghe 3.5mm', CAST(3.10 AS Decimal(18, 2)), N'90Wh', N'B001', N'p002.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P003', N'Dell Alienware', N'X17 Phantom 2025', 36, CAST(89000000.00 AS Decimal(18, 2)), CAST(86990000.00 AS Decimal(18, 2)), N'17inch Mini LED 250Hz', N'1080p', N'2x Thunderbolt™ 4, 2x USB 3.2 Gen 2 Type-A, 1x HDMI 2.1, 1x RJ45, 1x Jack tai nghe 3.5mm', CAST(3.00 AS Decimal(18, 2)), N'99Wh', N'B001', N'p003.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P004', N'Dell Inspiron', N'14 Slim 2025', 36, CAST(82000000.00 AS Decimal(18, 2)), CAST(79990000.00 AS Decimal(18, 2)), N'16inch WQXGA 240Hz', N'1080p', N'1x HDMI 2.1, 2x Thunderbolt™ 4, 2x USB 3.2 Gen 2, 1x RJ45 Ethernet, 1x Audio Combo Jack', CAST(2.55 AS Decimal(18, 2)), N'99Wh', N'B001', N'p004.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P005', N'Dell Inspiron', N'15 Flex 2024', 24, CAST(72000000.00 AS Decimal(18, 2)), CAST(69990000.00 AS Decimal(18, 2)), N'16inch QHD+ 240Hz', N'1080p', N'1x HDMI 2.1, 2x Thunderbolt™ 4, 2x USB 3.2 Gen 1, 1x Jack tai nghe, 1x RJ45 LAN', CAST(2.45 AS Decimal(18, 2)), N'83Wh', N'B001', N'p005.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P006', N'Dell Inspiron', N'16 Plus 2025', 36, CAST(89000000.00 AS Decimal(18, 2)), CAST(85990000.00 AS Decimal(18, 2)), N'17inch QHD+ 240Hz', N'1080p', N'2x Thunderbolt™ 4, 2x USB 3.2 Gen 2, 1x HDMI 2.1, 1x RJ45, 1x Jack tai nghe', CAST(3.10 AS Decimal(18, 2)), N'99Wh', N'B001', N'p006.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P007', N'Lenovo ThinkPad', N'X16 Gen 5', 36, CAST(95000000.00 AS Decimal(18, 2)), CAST(92990000.00 AS Decimal(18, 2)), N'16inch Liquid Retina XDR', N'1080p', N'3x Thunderbolt™ 4, 1x HDMI, 1x SDXC, 1x MagSafe 3, 1x Jack tai nghe', CAST(2.16 AS Decimal(18, 2)), N'100Wh', N'B002', N'p007.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P008', N'Lenovo ThinkPad', N'P1 Gen 6', 36, CAST(69000000.00 AS Decimal(18, 2)), CAST(66990000.00 AS Decimal(18, 2)), N'16inch OLED 3.5K', N'1080p', N'2x Thunderbolt™ 4, 1x USB-C 3.2 Gen 2, 1x HDMI 2.1, 1x Jack tai nghe', CAST(2.10 AS Decimal(18, 2)), N'86Wh', N'B002', N'p008.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P009', N'Lenovo ThinkPad', N'T14 Gen 6', 36, CAST(72000000.00 AS Decimal(18, 2)), CAST(69990000.00 AS Decimal(18, 2)), N'14inch 3K OLED + 12.7inch', N'1080p', N'2x USB-C (Thunderbolt™ 4), 1x HDMI 2.1, 1x USB-A 3.2, 1x Audio Jack', CAST(1.70 AS Decimal(18, 2)), N'75Wh', N'B002', N'p009.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P010', N'Lenovo Legion', N'7 Pro 2024', 36, CAST(54000000.00 AS Decimal(18, 2)), CAST(51990000.00 AS Decimal(18, 2)), N'14inch 2.8K OLED', N'1080p', N'2x Thunderbolt™ 4, 2x USB 3.2, 1x HDMI 2.0b, 1x Audio Combo Jack', CAST(1.20 AS Decimal(18, 2)), N'57Wh', N'B002', N'p010.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P011', N'Lenovo Legion', N'Slim 5 2025', 24, CAST(27000000.00 AS Decimal(18, 2)), CAST(25990000.00 AS Decimal(18, 2)), N'14inch 2.8K OLED', N'1080p', N'2x USB-C (Thunderbolt™ 4), 2x USB-A, 1x HDMI 2.1, 1x Audio Combo Jack', CAST(1.39 AS Decimal(18, 2)), N'59Wh', N'B002', N'p011.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P012', N'Lenovo Legion', N'9i Gen 8', 24, CAST(29000000.00 AS Decimal(18, 2)), CAST(27990000.00 AS Decimal(18, 2)), N'14inch 2.8K OLED', N'1080p', N'2x USB-C (Thunderbolt™ 4), 2x USB-A, 1x HDMI 2.1, 1x Jack tai nghe', CAST(1.30 AS Decimal(18, 2)), N'65Wh', N'B002', N'p012.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P013', N'Lenovo Legion', N'5i Gen 8', 36, CAST(46000000.00 AS Decimal(18, 2)), CAST(43990000.00 AS Decimal(18, 2)), N'16inch 4K UHD+', N'1080p', N'2x Thunderbolt™ 4, 2x USB-A, 1x HDMI 2.1, 1x RJ45, 1x Audio Combo Jack', CAST(1.80 AS Decimal(18, 2)), N'82Wh', N'B002', N'p013.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P014', N'HP Pavilion', N'14 Aero 2024', 24, CAST(37000000.00 AS Decimal(18, 2)), CAST(34990000.00 AS Decimal(18, 2)), N'15.3inch Retina', N'1080p', N'2x Thunderbolt™ 4, 1x MagSafe 3, 1x Jack tai nghe 3.5mm', CAST(1.49 AS Decimal(18, 2)), N'66Wh', N'B003', N'p014.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P015', N'HP Pavilion', N'15 Max 2025', 24, CAST(25000000.00 AS Decimal(18, 2)), CAST(23990000.00 AS Decimal(18, 2)), N'16inch FHD+', N'1080p', N'1x HDMI 1.4, 2x USB-A, 1x USB-C, 1x Jack tai nghe', CAST(1.82 AS Decimal(18, 2)), N'54Wh', N'B003', N'p015.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P016', N'HP Pavilion', N'16 Ultra 2024', 24, CAST(23000000.00 AS Decimal(18, 2)), CAST(21990000.00 AS Decimal(18, 2)), N'14inch 2.8K OLED', N'1080p', N'1x HDMI 2.1, 2x USB-A, 1x USB-C, 1x Audio Combo Jack', CAST(1.39 AS Decimal(18, 2)), N'50Wh', N'B003', N'p016.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P017', N'HP Pavilion', N'13 Slim 2025', 24, CAST(22000000.00 AS Decimal(18, 2)), CAST(20990000.00 AS Decimal(18, 2)), N'14inch 2.2K IPS', N'1080p', N'1x HDMI 2.1, 2x USB-A, 1x USB-C, 1x Jack tai nghe', CAST(1.37 AS Decimal(18, 2)), N'45Wh', N'B003', N'p017.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P018', N'HP Omen', N'16 Shadow 2024', 12, CAST(19000000.00 AS Decimal(18, 2)), CAST(17990000.00 AS Decimal(18, 2)), N'15.6inch FHD', N'720p', N'1x HDMI 2.1, 3x USB-A, 1x USB-C, 1x Jack tai nghe', CAST(1.70 AS Decimal(18, 2)), N'50Wh', N'B003', N'p018.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P019', N'HP Omen', N'17 Fury 2025', 12, CAST(17000000.00 AS Decimal(18, 2)), CAST(15990000.00 AS Decimal(18, 2)), N'15.6inch FHD', N'720p', N'2x USB-A, 1x USB-C, 1x HDMI 1.4b, 1x Audio Combo Jack', CAST(1.63 AS Decimal(18, 2)), N'47Wh', N'B003', N'p019.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P020', N'HP Omen', N'15 Stealth 2024', 12, CAST(16000000.00 AS Decimal(18, 2)), CAST(14990000.00 AS Decimal(18, 2)), N'14inch FHD', N'720p', N'1x HDMI, 2x USB-A, 1x USB-C, 1x Audio Combo Jack', CAST(1.45 AS Decimal(18, 2)), N'39Wh', N'B003', N'p020.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P021', N'HP Omen', N'14 Phantom 2025', 12, CAST(16000000.00 AS Decimal(18,2)), CAST(14990000.00 AS Decimal(18,2)), N'14inch FHD', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(1.50 AS Decimal(18,2)), N'41Wh', N'B003', N'p021.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P022', N'ASUS ExpertBook', N'B14 Pro 2024', 24, CAST(19000000.00 AS Decimal(18,2)), CAST(17990000.00 AS Decimal(18,2)), N'14inch FHD', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(1.45 AS Decimal(18,2)), N'48Wh', N'B004', N'p022.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P023', N'ASUS ExpertBook', N'B15 Elite 2025', 12, CAST(15000000.00 AS Decimal(18,2)), CAST(13990000.00 AS Decimal(18,2)), N'15.6inch FHD', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(1.65 AS Decimal(18,2)), N'41Wh', N'B004', N'p023.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P024', N'ASUS ExpertBook', N'B16 Nano 2024', 12, CAST(13000000.00 AS Decimal(18,2)), CAST(11990000.00 AS Decimal(18,2)), N'15.6inch FHD', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(1.78 AS Decimal(18,2)), N'40Wh', N'B004', N'p024.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P025', N'ASUS ExpertBook', N'B14 Air 2024', 12, CAST(11000000.00 AS Decimal(18,2)), CAST(9990000.00 AS Decimal(18,2)), N'14inch FHD', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(1.39 AS Decimal(18,2)), N'42Wh', N'B004', N'p025.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P026', N'ASUS TUF Gaming', N'A17 2024', 24, CAST(24000000.00 AS Decimal(18,2)), CAST(22990000.00 AS Decimal(18,2)), N'15.6inch FHD 144Hz', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(2.20 AS Decimal(18,2)), N'53Wh', N'B004', N'p026.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P027', N'ASUS TUF Gaming', N'F15 Elite 2025', 24, CAST(29000000.00 AS Decimal(18,2)), CAST(27990000.00 AS Decimal(18,2)), N'13.6inch Retina', N'1080p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(1.24 AS Decimal(18,2)), N'52Wh', N'B004', N'p027.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P028', N'ASUS TUF Gaming', N'X16 Titan 2024', 24, CAST(20000000.00 AS Decimal(18,2)), CAST(18990000.00 AS Decimal(18,2)), N'14inch FHD', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(1.47 AS Decimal(18,2)), N'54Wh', N'B004', N'p028.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P029', N'ASUS TUF Gaming', N'A15 Ultra 2025', 24, CAST(27000000.00 AS Decimal(18,2)), CAST(25990000.00 AS Decimal(18,2)), N'15.6inch FHD 144Hz', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(2.30 AS Decimal(18,2)), N'90Wh', N'B004', N'p029.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P030', N'ASUS TUF Gaming', N'A17 2025', 24, CAST(25000000.00 AS Decimal(18,2)), CAST(23990000.00 AS Decimal(18,2)), N'15.6inch FHD 144Hz', N'720p', N'1x Thunderbolt™ 4 (hỗ trợ DisplayPort™ 2.1), 1x USB 3.2 Gen 2 Type-C® (hỗ trợ Power Delivery), 2x USB 3.2 Gen 1 Type-A, 1x HDMI 2.1, 1x RJ45 Ethernet (1Gbps), 1x Jack tai nghe 3.5mm', CAST(2.48 AS Decimal(18,2)), N'70Wh', N'B004', N'p030.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P031', N'Lenovo LOQ', N'15 Gen 4', 12, CAST(14000000.00 AS Decimal(18,2)), CAST(12990000.00 AS Decimal(18,2)), N'14inch FHD', N'720p', N'1x USB Type-C, 2x USB Type-A, 1x HDMI, 1x Jack tai nghe', CAST(1.41 AS Decimal(18,2)), N'43Wh', N'B002', N'p031.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P032', N'Lenovo LOQ', N'16 Gen 6', 24, CAST(24000000.00 AS Decimal(18,2)), CAST(22990000.00 AS Decimal(18,2)), N'15.6inch FHD 144Hz', N'1080p', N'1x USB Type-C, 2x USB Type-A, 1x HDMI, 1x RJ45 Ethernet', CAST(2.37 AS Decimal(18,2)), N'70Wh', N'B002', N'p032.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P033', N'Lenovo LOQ', N'17 Gen 7', 24, CAST(22000000.00 AS Decimal(18,2)), CAST(20990000.00 AS Decimal(18,2)), N'13.3inch FHD', N'720p', N'2x Thunderbolt™ 4, 1x USB Type-A, 1x Audio Combo Jack', CAST(1.30 AS Decimal(18,2)), N'51Wh', N'B002', N'p033.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P034', N'Lenovo LOQ', N'14 Gen 4', 36, CAST(33000000.00 AS Decimal(18,2)), CAST(31990000.00 AS Decimal(18,2)), N'16inch OLED 3K+', N'1080p', N'2x Thunderbolt™ 4, 1x USB Type-A, 1x HDMI 2.1', CAST(1.90 AS Decimal(18,2)), N'83Wh', N'B002', N'p034.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P035', N'Lenovo LOQ', N'15 Gen 5', 12, CAST(15000000.00 AS Decimal(18,2)), CAST(13990000.00 AS Decimal(18,2)), N'15.6inch FHD', N'720p', N'1x USB Type-C, 2x USB Type-A, 1x HDMI 1.4', CAST(1.65 AS Decimal(18,2)), N'42Wh', N'B002', N'p035.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P036', N'Dell XPS', N'14 Carbon 2024', 24, CAST(35000000.00 AS Decimal(18,2)), CAST(33990000.00 AS Decimal(18,2)), N'13.4inch FHD+', N'1080p', N'2x Thunderbolt™ 4 (USB Type-C)', CAST(1.17 AS Decimal(18,2)), N'51Wh', N'B001', N'p036.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P037', N'Dell XPS', N'16 Infinity 2024', 36, CAST(29000000.00 AS Decimal(18,2)), CAST(27990000.00 AS Decimal(18,2)), N'14inch FHD', N'1080p', N'2x Thunderbolt™ 4, 2x USB Type-A, 1x HDMI 2.0', CAST(1.48 AS Decimal(18,2)), N'58Wh', N'B001', N'p037.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P038', N'Dell XPS', N'13 Evo 2024', 12, CAST(17000000.00 AS Decimal(18,2)), CAST(15990000.00 AS Decimal(18,2)), N'15.6inch FHD', N'720p', N'1x USB Type-C, 2x USB Type-A, 1x HDMI 1.4', CAST(1.78 AS Decimal(18,2)), N'41Wh', N'B001', N'p038.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P039', N'Dell XPS', N'15 Ultra 2024', 24, CAST(33000000.00 AS Decimal(18,2)), CAST(31990000.00 AS Decimal(18,2)), N'16inch WQXGA 165Hz', N'1080p', N'4x USB, 1x HDMI 2.1, 1x Ethernet, 1x Jack 3.5mm', CAST(2.45 AS Decimal(18,2)), N'80Wh', N'B001', N'p039.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P040', N'Dell XPS', N'13 Carbon 2025', 12, CAST(12000000.00 AS Decimal(18,2)), CAST(10990000.00 AS Decimal(18,2)), N'14inch FHD', N'720p', N'2x USB Type-A, 1x USB Type-C, 1x HDMI, 1x Jack tai nghe', CAST(1.50 AS Decimal(18,2)), N'45Wh', N'B001', N'p040.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P041', N'Dell Alienware', N'16X Aurora AC2024', 36, CAST(34920000.00 AS Decimal(18,2)), CAST(33420000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p041.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P042', N'Dell Alienware', N'M17 R9 Pro', 36, CAST(35040000.00 AS Decimal(18,2)), CAST(33540000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p042.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P043', N'Dell Alienware', N'X18 Phantom 2024', 36, CAST(35160000.00 AS Decimal(18,2)), CAST(33660000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p043.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P044', N'Dell Inspiron', N'Slim 2025', 36, CAST(35280000.00 AS Decimal(18,2)), CAST(33780000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p044.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P045', N'Dell Inspiron', N'15 Flex 2025', 36, CAST(35400000.00 AS Decimal(18,2)), CAST(33900000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p045.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P046', N'Dell Inspiron', N'16 Plus 2024', 36, CAST(35520000.00 AS Decimal(18,2)), CAST(34020000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p046.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P047', N'Dell XPS', N'14 Carbon 2025', 36, CAST(35640000.00 AS Decimal(18,2)), CAST(34140000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p047.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P048', N'Dell XPS', N'16 Infinity 2025', 36, CAST(35760000.00 AS Decimal(18,2)), CAST(34260000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p048.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P049', N'Dell XPS', N'13 Evo 2025', 36, CAST(35880000.00 AS Decimal(18,2)), CAST(34380000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B001', N'p049.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P050', N'Lenovo ThinkPad', N'X16 Gen 6', 36, CAST(36000000.00 AS Decimal(18,2)), CAST(34500000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p050.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P051', N'Lenovo ThinkPad', N'P1 Gen 7', 36, CAST(36120000.00 AS Decimal(18,2)), CAST(34620000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p051.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P052', N'Lenovo ThinkPad', N'T14 Gen 5', 36, CAST(36240000.00 AS Decimal(18,2)), CAST(34740000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p052.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P053', N'Lenovo Legion', N'7 Pro 2025', 36, CAST(36360000.00 AS Decimal(18,2)), CAST(34860000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p053.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P054', N'Lenovo Legion', N'Slim 5 2024', 36, CAST(36480000.00 AS Decimal(18,2)), CAST(34980000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p054.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P055', N'Lenovo Legion', N'9i Gen 9', 36, CAST(36600000.00 AS Decimal(18,2)), CAST(35100000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p055.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P056', N'Lenovo LOQ', N'15 Gen 6', 36, CAST(36720000.00 AS Decimal(18,2)), CAST(35220000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p056.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P057', N'Lenovo LOQ', N'16 Gen 7', 36, CAST(36840000.00 AS Decimal(18,2)), CAST(35340000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p057.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P058', N'Lenovo LOQ', N'17 Gen 8', 36, CAST(36960000.00 AS Decimal(18,2)), CAST(35460000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B002', N'p058.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P059', N'HP Pavilion', N'14 Aero 2025', 24, CAST(37080000.00 AS Decimal(18,2)), CAST(35580000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B003', N'p059.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P060', N'HP Pavilion', N'15 Max 2024', 24, CAST(37200000.00 AS Decimal(18,2)), CAST(35700000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B003', N'p060.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P061', N'HP Pavilion', N'16 Ultra 2025', 24, CAST(37320000.00 AS Decimal(18,2)), CAST(35820000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B003', N'p061.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P062', N'HP Omen', N'16 Shadow 2025', 24, CAST(37440000.00 AS Decimal(18,2)), CAST(35940000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B003', N'p062.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P063', N'HP Omen', N'17 Fury 2024', 24, CAST(37560000.00 AS Decimal(18,2)), CAST(36060000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B003', N'p063.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P064', N'HP Omen', N'15 Stealth 2025', 24, CAST(37680000.00 AS Decimal(18,2)), CAST(36180000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B003', N'p064.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P065', N'ASUS ExpertBook', N'B14 Pro 2025', 24, CAST(37800000.00 AS Decimal(18,2)), CAST(36300000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B004', N'p065.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P066', N'ASUS ExpertBook', N'B15 Elite 2024', 24, CAST(37920000.00 AS Decimal(18,2)), CAST(36420000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B004', N'p066.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P067', N'ASUS ExpertBook', N'B16 Nano 2025', 24, CAST(38040000.00 AS Decimal(18,2)), CAST(36540000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B004', N'p067.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P068', N'ASUS TUF Gaming', N'A16 2025', 24, CAST(38160000.00 AS Decimal(18,2)), CAST(36660000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B004', N'p068.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P069', N'ASUS TUF Gaming', N'F15 Elite 2024', 24, CAST(38280000.00 AS Decimal(18,2)), CAST(36780000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B004', N'p069.jpg', 1);
INSERT INTO Product (product_id, product_name, product_model, warranty_period, original_selling_price, selling_price, screen, camera, connect, weight, pin, brand_id, avatar, active)
VALUES (N'P070', N'ASUS TUF Gaming', N'X16 Titan 2025', 24, CAST(38400000.00 AS Decimal(18,2)), CAST(36900000.00 AS Decimal(18,2)), N'15.6inch FHD', N'1080p', N'USB-C, USB-A, HDMI', CAST(1.8 AS Decimal(18,2)), N'60Wh', N'B004', N'p070.jpg', 0);


-- ========== PRODUCTIMAGE ==========
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p001_1.jpg', N'P001');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p001_2.jpg', N'P001');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p001_3.jpg', N'P001');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p001_4.jpg', N'P001');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p002_1.jpg', N'P002');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p002_2.jpg', N'P002');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p002_3.jpg', N'P002');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p002_4.jpg', N'P002');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p003_1.jpg', N'P003');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p003_2.jpg', N'P003');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p003_3.jpg', N'P003');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p003_4.jpg', N'P003');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p004_1.jpg', N'P004');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p004_2.jpg', N'P004');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p004_3.jpg', N'P004');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p004_4.jpg', N'P004');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p005_1.jpg', N'P005');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p005_2.jpg', N'P005');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p005_3.jpg', N'P005');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p005_4.jpg', N'P005');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p006_1.jpg', N'P006');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p006_2.jpg', N'P006');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p006_3.jpg', N'P006');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p006_4.jpg', N'P006');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p007_1.jpg', N'P007');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p007_2.jpg', N'P007');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p007_3.jpg', N'P007');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p007_4.jpg', N'P007');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p008_1.jpg', N'P008');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p008_2.jpg', N'P008');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p008_3.jpg', N'P008');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p008_4.jpg', N'P008');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p009_1.jpg', N'P009');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p009_2.jpg', N'P009');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p009_3.jpg', N'P009');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p009_4.jpg', N'P009');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p010_1.jpg', N'P010');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p010_2.jpg', N'P010');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p010_3.jpg', N'P010');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p010_4.jpg', N'P010');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p011_1.jpg', N'P011');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p011_2.jpg', N'P011');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p011_3.jpg', N'P011');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p011_4.jpg', N'P011');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p012_1.jpg', N'P012');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p012_2.jpg', N'P012');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p012_3.jpg', N'P012');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p012_4.jpg', N'P012');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p013_1.jpg', N'P013');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p013_2.jpg', N'P013');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p013_3.jpg', N'P013');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p013_4.jpg', N'P013');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p014_1.jpg', N'P014');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p014_2.jpg', N'P014');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p014_3.jpg', N'P014');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p014_4.jpg', N'P014');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p015_1.jpg', N'P015');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p015_2.jpg', N'P015');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p015_3.jpg', N'P015');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p015_4.jpg', N'P015');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p016_1.jpg', N'P016');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p016_2.jpg', N'P016');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p016_3.jpg', N'P016');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p016_4.jpg', N'P016');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p017_1.jpg', N'P017');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p017_2.jpg', N'P017');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p017_3.jpg', N'P017');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p017_4.jpg', N'P017');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p018_1.jpg', N'P018');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p018_2.jpg', N'P018');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p018_3.jpg', N'P018');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p018_4.jpg', N'P018');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p019_1.jpg', N'P019');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p019_2.jpg', N'P019');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p019_3.jpg', N'P019');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p019_4.jpg', N'P019');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p020_1.jpg', N'P020');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p020_2.jpg', N'P020');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p020_3.jpg', N'P020');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p020_4.jpg', N'P020');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p021_1.jpg', N'P021');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p021_2.jpg', N'P021');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p021_3.jpg', N'P021');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p021_4.jpg', N'P021');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p022_1.jpg', N'P022');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p022_2.jpg', N'P022');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p022_3.jpg', N'P022');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p022_4.jpg', N'P022');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p023_1.jpg', N'P023');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p023_2.jpg', N'P023');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p023_3.jpg', N'P023');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p023_4.jpg', N'P023');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p024_1.jpg', N'P024');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p024_2.jpg', N'P024');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p024_3.jpg', N'P024');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p024_4.jpg', N'P024');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p025_1.jpg', N'P025');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p025_2.jpg', N'P025');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p025_3.jpg', N'P025');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p025_4.jpg', N'P025');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p026_1.jpg', N'P026');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p026_2.jpg', N'P026');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p026_3.jpg', N'P026');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p026_4.jpg', N'P026');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p027_1.jpg', N'P027');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p027_2.jpg', N'P027');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p027_3.jpg', N'P027');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p027_4.jpg', N'P027');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p028_1.jpg', N'P028');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p028_2.jpg', N'P028');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p028_3.jpg', N'P028');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p028_4.jpg', N'P028');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p029_1.jpg', N'P029');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p029_2.jpg', N'P029');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p029_3.jpg', N'P029');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p029_4.jpg', N'P029');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p030_1.jpg', N'P030');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p030_2.jpg', N'P030');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p030_3.jpg', N'P030');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p030_4.jpg', N'P030');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p031_1.jpg', N'P031');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p031_2.jpg', N'P031');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p031_3.jpg', N'P031');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p031_4.jpg', N'P031');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p032_1.jpg', N'P032');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p032_2.jpg', N'P032');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p032_3.jpg', N'P032');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p032_4.jpg', N'P032');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p033_1.jpg', N'P033');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p033_2.jpg', N'P033');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p033_3.jpg', N'P033');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p033_4.jpg', N'P033');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p034_1.jpg', N'P034');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p034_2.jpg', N'P034');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p034_3.jpg', N'P034');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p034_4.jpg', N'P034');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p035_1.jpg', N'P035');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p035_2.jpg', N'P035');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p035_3.jpg', N'P035');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p035_4.jpg', N'P035');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p036_1.jpg', N'P036');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p036_2.jpg', N'P036');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p036_3.jpg', N'P036');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p036_4.jpg', N'P036');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p037_1.jpg', N'P037');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p037_2.jpg', N'P037');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p037_3.jpg', N'P037');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p037_4.jpg', N'P037');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p038_1.jpg', N'P038');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p038_2.jpg', N'P038');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p038_3.jpg', N'P038');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p038_4.jpg', N'P038');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p039_1.jpg', N'P039');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p039_2.jpg', N'P039');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p039_3.jpg', N'P039');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p039_4.jpg', N'P039');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p040_1.jpg', N'P040');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p040_2.jpg', N'P040');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p040_3.jpg', N'P040');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p040_4.jpg', N'P040');

-- Product P041 → P070
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p041_1.jpg', N'P041');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p041_2.jpg', N'P041');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p041_3.jpg', N'P041');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p041_4.jpg', N'P041');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p042_1.jpg', N'P042');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p042_2.jpg', N'P042');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p042_3.jpg', N'P042');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p042_4.jpg', N'P042');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p043_1.jpg', N'P043');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p043_2.jpg', N'P043');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p043_3.jpg', N'P043');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p043_4.jpg', N'P043');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p044_1.jpg', N'P044');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p044_2.jpg', N'P044');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p044_3.jpg', N'P044');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p044_4.jpg', N'P044');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p045_1.jpg', N'P045');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p045_2.jpg', N'P045');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p045_3.jpg', N'P045');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p045_4.jpg', N'P045');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p046_1.jpg', N'P046');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p046_2.jpg', N'P046');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p046_3.jpg', N'P046');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p046_4.jpg', N'P046');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p047_1.jpg', N'P047');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p047_2.jpg', N'P047');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p047_3.jpg', N'P047');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p047_4.jpg', N'P047');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p048_1.jpg', N'P048');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p048_2.jpg', N'P048');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p048_3.jpg', N'P048');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p048_4.jpg', N'P048');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p049_1.jpg', N'P049');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p049_2.jpg', N'P049');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p049_3.jpg', N'P049');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p049_4.jpg', N'P049');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p050_1.jpg', N'P050');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p050_2.jpg', N'P050');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p050_3.jpg', N'P050');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p050_4.jpg', N'P050');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p051_1.jpg', N'P051');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p051_2.jpg', N'P051');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p051_3.jpg', N'P051');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p051_4.jpg', N'P051');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p052_1.jpg', N'P052');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p052_2.jpg', N'P052');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p052_3.jpg', N'P052');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p052_4.jpg', N'P052');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p053_1.jpg', N'P053');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p053_2.jpg', N'P053');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p053_3.jpg', N'P053');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p053_4.jpg', N'P053');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p054_1.jpg', N'P054');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p054_2.jpg', N'P054');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p054_3.jpg', N'P054');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p054_4.jpg', N'P054');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p055_1.jpg', N'P055');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p055_2.jpg', N'P055');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p055_3.jpg', N'P055');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p055_4.jpg', N'P055');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p056_1.jpg', N'P056');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p056_2.jpg', N'P056');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p056_3.jpg', N'P056');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p056_4.jpg', N'P056');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p057_1.jpg', N'P057');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p057_2.jpg', N'P057');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p057_3.jpg', N'P057');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p057_4.jpg', N'P057');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p058_1.jpg', N'P058');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p058_2.jpg', N'P058');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p058_3.jpg', N'P058');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p058_4.jpg', N'P058');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p059_1.jpg', N'P059');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p059_2.jpg', N'P059');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p059_3.jpg', N'P059');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p059_4.jpg', N'P059');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p060_1.jpg', N'P060');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p060_2.jpg', N'P060');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p060_3.jpg', N'P060');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p060_4.jpg', N'P060');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p061_1.jpg', N'P061');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p061_2.jpg', N'P061');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p061_3.jpg', N'P061');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p061_4.jpg', N'P061');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p062_1.jpg', N'P062');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p062_2.jpg', N'P062');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p062_3.jpg', N'P062');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p062_4.jpg', N'P062');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p063_1.jpg', N'P063');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p063_2.jpg', N'P063');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p063_3.jpg', N'P063');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p063_4.jpg', N'P063');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p064_1.jpg', N'P064');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p064_2.jpg', N'P064');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p064_3.jpg', N'P064');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p064_4.jpg', N'P064');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p065_1.jpg', N'P065');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p065_2.jpg', N'P065');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p065_3.jpg', N'P065');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p065_4.jpg', N'P065');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p066_1.jpg', N'P066');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p066_2.jpg', N'P066');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p066_3.jpg', N'P066');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p066_4.jpg', N'P066');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p067_1.jpg', N'P067');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p067_2.jpg', N'P067');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p067_3.jpg', N'P067');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p067_4.jpg', N'P067');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p068_1.jpg', N'P068');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p068_2.jpg', N'P068');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p068_3.jpg', N'P068');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p068_4.jpg', N'P068');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p069_1.jpg', N'P069');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p069_2.jpg', N'P069');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p069_3.jpg', N'P069');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p069_4.jpg', N'P069');

INSERT INTO ProductImage (image_id, product_id) VALUES (N'p070_1.jpg', N'P070');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p070_2.jpg', N'P070');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p070_3.jpg', N'P070');
INSERT INTO ProductImage (image_id, product_id) VALUES (N'p070_4.jpg', N'P070');



---- ========== PRODUCTCONFIGURATION ==========
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF001',N'Core i5-11800H',N'8GB',N'512GB SSD',N'NVIDIA® GeForce RTX™ 3050 4GB GDDR6',CAST(0.00 AS Decimal(18,2)),2,N'P001');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF002',N'Core i7-10750H',N'16GB',N'1TB SSD',N'NVIDIA® GeForce RTX™ 3060 6GB GDDR6',CAST(500000.00 AS Decimal(18,2)),2,N'P001');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF003',N'Core i9-10750H',N'32GB',N'1TB SSD',N'NVIDIA® GeForce RTX™ 4070 8GB GDDR6',CAST(1300000.00 AS Decimal(18,2)),1,N'P001');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF004',N'Core i5-10750H',N'8GB',N'512GB SSD',N'NVIDIA® GeForce RTX™ 3050 4GB',CAST(0.00 AS Decimal(18,2)),2,N'P002');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF005',N'Core i7-11800H',N'16GB',N'1TB SSD',N'NVIDIA® GeForce RTX™ 4060 8GB',CAST(1200000.00 AS Decimal(18,2)),2,N'P002');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF006',N'Core i7-10750H',N'8GB',N'512GB SSD',N'NVIDIA® GeForce RTX™ 4050 6GB',CAST(0.00 AS Decimal(18,2)),3,N'P003');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF007',N'Core i7-11800H',N'16GB',N'1TB SSD',N'NVIDIA® GeForce RTX™ 4060 8GB',CAST(500000.00 AS Decimal(18,2)),2,N'P003');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF008',N'Core i9-10750H',N'32GB',N'1TB SSD',N'NVIDIA® GeForce RTX™ 4070 8GB',CAST(1200000.00 AS Decimal(18,2)),1,N'P003');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF009',N'Ryzen 7-8945HX',N'8GB',N'512GB SSD',N'NVIDIA® GeForce GTX™ 1650 4GB',CAST(0.00 AS Decimal(18,2)),4,N'P004');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF010',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'NVIDIA® GeForce RTX™ 3050 6GB',CAST(1700000.00 AS Decimal(18,2)),2,N'P004');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF011',N'Ultra 5 275 HX',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),3,N'P005');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF012',N'Ultra 7 275 HX',N'16GB',N'1TB SSD',N'NVIDIA® GeForce RTX™ 3050',CAST(700000.00 AS Decimal(18,2)),2,N'P005');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF013',N'Ultra 9 275 HX',N'32GB',N'1TB SSD',N'NVIDIA® GeForce RTX™ 4060',CAST(1600000.00 AS Decimal(18,2)),1,N'P005');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF014',N'Ryzen 5-8945HX',N'8GB',N'512GB SSD',N'NVIDIA® GTX™ 1650',CAST(0.00 AS Decimal(18,2)),3,N'P006');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF015',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(1500000.00 AS Decimal(18,2)),2,N'P006');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF016',N'Ryzen 5 H 255',N'8GB',N'256GB SSD',N'GPU 10-core',CAST(0.00 AS Decimal(18,2)),2,N'P007');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF017',N'Ryzen 7 H 255',N'16GB',N'512GB SSD',N'GPU 16-core',CAST(1800000.00 AS Decimal(18,2)),1,N'P007');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF018',N'Ultra 5 275 HX',N'16GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),2,N'P008');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF019',N'Ultra 9 275 HX',N'32GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(1900000.00 AS Decimal(18,2)),1,N'P008');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF020',N'Core i5-10750H',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),2,N'P009');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF021',N'Core i7-10750H',N'16GB',N'1TB SSD',N'Intel® Iris Xe',CAST(500000.00 AS Decimal(18,2)),2,N'P009');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF022',N'Ultra 5 275 HX',N'8GB',N'512GB SSD',N'NVIDIA® GTX™ 1650',CAST(0.00 AS Decimal(18,2)),3,N'P010');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF023',N'Ultra 9 275 HX',N'16GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(1800000.00 AS Decimal(18,2)),1,N'P010');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF024',N'Core i5-11800H',N'8GB',N'512GB SSD',N'Intel® UHD',CAST(0.00 AS Decimal(18,2)),0,N'P011');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF025',N'Core i7-10750H',N'16GB',N'1TB SSD',N'Intel® Iris Xe',CAST(1300000.00 AS Decimal(18,2)),2,N'P011');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF026',N'Core i7-10750H',N'8GB',N'256GB SSD',N'Intel® UHD',CAST(0.00 AS Decimal(18,2)),4,N'P012');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF027',N'Core i9-10750H',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(1100000.00 AS Decimal(18,2)),3,N'P012');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF028',N'Core i7-10750H',N'16GB',N'1TB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),2,N'P013');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF029',N'Core i9-11800H',N'32GB',N'1TB SSD',N'NVIDIA® RTX™ 4060',CAST(1800000.00 AS Decimal(18,2)),1,N'P013');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF030',N'Ryzen 5-8945HX',N'8GB',N'512GB SSD',N'NVIDIA® GTX™ 1650',CAST(0.00 AS Decimal(18,2)),3,N'P014');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF031',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(1700000.00 AS Decimal(18,2)),2,N'P014');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF032',N'Ryzen 7-8945HX',N'8GB',N'512GB SSD',N'Radeon Graphics',CAST(0.00 AS Decimal(18,2)),3,N'P015');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF033',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'Radeon Graphics',CAST(1600000.00 AS Decimal(18,2)),2,N'P015');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF034',N'Ryzen 9-8945HX','8GB',N'256GB SSD',N'GPU 10-core',CAST(1600000.00 AS Decimal(18,2)),1,N'P016');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF035',N'Ryzen 9-8945HX',N'16GB',N'512GB SSD',N'GPU 18-core',CAST(1300000.00 AS Decimal(18,2)),1,N'P016');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF036',N'Ultra 7 275 HX',N'8GB',N'512GB SSD',N'Intel® UHD',CAST(0.00 AS Decimal(18,2)),3,N'P017');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF037',N'Ultra 9 275 HX',N'16GB',N'1TB SSD',N'Intel® Iris Xe',CAST(1900000.00 AS Decimal(18,2)),2,N'P017');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF038',N'Ryzen 7-8945HX',N'8GB',N'512GB SSD',N'Radeon Graphics',CAST(0.00 AS Decimal(18,2)),3,N'P018');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF039',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'Radeon Graphics',CAST(1700000.00 AS Decimal(18,2)),2,N'P018');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF040',N'Ultra 5 275 HX',N'8GB',N'256GB SSD',N'Intel® UHD',CAST(0.00 AS Decimal(18,2)),4,N'P019');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF041',N'Ultra 9 275 HX',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(1400000.00 AS Decimal(18,2)),3,N'P019');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF042',N'Core i5-10750H',N'8GB',N'512GB SSD',N'NVIDIA® GTX™ 1650',CAST(0.00 AS Decimal(18,2)),3,N'P020');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF043',N'Core i7-10750H',N'16GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(1100000.00 AS Decimal(18,2)),2,N'P020');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF044',N'Core i5-10750H',N'8GB',N'512GB SSD',N'NVIDIA® GTX™ 1650',CAST(0.00 AS Decimal(18,2)),4,N'P021');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF045',N'Core i7-10750H',N'16GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(600000.00 AS Decimal(18,2)),2,N'P021');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF046',N'Ryzen 7-8945HX',N'8GB',N'512GB SSD',N'Radeon Graphics',CAST(0.00 AS Decimal(18,2)),3,N'P022');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF047',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'Radeon Graphics',CAST(1400000.00 AS Decimal(18,2)),4,N'P022');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF048',N'Ryzen 5-8945HX',N'8GB',N'256GB SSD',N'GPU 10-core',CAST(0.00 AS Decimal(18,2)),9,N'P023');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF049',N'Core i7-10750H',N'16GB',N'512GB SSD',N'GPU 16-core',CAST(1200000.00 AS Decimal(18,2)),2,N'P023');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF050',N'Core i5-10750H',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),4,N'P024');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF051',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(1700000.00 AS Decimal(18,2)),9,N'P024');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF052',N'Ryzen 5-8945HX',N'8GB',N'256GB SSD',N'Intel® UHD',CAST(0.00 AS Decimal(18,2)),5,N'P025');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF053',N'Core i7-10750H',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(1900000.00 AS Decimal(18,2)),3,N'P025');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF054',N'Ultra 5 275 HX',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),3,N'P026');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF055',N'Ultra 7 275 HX',N'16GB',N'1TB SSD',N'Intel® Iris Xe',CAST(1000000.00 AS Decimal(18,2)),2,N'P026');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF056',N'Ultra 5 275 HX',N'8GB',N'512GB SSD',N'Radeon Graphics',CAST(0.00 AS Decimal(18,2)),3,N'P027');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF057',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'Radeon Graphics',CAST(1600000.00 AS Decimal(18,2)),2,N'P027');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF058',N'Ultra 5 275 HX',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),3,N'P028');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF059',N'Core i7-10750H',N'16GB',N'1TB SSD',N'Intel® Iris Xe',CAST(1300000.00 AS Decimal(18,2)),2,N'P028');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF060',N'Ryzen 7-8945HX',N'8GB',N'512GB SSD',N'Radeon Graphics',CAST(0.00 AS Decimal(18,2)),1,N'P029');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF061',N'Ultra 9 275 HX',N'16GB',N'1TB SSD',N'Radeon Graphics',CAST(1000000.00 AS Decimal(18,2)),3,N'P029');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF062',N'Ultra 5 275 HX',N'8GB',N'256GB SSD',N'Intel® UHD',CAST(0.00 AS Decimal(18,2)),3,N'P030');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF063',N'Ultra 7 275 HX',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(1000000.00 AS Decimal(18,2)),3,N'P030');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF064',N'Core i7-10750H',N'8GB',N'256GB SSD',N'GPU 10-core',CAST(0.00 AS Decimal(18,2)),1,N'P031');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF065',N'Ryzen 9-8945HX',N'16GB',N'512GB SSD',N'GPU 16-core',CAST(1700000.00 AS Decimal(18,2)),3,N'P031');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF066',N'Core i5-11800H',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),3,N'P032');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF067',N'Core i7-10750H',N'16GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(1200000.00 AS Decimal(18,2)),2,N'P032');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF068',N'Core i7-10750H',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),3,N'P033');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF069',N'Ultra 9 275 HX',N'16GB',N'1TB SSD',N'Intel® Iris Xe',CAST(600000.00 AS Decimal(18,2)),2,N'P033');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF070',N'Ryzen 7-8945HX',N'8GB',N'512GB SSD',N'Radeon Graphics',CAST(0.00 AS Decimal(18,2)),3,N'P034');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF071',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'Radeon Graphics',CAST(600000.00 AS Decimal(18,2)),2,N'P034');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF072',N'Ryzen 7-8945HX',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(0.00 AS Decimal(18,2)),3,N'P035');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF073',N'Core i7-11800H',N'16GB',N'1TB SSD',N'Intel® Iris Xe',CAST(1700000.00 AS Decimal(18,2)),2,N'P035');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF074',N'Ryzen 7-8945HX',N'8GB',N'512GB SSD',N'Radeon Graphics',CAST(0.00 AS Decimal(18,2)),3,N'P036');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF075',N'Ultra 9 275 HX',N'16GB',N'1TB SSD',N'Radeon Graphics',CAST(700000.00 AS Decimal(18,2)),2,N'P036');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF076',N'Ultra 5 275 HX',N'8GB',N'256GB SSD',N'Intel® UHD',CAST(0.00 AS Decimal(18,2)),4,N'P037');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF077',N'Core i9-10750H',N'8GB',N'512GB SSD',N'Intel® Iris Xe',CAST(1500000.00 AS Decimal(18,2)),3,N'P037');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF078',N'Ryzen 5-8945HX',N'8GB',N'512GB SSD',N'NVIDIA® GTX™ 1650',CAST(0.00 AS Decimal(18,2)),2,N'P038');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF079',N'Ultra 9 275 HX',N'16GB',N'1TB SSD',N'NVIDIA® RTX™ 3050',CAST(2000000.00 AS Decimal(18,2)),3,N'P038');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF080',N'Ryzen 5-8945HX',N'8GB',N'512GB SSD',N'Radeon Graphics',CAST(0.00 AS Decimal(18,2)),3,N'P039');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF081',N'Ryzen 9-8945HX',N'16GB',N'1TB SSD',N'Radeon Graphics',CAST(1900000.00 AS Decimal(18,2)),2,N'P039');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF082',N'Ryzen 5-8945HX',N'8GB',N'256GB SSD',N'GPU 10-core',CAST(0.00 AS Decimal(18,2)),1,N'P040');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF083',N'Core i7-10750H',N'16GB',N'512GB SSD',N'GPU 16-core',CAST(1300000.00 AS Decimal(18,2)),1,N'P040');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF084', N'Core i7-14700HX', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(0.00 AS Decimal(18,2)), 2, N'P041');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF085', N'Core i9-14900HX', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 2, N'P041');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF086', N'Ultra 9 275 HX', N'64GB', N'2TB SSD', N'RTX 4080 12GB', CAST(1800000.00 AS Decimal(18,2)), 1, N'P041');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF087', N'Core i7-14700HX', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(0.00 AS Decimal(18,2)), 2, N'P042');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF088', N'Core i9-14900HX', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 2, N'P042');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF089', N'Ultra 9 275 HX', N'64GB', N'2TB SSD', N'RTX 4080 12GB', CAST(1800000.00 AS Decimal(18,2)), 1, N'P042');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF090', N'Core i7-14700HX', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(0.00 AS Decimal(18,2)), 2, N'P043');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF091', N'Core i9-14900HX', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 2, N'P043');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF092', N'Ultra 9 275 HX', N'64GB', N'2TB SSD', N'RTX 4080 12GB', CAST(1800000.00 AS Decimal(18,2)), 1, N'P043');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF093', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P044');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF094', N'Core i7-1355U', N'16GB', N'1TB SSD', N'Iris Xe', CAST(800000.00 AS Decimal(18,2)), 2, N'P044');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF095', N'Ultra 5 125H', N'16GB', N'1TB SSD', N'MX550', CAST(950000.00 AS Decimal(18,2)), 1, N'P044');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF096', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P045');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF097', N'Core i7-1355U', N'16GB', N'1TB SSD', N'Iris Xe', CAST(800000.00 AS Decimal(18,2)), 2, N'P045');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF098', N'Ultra 5 125H', N'16GB', N'1TB SSD', N'MX550', CAST(950000.00 AS Decimal(18,2)), 1, N'P045');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF099', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P046');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF100', N'Core i7-1355U', N'16GB', N'1TB SSD', N'Iris Xe', CAST(800000.00 AS Decimal(18,2)), 2, N'P046');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF101', N'Ultra 5 125H', N'16GB', N'1TB SSD', N'MX550', CAST(950000.00 AS Decimal(18,2)), 1, N'P046');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF102', N'Core i5-1340P', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P047');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF103', N'Core i7-1360P', N'16GB', N'1TB SSD', N'Iris Xe', CAST(850000.00 AS Decimal(18,2)), 2, N'P047');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF104', N'Ultra 7 165H', N'32GB', N'1TB SSD', N'Iris Xe', CAST(1100000.00 AS Decimal(18,2)), 1, N'P047');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF105', N'Core i5-1340P', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P048');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF106', N'Core i7-1360P', N'16GB', N'1TB SSD', N'Iris Xe', CAST(850000.00 AS Decimal(18,2)), 2, N'P048');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF107', N'Ultra 7 165H', N'32GB', N'1TB SSD', N'Iris Xe', CAST(1100000.00 AS Decimal(18,2)), 1, N'P048');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF108', N'Core i5-1340P', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P049');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF109', N'Core i7-1360P', N'16GB', N'1TB SSD', N'Iris Xe', CAST(850000.00 AS Decimal(18,2)), 2, N'P049');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF110', N'Ultra 7 165H', N'32GB', N'1TB SSD', N'Iris Xe', CAST(1100000.00 AS Decimal(18,2)), 1, N'P049');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF111', N'Ryzen 5 7600H', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(0.00 AS Decimal(18,2)), 2, N'P050');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF112', N'Ryzen 7 7840HS', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 2, N'P050');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF113', N'Core i7-13620H', N'64GB', N'2TB SSD', N'RTX 4080 12GB', CAST(1800000.00 AS Decimal(18,2)), 1, N'P050');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF114', N'Ryzen 5 7600H', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(0.00 AS Decimal(18,2)), 2, N'P051');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF115', N'Ryzen 7 7840HS', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 2, N'P051');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF116', N'Core i7-13620H', N'64GB', N'2TB SSD', N'RTX 4080 12GB', CAST(1800000.00 AS Decimal(18,2)), 1, N'P051');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF117', N'Ryzen 5 7600H', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(0.00 AS Decimal(18,2)), 2, N'P052');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF118', N'Ryzen 7 7840HS', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 2, N'P052');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF119', N'Core i7-13620H', N'64GB', N'2TB SSD', N'RTX 4080 12GB', CAST(1800000.00 AS Decimal(18,2)), 1, N'P052');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF120', N'Ryzen 5 6600H', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P053');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF121', N'Ryzen 7 7840HS', N'16GB', N'1TB SSD', N'RTX 4050 6GB', CAST(1100000.00 AS Decimal(18,2)), 2, N'P053');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF122', N'Core i5-13500H', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1300000.00 AS Decimal(18,2)), 1, N'P053');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF123', N'Ryzen 5 6600H', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P054');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF124', N'Ryzen 7 7840HS', N'16GB', N'1TB SSD', N'RTX 4050 6GB', CAST(1100000.00 AS Decimal(18,2)), 2, N'P054');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF125', N'Core i5-13500H', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1300000.00 AS Decimal(18,2)), 1, N'P054');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF126', N'Ryzen 5 6600H', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P055');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF127', N'Ryzen 7 7840HS', N'16GB', N'1TB SSD', N'RTX 4050 6GB', CAST(1100000.00 AS Decimal(18,2)), 2, N'P055');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF128', N'Core i5-13500H', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1300000.00 AS Decimal(18,2)), 1, N'P055');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF129', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P056');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF130', N'Core i7-1355U', N'16GB', N'512GB SSD', N'Iris Xe', CAST(650000.00 AS Decimal(18,2)), 2, N'P056');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF131', N'Ryzen 5 7600H', N'16GB', N'1TB SSD', N'GTX 1650 4GB', CAST(900000.00 AS Decimal(18,2)), 1, N'P056');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF132', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P057');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF133', N'Core i7-1355U', N'16GB', N'512GB SSD', N'Iris Xe', CAST(650000.00 AS Decimal(18,2)), 2, N'P057');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF134', N'Ryzen 5 7600H', N'16GB', N'1TB SSD', N'GTX 1650 4GB', CAST(900000.00 AS Decimal(18,2)), 1, N'P057');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF135', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P058');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF136', N'Core i7-1355U', N'16GB', N'512GB SSD', N'Iris Xe', CAST(650000.00 AS Decimal(18,2)), 2, N'P058');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF137', N'Ryzen 5 7600H', N'16GB', N'1TB SSD', N'GTX 1650 4GB', CAST(900000.00 AS Decimal(18,2)), 1, N'P058');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF138', N'Core i7-13700HX', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P059');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF139', N'Core i9-13900HX', N'16GB', N'1TB SSD', N'RTX 3060 6GB', CAST(1200000.00 AS Decimal(18,2)), 2, N'P059');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF140', N'Ryzen 9 7945HX', N'32GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1500000.00 AS Decimal(18,2)), 1, N'P059');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF141', N'Core i7-13700HX', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P060');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF142', N'Core i9-13900HX', N'16GB', N'1TB SSD', N'RTX 3060 6GB', CAST(1200000.00 AS Decimal(18,2)), 2, N'P060');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF143', N'Ryzen 9 7945HX', N'32GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1500000.00 AS Decimal(18,2)), 1, N'P060');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF144', N'Core i7-13700HX', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P061');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF145', N'Core i9-13900HX', N'16GB', N'1TB SSD', N'RTX 3060 6GB', CAST(1200000.00 AS Decimal(18,2)), 2, N'P061');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF146', N'Ryzen 9 7945HX', N'32GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1500000.00 AS Decimal(18,2)), 1, N'P061');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF147', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P062');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF148', N'Core i7-1355U', N'16GB', N'512GB SSD', N'Iris Xe', CAST(650000.00 AS Decimal(18,2)), 2, N'P062');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF149', N'Ultra 5 125H', N'16GB', N'1TB SSD', N'Iris Xe', CAST(850000.00 AS Decimal(18,2)), 1, N'P062');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF150', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P063');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF151', N'Core i7-1355U', N'16GB', N'512GB SSD', N'Iris Xe', CAST(650000.00 AS Decimal(18,2)), 2, N'P063');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF152', N'Ultra 5 125H', N'16GB', N'1TB SSD', N'Iris Xe', CAST(850000.00 AS Decimal(18,2)), 1, N'P063');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF153', N'Core i5-1335U', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P064');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF154', N'Core i7-1355U', N'16GB', N'512GB SSD', N'Iris Xe', CAST(650000.00 AS Decimal(18,2)), 2, N'P064');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF155', N'Ultra 5 125H', N'16GB', N'1TB SSD', N'Iris Xe', CAST(850000.00 AS Decimal(18,2)), 1, N'P064');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF156', N'Ryzen 5 7600H', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P065');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF157', N'Ryzen 7 7840HS', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1200000.00 AS Decimal(18,2)), 2, N'P065');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF158', N'Core i7-13620H', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 1, N'P065');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF159', N'Ryzen 5 7600H', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P066');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF160', N'Ryzen 7 7840HS', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1200000.00 AS Decimal(18,2)), 2, N'P066');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF161', N'Core i7-13620H', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 1, N'P066');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF162', N'Ryzen 5 7600H', N'8GB', N'512GB SSD', N'RTX 3050 6GB', CAST(0.00 AS Decimal(18,2)), 3, N'P067');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF163', N'Ryzen 7 7840HS', N'16GB', N'1TB SSD', N'RTX 4060 8GB', CAST(1200000.00 AS Decimal(18,2)), 2, N'P067');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF164', N'Core i7-13620H', N'32GB', N'1TB SSD', N'RTX 4070 8GB', CAST(1500000.00 AS Decimal(18,2)), 1, N'P067');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF165', N'Ultra 5 125H', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P068');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF166', N'Ultra 7 165H', N'16GB', N'1TB SSD', N'Iris Xe', CAST(900000.00 AS Decimal(18,2)), 2, N'P068');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF167', N'Ultra 9 275 HX', N'32GB', N'1TB SSD', N'Iris Xe', CAST(1200000.00 AS Decimal(18,2)), 1, N'P068');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF168', N'Ultra 5 125H', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P069');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF169', N'Ultra 7 165H', N'16GB', N'1TB SSD', N'Iris Xe', CAST(900000.00 AS Decimal(18,2)), 2, N'P069');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF170', N'Ultra 9 275 HX', N'32GB', N'1TB SSD', N'Iris Xe', CAST(1200000.00 AS Decimal(18,2)), 1, N'P069');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF171', N'Ultra 5 125H', N'8GB', N'512GB SSD', N'Iris Xe', CAST(0.00 AS Decimal(18,2)), 3, N'P070');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF172', N'Ultra 7 165H', N'16GB', N'1TB SSD', N'Iris Xe', CAST(900000.00 AS Decimal(18,2)), 2, N'P070');
INSERT INTO ProductConfiguration(configuration_id,cpu,ram,rom,card,price,quantity,product_id) VALUES(N'CF173', N'Ultra 9 275 HX', N'32GB', N'1TB SSD', N'Iris Xe', CAST(1200000.00 AS Decimal(18,2)), 1, N'P070');



-- ========== PROMOTION ==========
INSERT INTO Promotion (promotion_id, product_id, type, content_detail) VALUES (N'KM001', N'P001', N'Giảm giá', N'Giảm giá 10%');
INSERT INTO Promotion (promotion_id, product_id, type, content_detail) VALUES (N'KM002', N'P003', N'Freeship', N'Freeship');


-- ========== CHAT ==========
--INSERT INTO Chat (chat_id, content_detail, time, status, customer_id, employee_id, sender_type)
--VALUES (N'CH001', N'Máy tôi bị lỗi pin nhanh hết.', CAST(N'2024-11-20T22:08:47.077' AS DateTime), N'Đã phản hồi', N'C001', N'E003', N'customer');
--INSERT INTO Chat (chat_id, content_detail, time, status, customer_id, employee_id, sender_type)
--VALUES (N'CH002', N'Tôi muốn đổi màu sản phẩm.', CAST(N'2024-11-20T22:08:47.077' AS DateTime), N'Đang chờ', N'C002', N'E002', N'customer');


-- ========== PRODUCTREVIEW ==========
INSERT INTO ProductReview (productReview_id, content_detail, rate, customer_id, time, product_id)
VALUES (N'PRV001', N'Sản phẩm tốt, chạy êm.', 5, N'C001', CAST(N'2024-11-20T22:08:47.077' AS DateTime), N'P001');
INSERT INTO ProductReview (productReview_id ,content_detail, rate, customer_id, time, product_id)
VALUES (N'PRV002', N'Giá hơi cao nhưng đáng tiền.', 4, N'C003', CAST(N'2024-11-20T22:08:47.077' AS DateTime), N'P005');

-- ========== HISTORY ==========
INSERT INTO History (history_id, activity_type, employee_id, time)
VALUES (N'H001', N'Đăng nhập', N'E002', CAST(N'2024-11-20T22:08:47.077' AS DateTime));
INSERT INTO History (history_id, activity_type, employee_id, time)
VALUES (N'H002', N'Mua hàng', N'E005', CAST(N'2024-11-20T22:08:47.077' AS DateTime));

-- ========== CART ==========
INSERT INTO Cart (cart_id, total_amount, customer_id) VALUES (N'CA001', CAST(22990000.00 AS Decimal(18,2)), N'C003');
INSERT INTO Cart (cart_id, total_amount, customer_id) VALUES (N'CA002', CAST(39990000.00 AS Decimal(18,2)), N'C006');

-- ========== CARTDETAIL ==========
INSERT INTO CartDetail (cartDetail_id, quantity, specifications, cart_id, product_id) VALUES (N'CAD001', 2, N'Core i5-11800H / 8GB / 512GB SSD / NVIDIA® GeForce RTX™ 3050 4GB GDDR6', N'CA001', N'P001');
INSERT INTO CartDetail (cartDetail_id, quantity, specifications, cart_id, product_id) VALUES (N'CAD002', 1, N'Ultra 9 275 HX / 32GB / 1TB SSD / NVIDIA® GeForce RTX™ 4060', N'CA002', N'P005');



INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI001',N'SUP001',N'E001',CAST(N'2025-10-20T22:08:47.077' AS DateTime),CAST(434800000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI002',N'SUP001',N'E001',CAST(N'2025-10-21T04:08:47.077' AS DateTime),CAST(1809000000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI003',N'SUP001',N'E001',CAST(N'2025-10-22T04:08:47.077' AS DateTime),CAST(1304700000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI004',N'SUP001',N'E001',CAST(N'2025-10-23T04:08:47.077' AS DateTime),CAST(620500000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI005',N'SUP001',N'E001',CAST(N'2025-10-24T04:08:47.077' AS DateTime),CAST(479300000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI006',N'SUP001',N'E001',CAST(N'2025-10-25T04:08:47.077' AS DateTime),CAST(401000000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI007',N'SUP001',N'E001',CAST(N'2025-10-26T04:08:47.077' AS DateTime),CAST(595000000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI008',N'SUP001',N'E001',CAST(N'2025-10-27T04:08:47.077' AS DateTime),CAST(483800000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI009',N'SUP001',N'E001',CAST(N'2025-10-28T04:08:47.077' AS DateTime),CAST(447700000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI010',N'SUP001',N'E001',CAST(N'2025-10-29T04:08:47.077' AS DateTime),CAST(628500000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI011',N'SUP001',N'E001',CAST(N'2025-10-30T04:08:47.077' AS DateTime),CAST(285100000.00 AS Decimal(18,2)));
INSERT INTO StockImport (stockImport_id,supplier_id,employee_id,time,total_amount) VALUES (N'SI012',N'SUP001',N'E001',CAST(N'2025-11-01T16:08:47.077' AS DateTime),CAST(6486120000.00 AS Decimal(18,2)));


INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0001',N'SI001',N'P001',N'Core i5-11800H / 8GB / 512GB SSD / NVIDIA GeForce RTX 3050 4GB GDDR6',2,CAST(72000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0002',N'SI001',N'P001',N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA GeForce RTX 3060 6GB GDDR6',3,CAST(72500000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0003',N'SI001',N'P001',N'Core i9-10750H / 32GB / 1TB SSD / NVIDIA GeForce RTX 4070 8GB GDDR6',1,CAST(73300000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0004',N'SI002',N'P002',N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA GeForce RTX 3050 4GB',2,CAST(85000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0005',N'SI002',N'P002',N'Core i7-11800H / 16GB / 1TB SSD / NVIDIA GeForce RTX 4060 8GB',2,CAST(86200000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0006',N'SI002',N'P003',N'Core i7-10750H / 8GB / 512GB SSD / NVIDIA GeForce RTX 4050 6GB',3,CAST(89000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0007',N'SI002',N'P003',N'Core i7-11800H / 16GB / 1TB SSD / NVIDIA GeForce RTX 4060 8GB',2,CAST(89500000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0008',N'SI002',N'P003',N'Core i9-10750H / 32GB / 1TB SSD / NVIDIA GeForce RTX 4070 8GB',1,CAST(90200000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0009',N'SI002',N'P004',N'Ryzen 7-8945HX / 8GB / 512GB SSD / NVIDIA GeForce GTX 1650 4GB',4,CAST(82000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0010',N'SI002',N'P004',N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA GeForce RTX 3050 6GB',2,CAST(83700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0011',N'SI002',N'P005',N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(72000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0012',N'SI002',N'P005',N'Ultra 7 275 HX / 16GB / 1TB SSD / NVIDIA GeForce RTX 3050',2,CAST(72700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0013',N'SI002',N'P005',N'Ultra 9 275 HX / 32GB / 1TB SSD / NVIDIA GeForce RTX 4060',1,CAST(73600000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0014',N'SI003',N'P006',N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA GTX 1650',3,CAST(89000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0015',N'SI003',N'P006',N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA RTX 3050',2,CAST(90500000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0016',N'SI003',N'P007',N'Ryzen 5 H 255 / 8GB / 256GB SSD / GPU 10-core',2,CAST(95000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0017',N'SI003',N'P007',N'Ryzen 7 H 255 / 16GB / 512GB SSD / GPU 16-core',1,CAST(96800000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0018',N'SI003',N'P008',N'Ultra 5 275 HX / 16GB / 512GB SSD / Intel Iris Xe',2,CAST(69000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0019',N'SI003',N'P008',N'Ultra 9 275 HX / 32GB / 1TB SSD / NVIDIA RTX 3050',1,CAST(70900000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0020',N'SI003',N'P009',N'Core i5-10750H / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(72000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0021',N'SI003',N'P009',N'Core i7-10750H / 16GB / 1TB SSD / Intel Iris Xe',2,CAST(72500000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0022',N'SI004',N'P010',N'Ultra 5 275 HX / 8GB / 512GB SSD / NVIDIA GTX 1650',3,CAST(54000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0023',N'SI004',N'P010',N'Ultra 9 275 HX / 16GB / 1TB SSD / NVIDIA RTX 3050',1,CAST(55800000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0024',N'SI004',N'P011',N'Core i7-10750H / 16GB / 1TB SSD / Intel Iris Xe',2,CAST(28300000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0025',N'SI004',N'P012',N'Core i7-10750H / 8GB / 256GB SSD / Intel UHD',4,CAST(29000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0026',N'SI004',N'P012',N'Core i9-10750H / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(30100000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0027',N'SI004',N'P013',N'Core i7-10750H / 16GB / 1TB SSD / Intel Iris Xe',2,CAST(46000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0028',N'SI004',N'P013',N'Core i9-11800H / 32GB / 1TB SSD / NVIDIA RTX 4060',1,CAST(47800000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0029',N'SI005',N'P014',N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA GTX 1650',3,CAST(37000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0030',N'SI005',N'P014',N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA RTX 3050',2,CAST(38700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0031',N'SI005',N'P015',N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics',3,CAST(25000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0032',N'SI005',N'P015',N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics',2,CAST(26600000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0033',N'SI005',N'P016',N'M3 / 8GB / 256GB SSD / GPU 10-core',1,CAST(24600000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0034',N'SI005',N'P016',N'Ryzen 9-8945HX / 16GB / 512GB SSD / GPU 18-core',1,CAST(24300000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0035',N'SI005',N'P017',N'Ultra 7 275 HX / 8GB / 512GB SSD / Intel UHD',3,CAST(22000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0036',N'SI005',N'P017',N'Ultra 9 275 HX / 16GB / 1TB SSD / Intel Iris Xe',2,CAST(23900000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0037',N'SI006',N'P018',N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics',3,CAST(19000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0038',N'SI006',N'P018',N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics',2,CAST(20700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0039',N'SI006',N'P019',N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel UHD',4,CAST(17000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0040',N'SI006',N'P019',N'Ultra 9 275 HX / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(18400000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0041',N'SI006',N'P020',N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA GTX 1650',3,CAST(16000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0042',N'SI006',N'P020',N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA RTX 3050',2,CAST(17100000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0043',N'SI006',N'P021',N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA GTX 1650',4,CAST(16000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0044',N'SI006',N'P021',N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA RTX 3050',2,CAST(16600000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0045',N'SI007',N'P022',N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics',3,CAST(19000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0046',N'SI007',N'P022',N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics',4,CAST(20400000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0047',N'SI007',N'P023',N'Core i7-10750H / 16GB / 512GB SSD / GPU 16-core',2,CAST(16200000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0048',N'SI007',N'P023',N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core',9,CAST(15000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0049',N'SI007',N'P024',N'Core i5-10750H / 8GB / 512GB SSD / Intel Iris Xe',4,CAST(13000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0050',N'SI007',N'P024',N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA RTX 3050',9,CAST(14700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0051',N'SI007',N'P025',N'Core i7-10750H / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(12900000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0052',N'SI007',N'P025',N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel UHD',6,CAST(11000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0053',N'SI008',N'P026',N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(24000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0054',N'SI008',N'P026',N'Ultra 7 275 HX / 16GB / 1TB SSD / Intel Iris Xe',2,CAST(25000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0055',N'SI008',N'P027',N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics',2,CAST(30600000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0056',N'SI008',N'P027',N'Ultra 5 275 HX / 8GB / 512GB SSD / Radeon Graphics',3,CAST(29000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0057',N'SI008',N'P028',N'Core i7-10750H / 16GB / 1TB SSD / Intel Iris Xe',2,CAST(21300000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0058',N'SI008',N'P028',N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(20000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0059',N'SI008',N'P029',N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics',1,CAST(27000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0060',N'SI008',N'P029',N'Ultra 9 275 HX / 16GB / 1TB SSD / Radeon Graphics',3,CAST(28000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0061',N'SI009',N'P030',N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel UHD',3,CAST(25000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0062',N'SI009',N'P030',N'Ultra 7 275 HX / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(26000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0063',N'SI009',N'P031',N'Core i7-10750H / 8GB / 256GB SSD / GPU 10-core',1,CAST(14000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0064',N'SI009',N'P031',N'Ryzen 9-8945HX / 16GB / 512GB SSD / GPU 16-core',3,CAST(15700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0065',N'SI009',N'P032',N'Core i5-11800H / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(24000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0066',N'SI009',N'P032',N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA RTX 3050',2,CAST(25200000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0067',N'SI009',N'P033',N'Core i7-10750H / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(22000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0068',N'SI009',N'P033',N'Ultra 9 275 HX / 16GB / 1TB SSD / Intel Iris Xe',2,CAST(22600000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0069',N'SI010',N'P034',N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics',3,CAST(33000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0070',N'SI010',N'P034',N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics',2,CAST(33600000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0071',N'SI010',N'P035',N'Core i7-11800H / 16GB / 1TB SSD / Intel Iris Xe',2,CAST(16700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0072',N'SI010',N'P035',N'Ryzen 7-8945HX / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(15000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0073',N'SI010',N'P036',N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics',3,CAST(35000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0074',N'SI010',N'P036',N'Ultra 9 275 HX / 16GB / 1TB SSD / Radeon Graphics',2,CAST(35700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0075',N'SI010',N'P037',N'Core i9-10750H / 8GB / 512GB SSD / Intel Iris Xe',3,CAST(30500000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0076',N'SI010',N'P037',N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel UHD',4,CAST(29000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0077',N'SI011',N'P038',N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA GTX 1650',2,CAST(17000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0078',N'SI011',N'P038',N'Ultra 9 275 HX / 16GB / 1TB SSD / NVIDIA RTX 3050',3,CAST(19000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0079',N'SI011',N'P039',N'Ryzen 5-8945HX / 8GB / 512GB SSD / Radeon Graphics',3,CAST(33000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0080',N'SI011',N'P039',N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics',2,CAST(34900000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0081',N'SI011',N'P040',N'Core i7-10750H / 16GB / 512GB SSD / GPU 16-core',1,CAST(13300000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0082',N'SI011',N'P040',N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core',1,CAST(12000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0083',N'SI012',N'P041',N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(34920000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0084',N'SI012',N'P041',N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB',2,CAST(36420000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0085',N'SI012',N'P041',N'Ultra 9 275 HX / 64GB / 2TB SSD / RTX 4080 12GB',1,CAST(36720000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0086',N'SI012',N'P042',N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(35040000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0087',N'SI012',N'P042',N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB',2,CAST(36540000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0088',N'SI012',N'P042',N'Ultra 9 275 HX / 64GB / 2TB SSD / RTX 4080 12GB',1,CAST(36840000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0089',N'SI012',N'P043',N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(35160000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0090',N'SI012',N'P043',N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB',2,CAST(36660000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0091',N'SI012',N'P043',N'Ultra 9 275 HX / 64GB / 2TB SSD / RTX 4080 12GB',1,CAST(36960000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0092',N'SI012',N'P044',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(35280000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0093',N'SI012',N'P044',N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe',2,CAST(36080000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0094',N'SI012',N'P044',N'Ultra 5 125H / 16GB / 1TB SSD / MX550',1,CAST(36230000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0095',N'SI012',N'P045',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(35400000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0096',N'SI012',N'P045',N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe',2,CAST(36200000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0097',N'SI012',N'P045',N'Ultra 5 125H / 16GB / 1TB SSD / MX550',1,CAST(36350000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0098',N'SI012',N'P046',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(35520000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0099',N'SI012',N'P046',N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe',2,CAST(36320000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0100',N'SI012',N'P046',N'Ultra 5 125H / 16GB / 1TB SSD / MX550',1,CAST(36470000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0101',N'SI012',N'P047',N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe',3,CAST(35640000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0102',N'SI012',N'P047',N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe',2,CAST(36490000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0103',N'SI012',N'P047',N'Ultra 7 165H / 32GB / 1TB SSD / Iris Xe',1,CAST(36740000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0104',N'SI012',N'P048',N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe',3,CAST(35760000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0105',N'SI012',N'P048',N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe',2,CAST(36610000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0106',N'SI012',N'P048',N'Ultra 7 165H / 32GB / 1TB SSD / Iris Xe',1,CAST(36860000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0107',N'SI012',N'P049',N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe',3,CAST(35880000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0108',N'SI012',N'P049',N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe',2,CAST(36730000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0109',N'SI012',N'P049',N'Ultra 7 165H / 32GB / 1TB SSD / Iris Xe',1,CAST(36980000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0110',N'SI012',N'P050',N'Core i7-13620H / 64GB / 2TB SSD / RTX 4080 12GB',1,CAST(37800000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0111',N'SI012',N'P050',N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(36000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0112',N'SI012',N'P050',N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB',2,CAST(37500000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0113',N'SI012',N'P051',N'Core i7-13620H / 64GB / 2TB SSD / RTX 4080 12GB',1,CAST(37920000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0114',N'SI012',N'P051',N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(36120000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0115',N'SI012',N'P051',N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB',2,CAST(37620000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0116',N'SI012',N'P052',N'Core i7-13620H / 64GB / 2TB SSD / RTX 4080 12GB',1,CAST(38040000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0117',N'SI012',N'P052',N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(36240000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0118',N'SI012',N'P052',N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB',2,CAST(37740000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0119',N'SI012',N'P053',N'Core i5-13500H / 16GB / 1TB SSD / RTX 4060 8GB',1,CAST(37660000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0120',N'SI012',N'P053',N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(36360000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0121',N'SI012',N'P053',N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB',2,CAST(37460000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0122',N'SI012',N'P054',N'Core i5-13500H / 16GB / 1TB SSD / RTX 4060 8GB',1,CAST(37780000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0123',N'SI012',N'P054',N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(36480000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0124',N'SI012',N'P054',N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB',2,CAST(37580000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0125',N'SI012',N'P055',N'Core i5-13500H / 16GB / 1TB SSD / RTX 4060 8GB',1,CAST(37900000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0126',N'SI012',N'P055',N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(36600000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0127',N'SI012',N'P055',N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB',2,CAST(37700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0128',N'SI012',N'P056',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(36720000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0129',N'SI012',N'P056',N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe',2,CAST(37370000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0130',N'SI012',N'P056',N'Ryzen 5 7600H / 16GB / 1TB SSD / GTX 1650 4GB',1,CAST(37620000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0131',N'SI012',N'P057',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(36840000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0132',N'SI012',N'P057',N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe',2,CAST(37490000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0133',N'SI012',N'P057',N'Ryzen 5 7600H / 16GB / 1TB SSD / GTX 1650 4GB',1,CAST(37740000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0134',N'SI012',N'P058',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(36960000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0135',N'SI012',N'P058',N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe',2,CAST(37610000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0136',N'SI012',N'P058',N'Ryzen 5 7600H / 16GB / 1TB SSD / GTX 1650 4GB',1,CAST(37860000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0137',N'SI012',N'P059',N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(37080000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0138',N'SI012',N'P059',N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB',2,CAST(38280000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0139',N'SI012',N'P059',N'Ryzen 9 7945HX / 32GB / 1TB SSD / RTX 4060 8GB',1,CAST(38580000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0140',N'SI012',N'P060',N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(37200000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0141',N'SI012',N'P060',N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB',2,CAST(38400000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0142',N'SI012',N'P060',N'Ryzen 9 7945HX / 32GB / 1TB SSD / RTX 4060 8GB',1,CAST(38700000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0143',N'SI012',N'P061',N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(37320000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0144',N'SI012',N'P061',N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB',2,CAST(38520000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0145',N'SI012',N'P061',N'Ryzen 9 7945HX / 32GB / 1TB SSD / RTX 4060 8GB',1,CAST(38820000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0146',N'SI012',N'P062',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(37440000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0147',N'SI012',N'P062',N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe',2,CAST(38090000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0148',N'SI012',N'P062',N'Ultra 5 125H / 16GB / 1TB SSD / Iris Xe',1,CAST(38290000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0149',N'SI012',N'P063',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(37560000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0150',N'SI012',N'P063',N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe',2,CAST(38210000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0151',N'SI012',N'P063',N'Ultra 5 125H / 16GB / 1TB SSD / Iris Xe',1,CAST(38410000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0152',N'SI012',N'P064',N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe',3,CAST(37680000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0153',N'SI012',N'P064',N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe',2,CAST(38330000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0154',N'SI012',N'P064',N'Ultra 5 125H / 16GB / 1TB SSD / Iris Xe',1,CAST(38530000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0155',N'SI012',N'P065',N'Core i7-13620H / 32GB / 1TB SSD / RTX 4070 8GB',1,CAST(39300000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0156',N'SI012',N'P065',N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(37800000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0157',N'SI012',N'P065',N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(39000000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0158',N'SI012',N'P066',N'Core i7-13620H / 32GB / 1TB SSD / RTX 4070 8GB',1,CAST(39420000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0159',N'SI012',N'P066',N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(37920000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0160',N'SI012',N'P066',N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(39120000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0161',N'SI012',N'P067',N'Core i7-13620H / 32GB / 1TB SSD / RTX 4070 8GB',1,CAST(39540000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0162',N'SI012',N'P067',N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB',3,CAST(38040000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0163',N'SI012',N'P067',N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB',2,CAST(39240000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0164',N'SI012',N'P068',N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe',3,CAST(38160000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0165',N'SI012',N'P068',N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe',2,CAST(39060000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0166',N'SI012',N'P068',N'Ultra 9 275 HX / 32GB / 1TB SSD / Iris Xe',1,CAST(39360000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0167',N'SI012',N'P069',N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe',3,CAST(38280000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0168',N'SI012',N'P069',N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe',2,CAST(39180000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0169',N'SI012',N'P069',N'Ultra 9 275 HX / 32GB / 1TB SSD / Iris Xe',1,CAST(39480000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0170',N'SI012',N'P070',N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe',3,CAST(38400000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0171',N'SI012',N'P070',N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe',2,CAST(39300000.00 AS Decimal(18,2)));
INSERT INTO StockImportDetail (stockImportDetail_id,stockImport_id,product_id,specifications,quantity,price) VALUES (N'STID0172',N'SI012',N'P070',N'Ultra 9 275 HX / 32GB / 1TB SSD / Iris Xe',1,CAST(39600000.00 AS Decimal(18,2)));



-- ========== SALEINVOICE ==========
INSERT INTO SaleInvoice (saleInvoice_id, payment_method, total_amount, time_create, status, phone, delivery_fee, discount, delivery_address, employee_id, customer_id)
VALUES (N'SI001', N'Thanh toán khi nhận hàng', CAST(80780000.00 AS Decimal(18,2)), CAST(N'2025-10-30T22:08:47.077' AS DateTime), N'Hoàn thành', N'01223308907', CAST(50000.00 AS Decimal(18,2)), CAST(0.00 AS Decimal(18,2)), N'12 Nguyễn Trãi, Phường Thanh Xuân, Thành phố Hà Nội',N'E002', N'C001');
INSERT INTO SaleInvoice (saleInvoice_id, payment_method, total_amount, time_create, status, phone, delivery_fee, discount, delivery_address, employee_id, customer_id)
VALUES (N'SI002', N'Chuyển khoản ngân hàng', CAST(73300000.00 AS Decimal(18,2)), CAST(N'2025-10-31T22:08:47.077' AS DateTime), N'Hoàn thành', N'0352274933', CAST(0.00 AS Decimal(18,2)), CAST(0.00 AS Decimal(18,2)), N'79 Cầu Giấy, Phường Cầu Giấy, Thành phố Hà Nội', N'E003', N'C002');

-- ========== SALEINVOICEDETAIL ==========
INSERT INTO SaleInvoiceDetail (saleInvoiceDetail_id, saleInvoice_id, quantity, unit_price, product_id, specifications) 
VALUES (N'SID001', N'SI001', 1, CAST(69490000.00 AS Decimal(18,2)), N'P001', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3060 6GB GDDR6');
INSERT INTO SaleInvoiceDetail (saleInvoiceDetail_id, saleInvoice_id, quantity, unit_price, product_id, specifications) 
VALUES (N'SID002', N'SI001', 1, CAST(11290000.00 AS Decimal(18,2)), N'P025', N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel® UHD');
INSERT INTO SaleInvoiceDetail (saleInvoiceDetail_id, saleInvoice_id, quantity, unit_price, product_id, specifications) 
VALUES (N'SID003', N'SI002', 1, CAST(73300000.00 AS Decimal(18,2)), N'P009', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe');


-- ========== STOCKEXPORT ==========
INSERT INTO StockExport (stockExport_id, employee_id, saleInvoice_id, status, time)
VALUES (N'SE001', N'E005', N'SI001', N'Hoàn thành', CAST(N'2025-10-30T22:08:47.077' AS DateTime));
INSERT INTO StockExport (stockExport_id, employee_id, saleInvoice_id, status, time)
VALUES (N'SE002', N'E006', N'SI002', N'Hoàn thành', CAST(N'2025-10-31T22:08:47.077' AS DateTime));

-- ========== STOCKEXPORTDETAIL ==========
INSERT INTO StockExportDetail (stockExportDetail_id, stockExport_id, product_id, specifications, quantity) VALUES (N'SED001', N'SE001', N'P001', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3060 6GB GDDR6', 1);
INSERT INTO StockExportDetail (stockExportDetail_id, stockExport_id, product_id, specifications, quantity) VALUES (N'SED002', N'SE001', N'P025', N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel® UHD', 1);
INSERT INTO StockExportDetail (stockExportDetail_id, stockExport_id, product_id, specifications, quantity) VALUES (N'SED003', N'SE002', N'P009', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe', 1);




INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP001CF001001', N'P001', N'Core i5-11800H / 8GB / 512GB SSD / NVIDIA® GeForce RTX™ 3050 4GB GDDR6', NULL, N'in stock', CAST(N'2025-10-20T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP001CF001002', N'P001', N'Core i5-11800H / 8GB / 512GB SSD / NVIDIA® GeForce RTX™ 3050 4GB GDDR6', NULL, N'in stock', CAST(N'2025-10-20T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP001CF002001', N'P001', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3060 6GB GDDR6', N'SED001', N'sold', CAST(N'2025-10-20T22:08:47.077' AS DateTime), CAST(N'2025-10-30T22:08:47.077' AS DateTime), CAST(N'2025-10-30T22:08:47.077' AS DateTime), CAST(N'2028-10-30T22:08:47.077' AS DateTime), NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP001CF002002', N'P001', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3060 6GB GDDR6', NULL, N'in stock', CAST(N'2025-10-20T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP001CF002003', N'P001', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3060 6GB GDDR6',  NULL, N'in stock', CAST(N'2025-10-20T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP001CF003001', N'P001', N'Core i9-10750H / 32GB / 1TB SSD / NVIDIA® GeForce RTX™ 4070 8GB GDDR6', NULL, N'in stock', CAST(N'2025-10-20T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP002CF004001', N'P002', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GeForce RTX™ 3050 4GB', NULL, N'in stock', CAST(N'2025-10-21T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP002CF004002', N'P002', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GeForce RTX™ 3050 4GB', NULL, N'in stock', CAST(N'2025-10-21T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP002CF005001', N'P002', N'Core i7-11800H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 4060 8GB', NULL, N'in stock', CAST(N'2025-10-21T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP002CF005002', N'P002', N'Core i7-11800H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 4060 8GB', NULL, N'in stock', CAST(N'2025-10-21T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP003CF006001', N'P003', N'Core i7-10750H / 8GB / 512GB SSD / NVIDIA® GeForce RTX™ 4050 6GB', NULL, N'in stock', CAST(N'2025-10-21T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP003CF006002', N'P003', N'Core i7-10750H / 8GB / 512GB SSD / NVIDIA® GeForce RTX™ 4050 6GB', NULL, N'in stock', CAST(N'2025-10-21T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP003CF006003', N'P003', N'Core i7-10750H / 8GB / 512GB SSD / NVIDIA® GeForce RTX™ 4050 6GB', NULL, N'in stock', CAST(N'2025-10-21T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP003CF007001', N'P003', N'Core i7-11800H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 4060 8GB', NULL, N'in stock', CAST(N'2025-10-21T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP003CF007002', N'P003', N'Core i7-11800H / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 4060 8GB', NULL, N'in stock', CAST(N'2025-10-21T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP003CF008001', N'P003', N'Core i9-10750H / 32GB / 1TB SSD / NVIDIA® GeForce RTX™ 4070 8GB', NULL, N'in stock', CAST(N'2025-10-21T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP004CF009001', N'P004', N'Ryzen 7-8945HX / 8GB / 512GB SSD / NVIDIA® GeForce GTX™ 1650 4GB', NULL, N'in stock', CAST(N'2025-10-21T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP004CF009002', N'P004', N'Ryzen 7-8945HX / 8GB / 512GB SSD / NVIDIA® GeForce GTX™ 1650 4GB', NULL, N'in stock', CAST(N'2025-10-21T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP004CF009003', N'P004', N'Ryzen 7-8945HX / 8GB / 512GB SSD / NVIDIA® GeForce GTX™ 1650 4GB', NULL, N'in stock', CAST(N'2025-10-21T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP004CF009004', N'P004', N'Ryzen 7-8945HX / 8GB / 512GB SSD / NVIDIA® GeForce GTX™ 1650 4GB', NULL, N'in stock', CAST(N'2025-10-21T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP004CF010001', N'P004', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3050 6GB', NULL, N'in stock', CAST(N'2025-10-21T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP004CF010002', N'P004', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3050 6GB', NULL, N'in stock', CAST(N'2025-10-21T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP005CF011001', N'P005', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-21T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP005CF011002', N'P005', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-21T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP005CF011003', N'P005', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-21T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP005CF012001', N'P005', N'Ultra 7 275 HX / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-21T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP005CF012002', N'P005', N'Ultra 7 275 HX / 16GB / 1TB SSD / NVIDIA® GeForce RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-21T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP005CF013001', N'P005', N'Ultra 9 275 HX / 32GB / 1TB SSD / NVIDIA® GeForce RTX™ 4060', NULL, N'in stock', CAST(N'2025-10-21T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP006CF014001', N'P006', N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-22T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP006CF014002', N'P006', N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-22T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP006CF014003', N'P006', N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-22T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP006CF015001', N'P006', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-22T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP006CF015002', N'P006', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-22T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP007CF016001', N'P007', N'Ryzen 5 H 255 / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-22T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP007CF016002', N'P007', N'Ryzen 5 H 255 / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-22T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP007CF017001', N'P007', N'Ryzen 7 H 255 / 16GB / 512GB SSD / GPU 16-core', NULL, N'in stock', CAST(N'2025-10-22T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP008CF018001', N'P008', N'Ultra 5 275 HX / 16GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-22T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP008CF018002', N'P008', N'Ultra 5 275 HX / 16GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-22T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP008CF019001', N'P008', N'Ultra 9 275 HX / 32GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-22T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP009CF020001', N'P009', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe', N'SED003', N'sold', CAST(N'2025-10-22T22:08:47.077' AS DateTime), CAST(N'2025-10-31T22:08:47.077' AS DateTime), CAST(N'2025-10-31T22:08:47.077' AS DateTime), CAST(N'2028-10-31T22:08:47.077' AS DateTime), NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP009CF020002', N'P009', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-22T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP009CF020003', N'P009', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe',  NULL, N'in stock', CAST(N'2025-10-22T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP009CF021001', N'P009', N'Core i7-10750H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-22T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP009CF021002', N'P009', N'Core i7-10750H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-22T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP010CF022001', N'P010', N'Ultra 5 275 HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-23T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP010CF022002', N'P010', N'Ultra 5 275 HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-23T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP010CF022003', N'P010', N'Ultra 5 275 HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-23T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP010CF023001', N'P010', N'Ultra 9 275 HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-23T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP011CF025001', N'P011', N'Core i7-10750H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-23T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP011CF025002', N'P011', N'Core i7-10750H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-23T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP012CF026001', N'P012', N'Core i7-10750H / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-23T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP012CF026002', N'P012', N'Core i7-10750H / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-23T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP012CF026003', N'P012', N'Core i7-10750H / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-23T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP012CF026004', N'P012', N'Core i7-10750H / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-23T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP012CF027001', N'P012', N'Core i9-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-23T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP012CF027002', N'P012', N'Core i9-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-23T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP012CF027003', N'P012', N'Core i9-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-23T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP013CF028001', N'P013', N'Core i7-10750H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-23T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP013CF028002', N'P013', N'Core i7-10750H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-23T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP013CF029001', N'P013', N'Core i9-11800H / 32GB / 1TB SSD / NVIDIA® RTX™ 4060', NULL, N'in stock', CAST(N'2025-10-23T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP014CF030001', N'P014', N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-24T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP014CF030002', N'P014', N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-24T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP014CF030003', N'P014', N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-24T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP014CF031001', N'P014', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-24T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP014CF031002', N'P014', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-24T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP015CF032001', N'P015', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-24T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP015CF032002', N'P015', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-24T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP015CF032003', N'P015', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-24T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP015CF033001', N'P015', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-24T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP015CF033002', N'P015', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-24T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP016CF034001', N'P016', N'M3 / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-24T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP016CF035001', N'P016', N'Ryzen 9-8945HX / 16GB / 512GB SSD / GPU 18-core', NULL, N'in stock', CAST(N'2025-10-24T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP017CF036001', N'P017', N'Ultra 7 275 HX / 8GB / 512GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-24T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP017CF036002', N'P017', N'Ultra 7 275 HX / 8GB / 512GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-24T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP017CF036003', N'P017', N'Ultra 7 275 HX / 8GB / 512GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-24T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP017CF037001', N'P017', N'Ultra 9 275 HX / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-24T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP017CF037002', N'P017', N'Ultra 9 275 HX / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-24T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP018CF038001', N'P018', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-25T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP018CF038002', N'P018', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-25T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP018CF038003', N'P018', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-25T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP018CF039001', N'P018', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-25T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP018CF039002', N'P018', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-25T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP019CF040001', N'P019', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-25T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP019CF040002', N'P019', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-25T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP019CF040003', N'P019', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-25T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP019CF040004', N'P019', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-25T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP019CF041001', N'P019', N'Ultra 9 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-25T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP019CF041002', N'P019', N'Ultra 9 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-25T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP019CF041003', N'P019', N'Ultra 9 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-25T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP020CF042001', N'P020', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-25T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP020CF042002', N'P020', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-25T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP020CF042003', N'P020', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-25T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP020CF043001', N'P020', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-25T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP020CF043002', N'P020', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-25T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP021CF044001', N'P021', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-25T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP021CF044002', N'P021', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-25T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP021CF044003', N'P021', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-25T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP021CF044004', N'P021', N'Core i5-10750H / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-25T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP021CF045001', N'P021', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-25T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP021CF045002', N'P021', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-25T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP022CF046001', N'P022', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-26T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP022CF046002', N'P022', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-26T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP022CF046003', N'P022', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-26T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP022CF047001', N'P022', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-26T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP022CF047002', N'P022', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-26T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP022CF047003', N'P022', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-26T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP022CF047004', N'P022', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-26T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048001', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048002', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048003', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048004', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048005', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048006', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048007', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048008', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF048009', N'P023', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF049001', N'P023', N'Core i7-10750H / 16GB / 512GB SSD / GPU 16-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP023CF049002', N'P023', N'Core i7-10750H / 16GB / 512GB SSD / GPU 16-core', NULL, N'in stock', CAST(N'2025-10-26T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF050001', N'P024', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF050002', N'P024', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF050003', N'P024', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF050004', N'P024', N'Core i5-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051001', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051002', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051003', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051004', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051005', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051006', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051007', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051008', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP024CF051009', N'P024', N'Ryzen 9-8945HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-26T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF052001', N'P025', N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel® UHD', N'SED002', N'sold', CAST(N'2025-10-26T22:08:47.077' AS DateTime), CAST(N'2025-10-30T22:08:47.077' AS DateTime), CAST(N'2025-10-30T22:08:47.077' AS DateTime), CAST(N'2026-10-30T22:08:47.077' AS DateTime), NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF052002', N'P025', N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-26T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF052003', N'P025', N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-26T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF052004', N'P025', N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-26T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF052005', N'P025', N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-26T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF052006', N'P025', N'Ryzen 5-8945HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-26T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF053001', N'P025', N'Core i7-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-26T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF053002', N'P025', N'Core i7-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-26T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP025CF053003', N'P025', N'Core i7-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-26T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP026CF054001', N'P026', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP026CF054002', N'P026', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP026CF054003', N'P026', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP026CF055001', N'P026', N'Ultra 7 275 HX / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP026CF055002', N'P026', N'Ultra 7 275 HX / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP027CF056001', N'P027', N'Ultra 5 275 HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP027CF056002', N'P027', N'Ultra 5 275 HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP027CF056003', N'P027', N'Ultra 5 275 HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP027CF057001', N'P027', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP027CF057002', N'P027', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP028CF058001', N'P028', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP028CF058002', N'P028', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP028CF058003', N'P028', N'Ultra 5 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP028CF059001', N'P028', N'Core i7-10750H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP028CF059002', N'P028', N'Core i7-10750H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-27T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP029CF060001', N'P029', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP029CF061001', N'P029', N'Ultra 9 275 HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP029CF061002', N'P029', N'Ultra 9 275 HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP029CF061003', N'P029', N'Ultra 9 275 HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-27T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP030CF062001', N'P030', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-28T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP030CF062002', N'P030', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-28T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP030CF062003', N'P030', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-28T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP030CF063001', N'P030', N'Ultra 7 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP030CF063002', N'P030', N'Ultra 7 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP030CF063003', N'P030', N'Ultra 7 275 HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP031CF064001', N'P031', N'Core i7-10750H / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-28T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP031CF065001', N'P031', N'Ryzen 9-8945HX / 16GB / 512GB SSD / GPU 16-core', NULL, N'in stock', CAST(N'2025-10-28T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP031CF065002', N'P031', N'Ryzen 9-8945HX / 16GB / 512GB SSD / GPU 16-core', NULL, N'in stock', CAST(N'2025-10-28T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP031CF065003', N'P031', N'Ryzen 9-8945HX / 16GB / 512GB SSD / GPU 16-core', NULL, N'in stock', CAST(N'2025-10-28T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP032CF066001', N'P032', N'Core i5-11800H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP032CF066002', N'P032', N'Core i5-11800H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP032CF066003', N'P032', N'Core i5-11800H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP032CF067001', N'P032', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-28T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP032CF067002', N'P032', N'Core i7-10750H / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-28T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP033CF068001', N'P033', N'Core i7-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP033CF068002', N'P033', N'Core i7-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP033CF068003', N'P033', N'Core i7-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP033CF069001', N'P033', N'Ultra 9 275 HX / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP033CF069002', N'P033', N'Ultra 9 275 HX / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-28T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP034CF070001', N'P034', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP034CF070002', N'P034', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP034CF070003', N'P034', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP034CF071001', N'P034', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP034CF071002', N'P034', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP035CF072001', N'P035', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-29T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP035CF072002', N'P035', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-29T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP035CF072003', N'P035', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-29T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP035CF073001', N'P035', N'Core i7-11800H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-29T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP035CF073002', N'P035', N'Core i7-11800H / 16GB / 1TB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-29T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP036CF074001', N'P036', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP036CF074002', N'P036', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP036CF074003', N'P036', N'Ryzen 7-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP036CF075001', N'P036', N'Ultra 9 275 HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP036CF075002', N'P036', N'Ultra 9 275 HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-29T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP037CF076001', N'P037', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-29T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP037CF076002', N'P037', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-29T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP037CF076003', N'P037', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-29T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP037CF076004', N'P037', N'Ultra 5 275 HX / 8GB / 256GB SSD / Intel® UHD', NULL, N'in stock', CAST(N'2025-10-29T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP037CF077001', N'P037', N'Core i9-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-29T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP037CF077002', N'P037', N'Core i9-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-29T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP037CF077003', N'P037', N'Core i9-10750H / 8GB / 512GB SSD / Intel® Iris Xe', NULL, N'in stock', CAST(N'2025-10-29T22:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP038CF078001', N'P038', N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-30T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP038CF078002', N'P038', N'Ryzen 5-8945HX / 8GB / 512GB SSD / NVIDIA® GTX™ 1650', NULL, N'in stock', CAST(N'2025-10-30T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP038CF079001', N'P038', N'Ultra 9 275 HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-30T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP038CF079002', N'P038', N'Ultra 9 275 HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-30T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP038CF079003', N'P038', N'Ultra 9 275 HX / 16GB / 1TB SSD / NVIDIA® RTX™ 3050', NULL, N'in stock', CAST(N'2025-10-30T04:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP039CF080001', N'P039', N'Ryzen 5-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-30T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP039CF080002', N'P039', N'Ryzen 5-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-30T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP039CF080003', N'P039', N'Ryzen 5-8945HX / 8GB / 512GB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-30T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP039CF081001', N'P039', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-30T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP039CF081002', N'P039', N'Ryzen 9-8945HX / 16GB / 1TB SSD / Radeon Graphics', NULL, N'in stock', CAST(N'2025-10-30T10:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP040CF082001', N'P040', N'Ryzen 5-8945HX / 8GB / 256GB SSD / GPU 10-core', NULL, N'in stock', CAST(N'2025-10-30T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP040CF083001', N'P040', N'Core i7-10750H / 16GB / 512GB SSD / GPU 16-core', NULL, N'in stock', CAST(N'2025-10-30T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP041CF084001', N'P041', N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP041CF084002', N'P041', N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP041CF085001', N'P041', N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP041CF085002', N'P041', N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP041CF086001', N'P041', N'Ultra 9 275 HX / 64GB / 2TB SSD / RTX 4080 12GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP042CF087001', N'P042', N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP042CF087002', N'P042', N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP042CF088001', N'P042', N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP042CF088002', N'P042', N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP042CF089001', N'P042', N'Ultra 9 275 HX / 64GB / 2TB SSD / RTX 4080 12GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP043CF090001', N'P043', N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP043CF090002', N'P043', N'Core i7-14700HX / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP043CF091001', N'P043', N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP043CF091002', N'P043', N'Core i9-14900HX / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP043CF092001', N'P043', N'Ultra 9 275 HX / 64GB / 2TB SSD / RTX 4080 12GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP044CF093001', N'P044', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP044CF093002', N'P044', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP044CF093003', N'P044', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP044CF094001', N'P044', N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP044CF094002', N'P044', N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP044CF095001', N'P044', N'Ultra 5 125H / 16GB / 1TB SSD / MX550', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP045CF096001', N'P045', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP045CF096002', N'P045', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP045CF096003', N'P045', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP045CF097001', N'P045', N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP045CF097002', N'P045', N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP045CF098001', N'P045', N'Ultra 5 125H / 16GB / 1TB SSD / MX550', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP046CF099001', N'P046', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP046CF099002', N'P046', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP046CF099003', N'P046', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP046CF100001', N'P046', N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP046CF100002', N'P046', N'Core i7-1355U / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP046CF101001', N'P046', N'Ultra 5 125H / 16GB / 1TB SSD / MX550', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP047CF102001', N'P047', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP047CF102002', N'P047', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP047CF102003', N'P047', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP047CF103001', N'P047', N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP047CF103002', N'P047', N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP047CF104001', N'P047', N'Ultra 7 165H / 32GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP048CF105001', N'P048', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP048CF105002', N'P048', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP048CF105003', N'P048', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP048CF106001', N'P048', N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP048CF106002', N'P048', N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP048CF107001', N'P048', N'Ultra 7 165H / 32GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP049CF108001', N'P049', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP049CF108002', N'P049', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP049CF108003', N'P049', N'Core i5-1340P / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP049CF109001', N'P049', N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP049CF109002', N'P049', N'Core i7-1360P / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP049CF110001', N'P049', N'Ultra 7 165H / 32GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP050CF111001', N'P050', N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP050CF111002', N'P050', N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP050CF112001', N'P050', N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP050CF112002', N'P050', N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP050CF113001', N'P050', N'Core i7-13620H / 64GB / 2TB SSD / RTX 4080 12GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP051CF114001', N'P051', N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP051CF114002', N'P051', N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP051CF115001', N'P051', N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP051CF115002', N'P051', N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP051CF116001', N'P051', N'Core i7-13620H / 64GB / 2TB SSD / RTX 4080 12GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP052CF117001', N'P052', N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP052CF117002', N'P052', N'Ryzen 5 7600H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP052CF118001', N'P052', N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP052CF118002', N'P052', N'Ryzen 7 7840HS / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP052CF119001', N'P052', N'Core i7-13620H / 64GB / 2TB SSD / RTX 4080 12GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP053CF120001', N'P053', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP053CF120002', N'P053', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP053CF120003', N'P053', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP053CF121001', N'P053', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP053CF121002', N'P053', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP053CF122001', N'P053', N'Core i5-13500H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP054CF123001', N'P054', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP054CF123002', N'P054', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP054CF123003', N'P054', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP054CF124001', N'P054', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP054CF124002', N'P054', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP054CF125001', N'P054', N'Core i5-13500H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP055CF126001', N'P055', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP055CF126002', N'P055', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP055CF126003', N'P055', N'Ryzen 5 6600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP055CF127001', N'P055', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP055CF127002', N'P055', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP055CF128001', N'P055', N'Core i5-13500H / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP056CF129001', N'P056', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP056CF129002', N'P056', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP056CF129003', N'P056', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP056CF130001', N'P056', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP056CF130002', N'P056', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP056CF131001', N'P056', N'Ryzen 5 7600H / 16GB / 1TB SSD / GTX 1650 4GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP057CF132001', N'P057', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP057CF132002', N'P057', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP057CF132003', N'P057', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP057CF133001', N'P057', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP057CF133002', N'P057', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP057CF134001', N'P057', N'Ryzen 5 7600H / 16GB / 1TB SSD / GTX 1650 4GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP058CF135001', N'P058', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP058CF135002', N'P058', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP058CF135003', N'P058', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP058CF136001', N'P058', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP058CF136002', N'P058', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP058CF137001', N'P058', N'Ryzen 5 7600H / 16GB / 1TB SSD / GTX 1650 4GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP059CF138001', N'P059', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP059CF138002', N'P059', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP059CF138003', N'P059', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP059CF139001', N'P059', N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP059CF139002', N'P059', N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP059CF140001', N'P059', N'Ryzen 9 7945HX / 32GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP060CF141001', N'P060', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP060CF141002', N'P060', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP060CF141003', N'P060', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP060CF142001', N'P060', N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP060CF142002', N'P060', N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP060CF143001', N'P060', N'Ryzen 9 7945HX / 32GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP061CF144001', N'P061', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP061CF144002', N'P061', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP061CF144003', N'P061', N'Core i7-13700HX / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP061CF145001', N'P061', N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP061CF145002', N'P061', N'Core i9-13900HX / 16GB / 1TB SSD / RTX 3060 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP061CF146001', N'P061', N'Ryzen 9 7945HX / 32GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP062CF147001', N'P062', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP062CF147002', N'P062', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP062CF147003', N'P062', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP062CF148001', N'P062', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP062CF148002', N'P062', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP062CF149001', N'P062', N'Ultra 5 125H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP063CF150001', N'P063', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP063CF150002', N'P063', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP063CF150003', N'P063', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP063CF151001', N'P063', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP063CF151002', N'P063', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP063CF152001', N'P063', N'Ultra 5 125H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP064CF153001', N'P064', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP064CF153002', N'P064', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP064CF153003', N'P064', N'Core i5-1335U / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP064CF154001', N'P064', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP064CF154002', N'P064', N'Core i7-1355U / 16GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP064CF155001', N'P064', N'Ultra 5 125H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP065CF156001', N'P065', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP065CF156002', N'P065', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP065CF156003', N'P065', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP065CF157001', N'P065', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP065CF157002', N'P065', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP065CF158001', N'P065', N'Core i7-13620H / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP066CF159001', N'P066', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP066CF159002', N'P066', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP066CF159003', N'P066', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP066CF160001', N'P066', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP066CF160002', N'P066', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP066CF161001', N'P066', N'Core i7-13620H / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP067CF162001', N'P067', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP067CF162002', N'P067', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP067CF162003', N'P067', N'Ryzen 5 7600H / 8GB / 512GB SSD / RTX 3050 6GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP067CF163001', N'P067', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP067CF163002', N'P067', N'Ryzen 7 7840HS / 16GB / 1TB SSD / RTX 4060 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP067CF164001', N'P067', N'Core i7-13620H / 32GB / 1TB SSD / RTX 4070 8GB', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP068CF165001', N'P068', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP068CF165002', N'P068', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP068CF165003', N'P068', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP068CF166001', N'P068', N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP068CF166002', N'P068', N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP068CF167001', N'P068', N'Ultra 9 275 HX / 32GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP069CF168001', N'P069', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP069CF168002', N'P069', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP069CF168003', N'P069', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP069CF169001', N'P069', N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP069CF169002', N'P069', N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP069CF170001', N'P069', N'Ultra 9 275 HX / 32GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP070CF171001', N'P070', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP070CF171002', N'P070', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP070CF171003', N'P070', N'Ultra 5 125H / 8GB / 512GB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP070CF172001', N'P070', N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP070CF172002', N'P070', N'Ultra 7 165H / 16GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);
INSERT INTO ProductSerial(serial_id, product_id, specifications, stockExportDetail_id, status, import_date, export_date, warranty_start_date, warranty_end_date, note) VALUES (N'SRP070CF173001', N'P070', N'Ultra 9 275 HX / 32GB / 1TB SSD / Iris Xe', NULL, N'in stock', CAST(N'2025-11-01T16:08:47.077' AS DateTime), NULL, NULL, NULL, NULL);


---- ========== WARRANTY ==========
INSERT INTO Warranty (warranty_id, customer_id, phone_number, serial_id, employee_id, type, content_detail, status, time, total_amount)
VALUES (N'WA001', N'C001', N'0905123456', N'SRP001CF002001', N'E003', N'Bảo hành', N'Thay bàn phím', N'Đang xử lý', CAST(N'2025-10-30T22:08:47.077' AS DateTime), CAST(0.00 AS Decimal(18,2)));
INSERT INTO Warranty (warranty_id, customer_id, phone_number, serial_id, employee_id, type, content_detail, status, time, total_amount)
VALUES (N'WA002', N'C002', N'0906554321', N'SRP009CF020001', N'E002', N'Bảo hành', N'Hỏng màn hình', N'Hoàn thành', CAST(N'2025-10-30T22:08:47.077' AS DateTime), CAST(400000.00 AS Decimal(18,2)));

--select employee_id from StockExport inner join StockExportDetail on StockExport.stockExport_id = StockExportDetail.stockExport_id
--									inner join ProductSerial on ProductSerial.stockExportDetail_id = StockExportDetail.stockExportDetail_id
--where serial_id = N'SRP009CF020003'


--select serial_id from SaleInvoice inner join StockExport on SaleInvoice.saleInvoice_id = StockExport.saleInvoice_id
--								  inner join StockExportDetail on StockExport.stockExport_id = StockExportDetail.stockExport_id
--								  inner join ProductSerial on StockExportDetail.stockExportDetail_id = ProductSerial.stockExportDetail_id
--where SaleInvoice.saleInvoice_id = N'SI001'

--delete from ProductConfiguration where configuration_id = N'CF075'
--select * from ProductConfiguration

--select * from Product 
--select * from Customer
--delete from ProductSerial where serial_id = N'SRP025CF052007'

--select count(*) from ProductSerial
--select count(*) from StockImport
--select count(*) from StockImportDetail
--delete from Customer where customer_id = N'KH001'
--select * from ProductSerial
--select quantity from ProductConfiguration 
--where configuration_id = N'CF001'

--select * from StockImportDetail

--select * from Promotion
--select * from Employee

--select * from CartDetail
--select * from Cart 
--select * from SaleInvoice
--select * from SaleInvoiceDetail

--select * from History


--delete from Chat
--where chat_id = N'CHAT2025121019146f46'
--select * from Chat
--delete from SaleInvoice where saleInvoice_id = N'SI008'
--delete from SaleInvoiceDetail where saleInvoice_id = N'SI008'

--delete from Cart where cart_id = N'CA002'
--delete from CartDetail where cart_id = N'CA002'
--delete from Customer
--where customer_id = N'C013'

--select * from ProductReview
--select * from ProductConfiguration
--where quantity = 0

--update ProductConfiguration
--set quantity = 2
--where configuration_id = N'CF025'


--ALTER TABLE [dbo].[Customer]
--alter column [address] nvarchar(500) NULL;


--ALTER TABLE [dbo].[warranty]
--add [time] [datetime] NULL

-- Migration script to create recommendation system tables
-- Run this script on your SQL Server database
