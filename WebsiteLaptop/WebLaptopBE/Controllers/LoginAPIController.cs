using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;
using WebLaptopBE.Data;
using Google.Apis.Auth;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
namespace WebLaptopBE.Controllers
{
    [Route("api/Login")]
    [ApiController]
    public class LoginAPIController : ControllerBase
    {
        private readonly Testlaptop35Context _db = new();
        private readonly IConfiguration _configuration;

        public LoginAPIController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrWhiteSpace(request.EmailOrUsername) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Email/Tên đăng nhập và mật khẩu không được để trống" });
                }

                var credential = request.EmailOrUsername.Trim();
                var password = request.Password.Trim();

                if (password.Length > 50)
                {
                    return BadRequest(new { message = "Mật khẩu không hợp lệ" });
                }

                var customer = _db.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c =>
                        (c.Email == credential || c.Username == credential) &&
                        c.Password == password &&
                        (c.Active == null || c.Active.Value));

                if (customer == null)
                {
                    return Unauthorized(new { message = "Email/Tên đăng nhập hoặc mật khẩu không đúng" });
                }

                return Ok(new
                {
                    message = "Đăng nhập thành công",
                    customer = new
                    {
                        customer.CustomerId,
                        customer.CustomerName,
                        customer.Email,
                        customer.PhoneNumber,
                        customer.Address,
                        customer.Username,
                        customer.Avatar,
                        customer.Active
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi đăng nhập",
                    error = ex.Message
                });
            }
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Dữ liệu đăng nhập không hợp lệ" });
                }

                GoogleJsonWebSignature.Payload payload = null;

                // Ưu tiên sử dụng ID token
                if (!string.IsNullOrWhiteSpace(request.IdToken))
                {
                    try
                    {
                        var settings = new GoogleJsonWebSignature.ValidationSettings();
                        var clientId = _configuration["GoogleOAuth:ClientId"];
                        if (!string.IsNullOrEmpty(clientId))
                        {
                            settings.Audience = new[] { clientId };
                        }

                        payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                    }
                    catch (Exception ex)
                    {
                        return Unauthorized(new { message = "Token Google không hợp lệ", error = ex.Message });
                    }
                }
                // Fallback: Sử dụng access_token và userInfo
                else if (!string.IsNullOrWhiteSpace(request.AccessToken) && request.UserInfo != null)
                {
                    // Verify access_token bằng cách gọi Google API
                    using var httpClient = new HttpClient();
                    try
                    {
                        var verifyResponse = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={request.AccessToken}");
                        if (!verifyResponse.IsSuccessStatusCode)
                        {
                            return Unauthorized(new { message = "Access token Google không hợp lệ" });
                        }

                        // Sử dụng thông tin từ userInfo
                        payload = new GoogleJsonWebSignature.Payload
                        {
                            Email = request.UserInfo.Email?.ToLowerInvariant(),
                            Name = request.UserInfo.Name,
                            Picture = request.UserInfo.Picture
                        };
                    }
                    catch (Exception ex)
                    {
                        return Unauthorized(new { message = "Không thể xác thực Google token", error = ex.Message });
                    }
                }
                else
                {
                    return BadRequest(new { message = "Token Google hoặc thông tin đăng nhập không được để trống" });
                }

                if (payload == null)
                {
                    return Unauthorized(new { message = "Không thể xác thực thông tin Google" });
                }

                var email = payload.Email?.ToLowerInvariant();
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email từ Google không hợp lệ" });
                }

                // Tìm hoặc tạo customer
                var customer = _db.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c => c.Email == email);

                if (customer == null)
                {
                    // Tạo customer mới từ Google
                    customer = new Customer
                    {
                        CustomerId = GenerateCustomerId(),
                        CustomerName = payload.Name ?? payload.Email.Split('@')[0],
                        Email = email,
                        Username = GenerateUsername(email),
                        Avatar = payload.Picture,
                        Active = true,
                        Password = "GOOGLE_OAUTH" // Đánh dấu đăng nhập bằng Google
                    };

                    _db.Customers.Add(customer);
                    _db.SaveChanges();

                    // Reload để lấy dữ liệu đầy đủ
                    customer = _db.Customers
                        .AsNoTracking()
                        .FirstOrDefault(c => c.CustomerId == customer.CustomerId);
                }
                else
                {
                    // Cập nhật thông tin nếu cần
                    if (customer.Active == false)
                    {
                        return Unauthorized(new { message = "Tài khoản của bạn đã bị khóa" });
                    }

                    // Chỉ cập nhật avatar nếu customer chưa có avatar (chưa upload ảnh)
                    // Không ghi đè avatar mà user đã upload
                    if (string.IsNullOrEmpty(customer.Avatar) && !string.IsNullOrEmpty(payload.Picture))
                    {
                        var customerToUpdate = _db.Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
                        if (customerToUpdate != null)
                        {
                            customerToUpdate.Avatar = payload.Picture;
                            if (string.IsNullOrEmpty(customerToUpdate.CustomerName) && !string.IsNullOrEmpty(payload.Name))
                            {
                                customerToUpdate.CustomerName = payload.Name;
                            }
                            _db.SaveChanges();
                            customer.Avatar = payload.Picture;
                            if (string.IsNullOrEmpty(customer.CustomerName))
                            {
                                customer.CustomerName = payload.Name;
                            }
                        }
                    }
                    else if (string.IsNullOrEmpty(customer.CustomerName) && !string.IsNullOrEmpty(payload.Name))
                    {
                        // Chỉ cập nhật tên nếu chưa có tên
                        var customerToUpdate = _db.Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
                        if (customerToUpdate != null)
                        {
                            customerToUpdate.CustomerName = payload.Name;
                            _db.SaveChanges();
                            customer.CustomerName = payload.Name;
                        }
                    }
                }

                return Ok(new
                {
                    message = "Đăng nhập bằng Google thành công",
                    customer = new
                    {
                        customer.CustomerId,
                        customer.CustomerName,
                        customer.Email,
                        customer.PhoneNumber,
                        customer.Address,
                        customer.Username,
                        customer.Avatar,
                        customer.Active
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi đăng nhập bằng Google",
                    error = ex.Message
                });
            }
        }

        private string GenerateCustomerId()
        {
            var lastId = _db.Customers
                .OrderByDescending(c => c.CustomerId)
                .Select(c => c.CustomerId)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(lastId) || lastId.Length < 2)
            {
                return "C001";
            }

            var numericPart = lastId.Substring(1);
            if (!int.TryParse(numericPart, out var number))
            {
                number = 0;
            }

            return $"C{number + 1:000}";
        }

        private string GenerateUsername(string email)
        {
            var baseUsername = email.Split('@')[0];
            var username = baseUsername;
            var suffix = 1;

            while (_db.Customers.Any(c => c.Username == username))
            {
                username = $"{baseUsername}{suffix++}";
                if (suffix > 999)
                {
                    username = $"{baseUsername}{Guid.NewGuid():N}".Substring(0, Math.Min(baseUsername.Length + 4, 50));
                    break;
                }
            }

            return username;
        }

        private string? TruncateAvatarUrl(string? url, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            // Nếu URL quá dài, không lưu (để tránh lỗi database)
            // URL từ Facebook thường rất dài, nên không lưu trực tiếp
            if (url.Length > maxLength)
            {
                System.Diagnostics.Debug.WriteLine($"Avatar URL quá dài ({url.Length} ký tự, giới hạn: {maxLength}), không lưu. URL: {url.Substring(0, Math.Min(80, url.Length))}...");
                return null; // Không lưu URL quá dài - user có thể upload avatar sau
            }

            return url;
        }


        public class LoginRequest
        {
            public string EmailOrUsername { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class GoogleLoginRequest
        {
            public string IdToken { get; set; } = string.Empty;
            public string AccessToken { get; set; } = string.Empty;
            public GoogleUserInfo? UserInfo { get; set; }
        }

        [HttpPost("facebook")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    return BadRequest(new { message = "Access token Facebook không được để trống" });
                }

                // Verify access token với Facebook Graph API
                using var httpClient = new HttpClient();
                try
                {
                    // Lấy thông tin user từ Facebook - lấy id, name, email, picture
                    // Request picture với width nhỏ hơn để có URL ngắn hơn
                    var userInfoResponse = await httpClient.GetAsync($"https://graph.facebook.com/me?fields=id,name,email,picture.width(200).height(200)&access_token={request.AccessToken}");
                    var responseContent = await userInfoResponse.Content.ReadAsStringAsync();
                    
                    // Nếu không có email trong response, thử request riêng email
                    string? emailFromSeparateRequest = null;
                    if (!responseContent.Contains("\"email\""))
                    {
                        System.Diagnostics.Debug.WriteLine("Không tìm thấy email trong response đầu tiên, thử request riêng email...");
                        try
                        {
                            var emailResponse = await httpClient.GetAsync($"https://graph.facebook.com/me?fields=email&access_token={request.AccessToken}");
                            var emailContent = await emailResponse.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"Email response: {emailContent}");
                            
                            if (emailResponse.IsSuccessStatusCode)
                            {
                                var emailDoc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(emailContent);
                                if (emailDoc.TryGetProperty("email", out var emailElement))
                                {
                                    emailFromSeparateRequest = emailElement.GetString();
                                    System.Diagnostics.Debug.WriteLine($"Đã lấy được email từ request riêng: {emailFromSeparateRequest}");
                                }
                            }
                        }
                        catch (Exception emailEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi request email riêng: {emailEx.Message}");
                        }
                    }
                    
                    if (!userInfoResponse.IsSuccessStatusCode)
                    {
                        // Log lỗi chi tiết
                        System.Diagnostics.Debug.WriteLine($"Facebook API Error: {userInfoResponse.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"Response: {responseContent}");
                        
                        // Parse error từ Facebook
                        try
                        {
                            var errorObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                            if (errorObj.TryGetProperty("error", out var error))
                            {
                                var errorMessage = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Lỗi không xác định";
                                return Unauthorized(new { message = $"Facebook API Error: {errorMessage}", details = responseContent });
                            }
                        }
                        catch { }
                        
                        return Unauthorized(new { message = "Access token Facebook không hợp lệ hoặc đã hết hạn", details = responseContent });
                    }

                    // Log response để debug
                    System.Diagnostics.Debug.WriteLine($"Facebook API Response: {responseContent}");
                    
                    // Parse user info - sử dụng JsonElement để linh hoạt hơn
                    string? facebookId = null;
                    string? name = null;
                    string? email = null;
                    string? pictureUrl = null;
                    
                    try
                    {
                        var jsonDoc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                        
                        // Lấy ID
                        if (jsonDoc.TryGetProperty("id", out var idElement))
                        {
                            facebookId = idElement.GetString();
                            System.Diagnostics.Debug.WriteLine($"Facebook ID: {facebookId}");
                        }
                        
                        // Lấy name
                        if (jsonDoc.TryGetProperty("name", out var nameElement))
                        {
                            name = nameElement.GetString();
                            System.Diagnostics.Debug.WriteLine($"Facebook Name: {name}");
                        }
                        
                        // Lấy email
                        if (jsonDoc.TryGetProperty("email", out var emailElement))
                        {
                            email = emailElement.GetString();
                            System.Diagnostics.Debug.WriteLine($"Facebook Email: {email}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Facebook không trả về email trong response chính - có thể do chưa được cấp quyền hoặc app chưa được review");
                            // Sử dụng email từ request riêng nếu có
                            if (!string.IsNullOrEmpty(emailFromSeparateRequest))
                            {
                                email = emailFromSeparateRequest;
                                System.Diagnostics.Debug.WriteLine($"Sử dụng email từ request riêng: {email}");
                            }
                        }
                        
                        // Lấy picture - Facebook có thể trả về nhiều format
                        if (jsonDoc.TryGetProperty("picture", out var pictureElement))
                        {
                            // Format 1: picture.data.url
                            if (pictureElement.TryGetProperty("data", out var dataElement))
                            {
                                if (dataElement.TryGetProperty("url", out var urlElement))
                                {
                                    pictureUrl = urlElement.GetString();
                                }
                            }
                            // Format 2: picture.url (direct)
                            else if (pictureElement.TryGetProperty("url", out var directUrlElement))
                            {
                                pictureUrl = directUrlElement.GetString();
                            }
                            // Format 3: picture là string trực tiếp
                            else if (pictureElement.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                pictureUrl = pictureElement.GetString();
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Parse Error: {parseEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"Response Content: {responseContent}");
                        return StatusCode(500, new { message = "Lỗi khi xử lý dữ liệu từ Facebook", error = parseEx.Message, details = responseContent });
                    }

                    if (string.IsNullOrEmpty(facebookId))
                    {
                        return Unauthorized(new { message = "Không thể lấy thông tin từ Facebook", details = responseContent });
                    }

                    // Sử dụng email từ Facebook nếu có, nếu không thì để null (không tạo email tạm)
                    if (string.IsNullOrEmpty(email))
                    {
                        System.Diagnostics.Debug.WriteLine($"Không lấy được email từ Facebook, để email = null");
                        email = null;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Đã lấy được email từ Facebook: {email}");
                    }

                    // Tìm customer bằng email (nếu có) hoặc username từ Facebook ID
                    // Không tìm bằng email tạm (email có dạng fb_*@facebook.temp)
                    var usernameFromId = $"fb_{facebookId}";
                    var existingCustomer = _db.Customers
                        .FirstOrDefault(c => 
                            (!string.IsNullOrEmpty(email) && c.Email == email && !c.Email.EndsWith("@facebook.temp")) || 
                            c.Username == usernameFromId);

                    Customer? customer = null;
                    
                    if (existingCustomer == null)
                    {
                        // Kiểm tra duplicate trước khi tạo
                        var customerId = GenerateCustomerId();
                        
                        // Đảm bảo CustomerId không trùng
                        while (_db.Customers.Any(c => c.CustomerId == customerId))
                        {
                            customerId = GenerateCustomerId();
                        }
                        
                        // Đảm bảo Username không trùng
                        var finalUsername = usernameFromId;
                        var usernameSuffix = 1;
                        while (_db.Customers.Any(c => c.Username == finalUsername))
                        {
                            finalUsername = $"{usernameFromId}_{usernameSuffix++}";
                            if (usernameSuffix > 999)
                            {
                                finalUsername = $"{usernameFromId}_{Guid.NewGuid():N}".Substring(0, Math.Min(usernameFromId.Length + 10, 50));
                                break;
                            }
                        }
                        
                        // Đảm bảo Email không trùng (chỉ nếu có email)
                        var finalEmail = email;
                        if (!string.IsNullOrEmpty(finalEmail) && _db.Customers.Any(c => c.Email == finalEmail))
                        {
                            // Nếu email trùng, để null thay vì tạo email tạm
                            finalEmail = null;
                        }
                        
                        // Tạo customer mới từ Facebook
                        // Lưu avatar URL từ Facebook (đã được rút ngắn bằng cách request picture nhỏ hơn)
                        // Nếu URL vẫn quá dài, sẽ được xử lý bởi TruncateAvatarUrl
                        
                        var newCustomer = new Customer
                        {
                            CustomerId = customerId,
                            CustomerName = name ?? $"Facebook User {facebookId}",
                            Email = finalEmail, // Có thể là null nếu không lấy được email
                            Username = finalUsername,
                            Avatar = TruncateAvatarUrl(pictureUrl, 200), // Lưu avatar URL (rút ngắn nếu cần)
                            Active = true,
                            Password = "FACEBOOK_OAUTH", // Đánh dấu đăng nhập bằng Facebook
                            Address = string.Empty // Đảm bảo không null
                        };

                        try
                        {
                            _db.Customers.Add(newCustomer);
                            _db.SaveChanges();
                            
                            // Lấy customer vừa tạo
                            customer = newCustomer;
                        }
                        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                        {
                            var innerEx = dbEx.InnerException;
                            var errorDetails = innerEx?.Message ?? dbEx.Message;
                            
                            System.Diagnostics.Debug.WriteLine($"Database Error: {dbEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"Inner Exception: {errorDetails}");
                            System.Diagnostics.Debug.WriteLine($"Stack Trace: {dbEx.StackTrace}");
                            
                            // Kiểm tra loại lỗi
                            if (errorDetails.Contains("PRIMARY KEY") || errorDetails.Contains("duplicate") || errorDetails.Contains("UNIQUE"))
                            {
                                // Thử tìm lại customer (có thể đã được tạo bởi request khác)
                                customer = _db.Customers.FirstOrDefault(c => c.Email == finalEmail || c.Username == finalUsername);
                                if (customer != null)
                                {
                                    // Customer đã tồn tại, tiếp tục xử lý
                                    System.Diagnostics.Debug.WriteLine("Customer đã tồn tại, sử dụng customer hiện có");
                                }
                                else
                                {
                                    return Conflict(new { 
                                        message = "Thông tin khách hàng đã tồn tại. Vui lòng thử lại.", 
                                        error = errorDetails,
                                        details = $"CustomerId: {customerId}, Email: {finalEmail}, Username: {finalUsername}"
                                    });
                                }
                            }
                            else if (errorDetails.Contains("FOREIGN KEY") || errorDetails.Contains("constraint"))
                            {
                                return BadRequest(new { 
                                    message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại thông tin.", 
                                    error = errorDetails,
                                    details = $"CustomerId: {customerId}, Email: {finalEmail}, Username: {finalUsername}"
                                });
                            }
                            else
                            {
                                return StatusCode(500, new { 
                                    message = "Lỗi khi lưu thông tin khách hàng", 
                                    error = dbEx.Message, 
                                    details = errorDetails,
                                    debugInfo = $"CustomerId: {customerId}, Email: {finalEmail}, Username: {finalUsername}, Name: {name}"
                                });
                            }
                        }
                        catch (Exception dbEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Database Error: {dbEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"Stack Trace: {dbEx.StackTrace}");
                            if (dbEx.InnerException != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                            }
                            
                            return StatusCode(500, new { 
                                message = "Lỗi khi lưu thông tin khách hàng", 
                                error = dbEx.Message, 
                                details = dbEx.InnerException?.Message,
                                stackTrace = dbEx.StackTrace
                            });
                        }
                    }
                    else
                    {
                        customer = existingCustomer;
                        
                        // Cập nhật thông tin nếu cần
                        if (customer.Active == false)
                        {
                            return Unauthorized(new { message = "Tài khoản của bạn đã bị khóa" });
                        }

                        // Cập nhật thông tin nếu cần
                        bool hasChanges = false;
                        
                        // Cập nhật tên nếu cần
                        if (string.IsNullOrEmpty(customer.CustomerName) && !string.IsNullOrEmpty(name))
                        {
                            customer.CustomerName = name;
                            hasChanges = true;
                        }
                        
                        // Xóa email tạm nếu có (email có dạng fb_*@facebook.temp)
                        if (!string.IsNullOrEmpty(customer.Email) && customer.Email.EndsWith("@facebook.temp"))
                        {
                            customer.Email = null; // Xóa email tạm
                            hasChanges = true;
                        }
                        
                        // Cập nhật email nếu customer chưa có email và có email từ Facebook
                        if (!string.IsNullOrEmpty(email) && string.IsNullOrEmpty(customer.Email))
                        {
                            // Kiểm tra email mới không trùng với email khác
                            if (!_db.Customers.Any(c => c.CustomerId != customer.CustomerId && c.Email == email))
                            {
                                customer.Email = email;
                                hasChanges = true;
                            }
                        }
                        
                        // Chỉ cập nhật avatar nếu customer chưa có avatar (chưa upload ảnh)
                        // Không ghi đè avatar mà user đã upload
                        if (string.IsNullOrEmpty(customer.Avatar) && !string.IsNullOrEmpty(pictureUrl))
                        {
                            var truncatedAvatar = TruncateAvatarUrl(pictureUrl, 200);
                            if (truncatedAvatar != null)
                            {
                                customer.Avatar = truncatedAvatar;
                                hasChanges = true;
                            }
                        }
                        
                        if (hasChanges)
                        {
                            try
                            {
                                _db.SaveChanges();
                            }
                            catch (Exception dbEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Database Update Error: {dbEx.Message}");
                                // Không trả về lỗi, chỉ log vì đăng nhập vẫn thành công
                            }
                        }
                    }
                    
                    // Đảm bảo customer không null
                    if (customer == null)
                    {
                        return StatusCode(500, new { message = "Không thể tạo hoặc tìm thấy khách hàng" });
                    }

                    return Ok(new
                    {
                        message = "Đăng nhập bằng Facebook thành công",
                        customer = new
                        {
                            customer.CustomerId,
                            customer.CustomerName,
                            customer.Email,
                            customer.PhoneNumber,
                            customer.Address,
                            customer.Username,
                            customer.Avatar,
                            customer.Active
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Log lỗi chi tiết
                    System.Diagnostics.Debug.WriteLine($"Facebook Login Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    
                    return StatusCode(500, new
                    {
                        message = "Lỗi khi xác thực với Facebook",
                        error = ex.Message,
                        details = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi đăng nhập bằng Facebook",
                    error = ex.Message
                });
            }
        }

        public class GoogleUserInfo
        {
            public string? Email { get; set; }
            public string? Name { get; set; }
            public string? Picture { get; set; }
        }

        public class FacebookLoginRequest
        {
            public string AccessToken { get; set; } = string.Empty;
        }

        public class FacebookUserInfo
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Email { get; set; }
            public FacebookPicture? Picture { get; set; }
        }

        public class FacebookPicture
        {
            public FacebookPictureData? Data { get; set; }
        }

        public class FacebookPictureData
        {
            public string? Url { get; set; }
        }
    }
}