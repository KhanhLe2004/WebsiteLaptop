using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Controllers;

[Route("api/Chat")]
[ApiController]
public class ChatAPIController : ControllerBase
{
    private readonly WebLaptopTenTechContext _db;

    public ChatAPIController(WebLaptopTenTechContext db)
    {
        _db = db;
    }

    [HttpGet("GetMessages")]
    public IActionResult GetMessages([FromQuery] string? customerId, [FromQuery] string? employeeId)
    {
        try
        {
            var query = _db.Chats.AsQueryable();

            if (!string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(employeeId))
            {
                // When employee views conversation: show all messages for this customer
                // including unassigned ones (employeeId = null) and assigned ones (employeeId = this employeeId)
                query = query.Where(c => c.CustomerId == customerId && 
                    (c.EmployeeId == null || c.EmployeeId == employeeId));
            }
            else if (!string.IsNullOrEmpty(customerId))
            {
                // Customer view: show all messages for this customer
                query = query.Where(c => c.CustomerId == customerId);
            }
            else if (!string.IsNullOrEmpty(employeeId))
            {
                // Employee view all: show messages assigned to this employee
                query = query.Where(c => c.EmployeeId == employeeId);
            }

            var messages = query
                .Include(c => c.Customer)
                .Include(c => c.Employee)
                .OrderBy(c => c.Time)
                .ToList()
                .Select(c => {
                    // Use SenderType from database if available, otherwise determine from EmployeeId
                    string senderType;
                    if (!string.IsNullOrEmpty(c.SenderType))
                    {
                        senderType = c.SenderType;
                    }
                    else
                    {
                        // Fallback: determine from EmployeeId (for old messages without SenderType)
                        if (!string.IsNullOrEmpty(employeeId) && c.EmployeeId == employeeId)
                        {
                            senderType = "employee";
                        }
                        else
                        {
                            senderType = "customer";
                        }
                    }
                    
                    return new ChatDTO
                    {
                        ChatId = c.ChatId,
                        ContentDetail = c.ContentDetail,
                        Time = c.Time,
                        Status = c.Status,
                        CustomerId = c.CustomerId,
                        EmployeeId = c.EmployeeId,
                        CustomerName = c.Customer != null ? c.Customer.CustomerName : null,
                        EmployeeName = c.Employee != null ? c.Employee.EmployeeName : null,
                        CustomerAvatar = c.Customer != null ? c.Customer.Avatar : null,
                        EmployeeAvatar = c.Employee != null ? c.Employee.Avatar : null,
                        SenderType = senderType
                    };
                })
                .ToList();

            return Ok(messages);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy tin nhắn", error = ex.Message });
        }
    }

