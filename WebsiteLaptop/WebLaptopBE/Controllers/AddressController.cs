using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebLaptopBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl = "https://provinces.open-api.vn/api";

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
                
                // API provinces.open-api.vn: endpoint đúng là /api/p/
                var response = await client.GetAsync($"{_baseUrl}/p/");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Không thể tải danh sách tỉnh/thành phố", error = errorContent });
                }

                var json = await response.Content.ReadAsStringAsync();
                
                // Kiểm tra nếu response là mảng JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Ok(new List<object>());
                }
                
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                
                // Nếu là mảng, trả về trực tiếp
                if (data.ValueKind == JsonValueKind.Array)
                {
                    return Ok(data);
                }
                
                return Ok(data);
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
                
                // API provinces.open-api.vn: lấy thông tin tỉnh với depth=3 để có tất cả wards từ tất cả districts
                // Sau sáp nhập, chỉ còn 2 cấp: Tỉnh/Thành phố → Phường/Xã
                var response = await client.GetAsync($"{_baseUrl}/p/{provinceCode}?depth=3");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Không thể tải danh sách phường/xã", error = errorContent });
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Ok(new List<object>());
                }
                
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                
                // Thu thập tất cả wards từ tất cả districts
                var allWards = new List<JsonElement>();
                
                if (data.TryGetProperty("districts", out var districts) && districts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var district in districts.EnumerateArray())
                    {
                        if (district.TryGetProperty("wards", out var wards) && wards.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var ward in wards.EnumerateArray())
                            {
                                allWards.Add(ward);
                            }
                        }
                    }
                }
                
                // Trả về tất cả wards dưới dạng mảng
                if (allWards.Count > 0)
                {
                    var wardsArray = JsonSerializer.Serialize(allWards);
                    return Ok(JsonSerializer.Deserialize<JsonElement>(wardsArray));
                }
                
                // Nếu không có wards, trả về mảng rỗng
                return Ok(new List<object>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tải danh sách phường/xã", error = ex.Message });
            }
        }
    }
}

