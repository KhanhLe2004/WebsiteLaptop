using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebLaptopBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl = "https://production.cas.so/address-kit/2025-07-01";

        public AddressController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                // Sử dụng endpoint giống ManageEmployeeAPIController
                var response = await client.GetAsync($"{_baseUrl}/provinces");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Không thể tải danh sách tỉnh/thành phố", error = errorContent });
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return Ok(new List<object>());
                }
                
                // Sử dụng JsonDocument.Parse giống ManageEmployeeAPIController
                var jsonDoc = JsonDocument.Parse(jsonString);
                var provinces = new List<object>();
                
                // Kiểm tra property "provinces" trong response
                if (jsonDoc.RootElement.TryGetProperty("provinces", out var provincesElement) && provincesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var province in provincesElement.EnumerateArray())
                    {
                        provinces.Add(new
                        {
                            code = province.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "",
                            name = province.TryGetProperty("name", out var name) ? name.GetString() : null
                        });
                    }
                }
                // Nếu response là mảng trực tiếp
                else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var province in jsonDoc.RootElement.EnumerateArray())
                    {
                        provinces.Add(new
                        {
                            code = province.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "",
                            name = province.TryGetProperty("name", out var name) ? name.GetString() : null
                        });
                    }
                }
                
                return Ok(provinces);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải danh sách tỉnh/thành phố", error = ex.Message });
            }
        }

        [HttpGet("wards/{provinceCode}")]
        public async Task<IActionResult> GetWards(string provinceCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(provinceCode))
                {
                    return BadRequest(new { message = "Mã tỉnh/thành phố không hợp lệ" });
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                // Sử dụng endpoint giống ManageEmployeeAPIController
                var response = await client.GetAsync($"{_baseUrl}/provinces/{provinceCode}/communes");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Không thể tải danh sách phường/xã", error = errorContent });
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return Ok(new List<object>());
                }
                
                // Sử dụng JsonDocument.Parse giống ManageEmployeeAPIController
                var jsonDoc = JsonDocument.Parse(jsonString);
                var communes = new List<object>();
                
                // Kiểm tra property "communes" trong response
                if (jsonDoc.RootElement.TryGetProperty("communes", out var communesElement) && communesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var commune in communesElement.EnumerateArray())
                    {
                        communes.Add(new
                        {
                            code = commune.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "",
                            name = commune.TryGetProperty("name", out var name) ? name.GetString() : null
                        });
                    }
                }
                // Nếu response là mảng trực tiếp
                else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var commune in jsonDoc.RootElement.EnumerateArray())
                    {
                        communes.Add(new
                        {
                            code = commune.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "",
                            name = commune.TryGetProperty("name", out var name) ? name.GetString() : null
                        });
                    }
                }
                
                return Ok(communes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải danh sách phường/xã", error = ex.Message });
            }
        }
    }
}


