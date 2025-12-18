# 💻 Laptop E-Commerce Website

## 📌 Overview

This project is a full-featured laptop e-commerce management system, developed to support online sales, inventory control, and business reporting.
The system integrates user-facing e-commerce functions with backend management modules, enabling efficient operations across sales, inventory, maintenance, and analytics.

## 🛠️ Technologies Used

### Frontend
  HTML – Page structure
  CSS – Styling and layout
  JavaScript – Client-side interaction

### Backend
  ASP.NET – Backend application development
  RESTful API – Communication between frontend and backend

### Database
  SQL Server – Relational database management

## ✨ Key Features

### 👤 User Functions
  - User authentication and authorization
  - Browse laptops with advanced search and filtering (brand, price, specifications)
  - View detailed product information
  - Add products to cart and place orders
  - Online payment integration
  - View order history
  - AI-powered chatbot for customer support and product inquiries
### 🔐 Admin & Staff Functions
  - Role-based authorization (Admin / Staff)
  - Product and category management
  - Inventory management:
  - Goods receipt (import)
  - Goods issue (export)
  - Stock quantity tracking
  - Repair & maintenance management:
  - Track laptop repair records
  - Update repair status
  - Order management and processing
  - Sales reporting and analytics dashboard
    
## 🗂️ Database Design

The database follows a relational model to ensure data consistency and integrity.
Main tables include:
  - Product, Brand, ProductImage, ProductConfiguration, ProductSerial – Product information and serial-level inventory tracking
  - Customer, Employee, Role – User management and role-based authorization
  - Cart, CartDetail, SaleInvoice, SaleInvoiceDetail, Promotion, ProductReview – Sales and order processing
  - StockImport, StockImportDetail, StockExport, StockExportDetail, Supplier – Inventory import and export management
  - Warranty – Warranty and repair tracking
  - Chat – Customer support and AI chatbot interaction
  - History – System activity logging
Relationships between tables are enforced using primary and foreign keys to maintain accurate data across sales, inventory, and post-sale workflows.

## 🔄 Data Processing & Reporting

### Data is collected and processed through RESTful APIs

### SQL queries are used for:
  - Sales statistics
  - Inventory status tracking
  - Repair history analysis

### Reporting dashboards provide insights into:
  - Revenue by time period
  - Top-selling products
  - Inventory inflow and outflow

## 🚀 Getting Started
### Prerequisites
  Visual Studio
  SQL Server & SQL Server Management Studio (SSMS)
  Modern web browser

### Installation
  1. Clone the repository: https://github.com/KhanhLe2004/WebsiteLaptop.git
  2. Database setup
    Open SSMS
    Run the provided SQL script to create tables and sample data
  3. Backend configuration
    Open the ASP.NET project
    Update the connection string in appsettings.json
  4. Run the application
    Start the backend API
    Open the frontend application in a browser
