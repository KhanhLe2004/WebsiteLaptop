using Microsoft.AspNetCore.SignalR;
using WebLaptopBE.Data;
using WebLaptopBE.Models;

namespace WebLaptopBE.Hubs;

public class ChatHub : Hub
{
    private readonly Testlaptop38Context _db;

    public ChatHub(Testlaptop38Context db)
    {
        _db = db;
    }

        public async Task JoinRoom(string userId, string userType)
        {
            var roomName = GetRoomName(userId, userType);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            
            // Also add employees to a general group for broadcasting
            if (userType == "employee")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "employee_all");
            }
            
            if (userType == "customer")
            {
                await Clients.Group(roomName).SendAsync("UserJoined", userId, "customer");
            }
            else if (userType == "employee")
            {
                await Clients.Group(roomName).SendAsync("UserJoined", userId, "employee");
            }
        }

        public async Task SendMessage(string customerId, string? employeeId, string content, string senderType)
        {
            try
            {
                // When employee sends first message, assign all previous customer messages to this employee
                // But keep the SenderType unchanged (still "customer")
                if (senderType == "employee" && !string.IsNullOrEmpty(employeeId))
                {
                    var unassignedMessages = _db.Chats
                        .Where(c => c.CustomerId == customerId && c.EmployeeId == null && c.SenderType == "customer")
                        .ToList();
                    
                    foreach (var msg in unassignedMessages)
                    {
                        msg.EmployeeId = employeeId;
                        // Keep SenderType as "customer" - don't change it
                    }
                    
                    if (unassignedMessages.Any())
                    {
                        await _db.SaveChangesAsync();
                    }
                }
                
                // When customer sends: EmployeeId should be null (will be assigned when employee responds)
                // When employee sends: both CustomerId and EmployeeId should be set
                var chat = new Chat
                {
                    ChatId = GenerateChatId(),
                    CustomerId = customerId,
                    EmployeeId = senderType == "employee" ? employeeId : null,
                    SenderType = senderType, // Store sender type to distinguish who sent the message
                    ContentDetail = content,
                    Time = DateTime.Now,
                    Status = "sent"
                };

                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();

            var customer = _db.Customers.FirstOrDefault(c => c.CustomerId == customerId);
            var employee = employeeId != null ? _db.Employees.FirstOrDefault(e => e.EmployeeId == employeeId) : null;

            var messageData = new
            {
                chatId = chat.ChatId,
                contentDetail = chat.ContentDetail,
                time = chat.Time,
                customerId = chat.CustomerId,
                employeeId = chat.EmployeeId,
                customerName = customer?.CustomerName,
                employeeName = employee?.EmployeeName,
                customerAvatar = customer?.Avatar,
                employeeAvatar = employee?.Avatar,
                senderType = senderType
            };

            if (senderType == "customer")
            {
                // Notify all employees about new message
                await Clients.Group("employee_all").SendAsync("ReceiveMessage", messageData);
                // Also send back to the sender so they can see their message
                var customerRoom = GetRoomName(customerId, "customer");
                await Clients.Group(customerRoom).SendAsync("ReceiveMessage", messageData);
            }
            else if (senderType == "employee")
            {
                var customerRoom = GetRoomName(customerId, "customer");
                await Clients.Group(customerRoom).SendAsync("ReceiveMessage", messageData);
                // Also send back to the sender so they can see their message
                var employeeRoom = GetRoomName(employeeId ?? "", "employee");
                await Clients.Group(employeeRoom).SendAsync("ReceiveMessage", messageData);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    private string GetRoomName(string userId, string userType)
    {
        return $"{userType}_{userId}";
    }

    private string GenerateChatId()
    {
        // Generate a unique ID with format CH001, CH002, CH003...
        // Get the latest ChatId from database
        var latestChat = _db.Chats
            .Where(c => c.ChatId.StartsWith("CH") && c.ChatId.Length == 5)
            .OrderByDescending(c => c.ChatId)
            .FirstOrDefault();
        
        int nextNumber = 1;
        
        if (latestChat != null)
        {
            // Extract number from latest ID (e.g., "CH001" -> 1)
            var numberPart = latestChat.ChatId.Substring(2); // Remove "CH" prefix
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }
        
        // Format as CH001, CH002, etc. (max 99999)
        return $"CH{nextNumber:D3}";
    }
}