    [HttpGet("GetConversations/Employee")]
    public IActionResult GetEmployeeConversations([FromQuery] string employeeId)
    {
        try
        {
            // Get all conversations:
            // 1. Conversations assigned to this employee (has messages with this employeeId)
            // 2. Unassigned conversations (has messages with employeeId = null) - these are new customer messages
            var assignedCustomerIds = _db.Chats
                .Where(c => c.EmployeeId == employeeId)
                .Select(c => c.CustomerId)
                .Distinct()
                .ToList();
            
            var unassignedCustomerIds = _db.Chats
                .Where(c => c.EmployeeId == null && c.CustomerId != null)
                .Select(c => c.CustomerId)
                .Distinct()
                .ToList();
            
            var allCustomerIds = assignedCustomerIds.Union(unassignedCustomerIds).Distinct().ToList();
            
            var conversations = allCustomerIds
                .Select(customerId => {
                    var allChatsForCustomer = _db.Chats
                        .Where(c => c.CustomerId == customerId)
                        .ToList();
                    
                    var lastMessage = allChatsForCustomer.OrderByDescending(c => c.Time).FirstOrDefault();
                    
                    // Unread count: messages sent by customer with status "sent" that haven't been read
                    // Only count messages from customer (not from employee)
                    var unreadCount = allChatsForCustomer.Count(c => 
                        c.Status == "sent" && // Only unread messages
                        // Message is from customer if:
                        // 1. SenderType is "customer" (most reliable)
                        // 2. SenderType is null AND EmployeeId is null (unassigned messages, definitely from customer)
                        // 3. SenderType is null AND EmployeeId is not null - this is ambiguous, check if it's actually from customer
                        //    by checking if there's a pattern: if EmployeeId exists but SenderType is null, it might be old data
                        //    We'll prioritize: if SenderType exists, use it; otherwise, if EmployeeId is null, it's from customer
                        ((c.SenderType != null && c.SenderType == "customer") ||
                         (c.SenderType == null && c.EmployeeId == null)));
                    
                    return new ChatConversationDTO
                    {
                        CustomerId = customerId,
                        CustomerName = _db.Customers.FirstOrDefault(cust => cust.CustomerId == customerId)?.CustomerName,
                        CustomerAvatar = _db.Customers.FirstOrDefault(cust => cust.CustomerId == customerId)?.Avatar,
                        LastMessageTime = lastMessage?.Time,
                        LastMessage = lastMessage?.ContentDetail,
                        UnreadCount = unreadCount
                    };
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách cuộc trò chuyện", error = ex.Message });
        }
    }

    [HttpGet("GetConversations/Customer")]
    public IActionResult GetCustomerConversations([FromQuery] string customerId)
    {
        try
        {
            // Get all messages for this customer
            var allChatsForCustomer = _db.Chats
                .Where(c => c.CustomerId == customerId)
                .ToList();
            
            var lastMessage = allChatsForCustomer.OrderByDescending(c => c.Time).FirstOrDefault();
            
            // Unread count: messages sent by employee with status "sent" that haven't been read
            // Only count messages from employee (not from customer)
            var unreadCount = allChatsForCustomer.Count(c => 
                c.Status == "sent" && // Only unread messages (not "read")
                // Message is from employee if:
                // 1. SenderType is "employee" (most reliable)
                // 2. SenderType is null AND EmployeeId is not null (old messages from employee)
                // 3. SenderType is null AND EmployeeId is null AND CustomerId matches (fallback - should not happen)
                ((c.SenderType != null && c.SenderType == "employee") ||
                 (c.SenderType == null && c.EmployeeId != null && c.EmployeeId != "")));
            
            var conversation = new ChatConversationDTO
            {
                CustomerId = customerId,
                CustomerName = _db.Customers.FirstOrDefault(cust => cust.CustomerId == customerId)?.CustomerName,
                CustomerAvatar = _db.Customers.FirstOrDefault(cust => cust.CustomerId == customerId)?.Avatar,
                LastMessageTime = lastMessage?.Time,
                LastMessage = lastMessage?.ContentDetail,
                UnreadCount = unreadCount
            };
            
            var conversations = new[] { conversation };

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách cuộc trò chuyện", error = ex.Message });
        }
    }

    [HttpPost("MarkAsRead")]
    public IActionResult MarkAsRead([FromBody] MarkAsReadRequest request)
    {
        try
        {
            // Mark all unread messages from customer as read
            // Only mark messages that were sent by customer
            // Logic: If EmployeeId is null, it was sent by customer
            //        If SenderType exists and is "customer", it was sent by customer
            var messages = _db.Chats
                .Where(c => c.CustomerId == request.CustomerId && 
                           (request.EmployeeId == null || c.EmployeeId == request.EmployeeId || c.EmployeeId == null) &&
                           c.Status == "sent");

            // Filter to only messages from the OTHER party
            List<Chat> messagesToMark;
            if (!string.IsNullOrEmpty(request.EmployeeId))
            {
                // Employee is marking messages as read: mark messages sent by customer
                messagesToMark = messages.Where(c => 
                    (c.SenderType != null && c.SenderType == "customer") ||
                    (c.SenderType == null && c.EmployeeId == null) // Unassigned messages are from customer
                ).ToList();
            }
            else
            {
                // Customer is marking messages as read: mark messages sent by employee
                messagesToMark = messages.Where(c => 
                    (c.SenderType != null && c.SenderType == "employee") ||
                    (c.SenderType == null && c.EmployeeId != null) // Messages with EmployeeId are from employee
                ).ToList();
            }

            foreach (var message in messagesToMark)
            {
                message.Status = "read";
            }

            _db.SaveChanges();
            return Ok(new { message = "Đã đánh dấu đã đọc", count = messagesToMark.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi đánh dấu đã đọc", error = ex.Message });
        }
    }

    public class MarkAsReadRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
    }
}

