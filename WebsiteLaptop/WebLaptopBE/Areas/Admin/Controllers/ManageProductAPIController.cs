using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/products")]
    [ApiController]
    public class ManageProductAPIController : ControllerBase
    {
        private readonly Testlaptop30Context _context;
        private readonly IWebHostEnvironment _environment;

        public ManageProductAPIController(Testlaptop30Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/admin/products
        // Lấy danh sách sản phẩm có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductDTO>>> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? brandId = null,
            [FromQuery] bool? active = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // Giới hạn tối đa

                var query = _context.Products
                    .Include(p => p.Brand)
                    .AsQueryable();

                // Tìm kiếm theo tên hoặc model
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(p => 
                        p.ProductName!.ToLower().Contains(searchTerm) || 
                        p.ProductModel!.ToLower().Contains(searchTerm) ||
                        p.ProductId.ToLower().Contains(searchTerm));
                }

                // Lọc theo thương hiệu
                if (!string.IsNullOrWhiteSpace(brandId))
                {
                    query = query.Where(p => p.BrandId == brandId);
                }

                // Lọc theo trạng thái active
                if (active.HasValue)
                {
                    query = query.Where(p => p.Active == active.Value);
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var products = await query
                    .OrderByDescending(p => p.ProductId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        ProductModel = p.ProductModel,
                        WarrantyPeriod = p.WarrantyPeriod,
                        OriginalSellingPrice = p.OriginalSellingPrice,
                        SellingPrice = p.SellingPrice,
                        Screen = p.Screen,
                        Camera = p.Camera,
                        Connect = p.Connect,
                        Weight = p.Weight,
                        Pin = p.Pin,
                        BrandId = p.BrandId,
                        BrandName = p.Brand != null ? p.Brand.BrandName : null,
                        Avatar = p.Avatar,
                        Active = p.Active
                    })
                    .ToListAsync();

                var result = new PagedResult<ProductDTO>
                {
                    Items = products,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sản phẩm", error = ex.Message });
            }
        }

        // GET: api/admin/products/next-id
        // Lấy ProductId tiếp theo (tự động generate)
        [HttpGet("next-id")]
        public async Task<ActionResult> GetNextProductId()
        {
            try
            {
                var nextId = await GenerateNextProductIdAsync();
                return Ok(new { productId = nextId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo ProductId", error = ex.Message });
            }
        }

        // Helper method để generate ProductId
        private async Task<string> GenerateNextProductIdAsync()
        {
            var lastProduct = await _context.Products
                .Where(p => p.ProductId.StartsWith("P") && p.ProductId.Length >= 2)
                .OrderByDescending(p => p.ProductId)
                .FirstOrDefaultAsync();

            if (lastProduct == null)
            {
                return "P001";
            }

            var lastId = lastProduct.ProductId;
            if (lastId.StartsWith("P") && lastId.Length > 1)
            {
                var numberPart = lastId.Substring(1);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    return $"P{lastNumber + 1:D3}"; // Format với 3 chữ số (P001, P002, ...)
                }
            }
            
            return "P001";
        }

        // GET: api/admin/products/{id}
        // Lấy chi tiết một sản phẩm
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(string id)
        {
            try
            {
                // Load product với các navigation properties
                var product = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.ProductConfigurations)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                // Load configurations trực tiếp từ database
                var configurations = await _context.ProductConfigurations
                    .Where(c => c.ProductId == id)
                    .ToListAsync();

                // Load images trực tiếp từ database
                var images = await _context.ProductImages
                    .Where(img => img.ProductId == id)
                    .ToListAsync();

                var productDTO = new ProductDTO
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ProductModel = product.ProductModel,
                    WarrantyPeriod = product.WarrantyPeriod,
                    OriginalSellingPrice = product.OriginalSellingPrice,
                    SellingPrice = product.SellingPrice,
                    Screen = product.Screen,
                    Camera = product.Camera,
                    Connect = product.Connect,
                    Weight = product.Weight,
                    Pin = product.Pin,
                    BrandId = product.BrandId,
                    BrandName = product.Brand != null ? product.Brand.BrandName : null,
                    Avatar = product.Avatar,
                    Active = product.Active,
                    Configurations = configurations.Select(c => new ProductConfigurationDTO
                    {
                        ConfigurationId = c.ConfigurationId,
                        Cpu = c.Cpu,
                        Ram = c.Ram,
                        Rom = c.Rom,
                        Card = c.Card,
                        Price = c.Price,
                        Quantity = c.Quantity,
                        ProductId = c.ProductId
                    }).ToList(),
                    Images = images.Select(img => new ProductImageDTO
                    {
                        ImageId = img.ImageId,
                        ProductId = img.ProductId,
                        ImageUrl = img.ImageId // Sử dụng ImageId làm tên file
                    }).ToList()
                };

                return Ok(productDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin sản phẩm", error = ex.Message });
            }
        }

        // POST: api/admin/products
        // Tạo mới sản phẩm
        [HttpPost]
        public async Task<ActionResult<ProductDTO>> CreateProduct([FromForm] ProductCreateDTO dto)
        {
            try
            {
                // Kiểm tra Request.Form trực tiếp và override DTO nếu cần (fix model binding issue)
                if (Request.Form.ContainsKey("ConfigurationsJson"))
                {
                    var configJsonFromForm = Request.Form["ConfigurationsJson"].ToString();
                    // Override DTO nếu Request.Form có giá trị (kể cả khi DTO đã có, để đảm bảo nhận được giá trị đúng)
                    if (!string.IsNullOrEmpty(configJsonFromForm))
                    {
                        dto.ConfigurationsJson = configJsonFromForm;
                    }
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Nếu không có ProductId hoặc rỗng, tự động generate
                if (string.IsNullOrWhiteSpace(dto.ProductId))
                {
                    dto.ProductId = await GenerateNextProductIdAsync();
                }

                // Kiểm tra sản phẩm đã tồn tại
                if (await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId))
                {
                    return Conflict(new { message = "Mã sản phẩm đã tồn tại" });
                }

                // Kiểm tra thương hiệu có tồn tại không
                if (!string.IsNullOrEmpty(dto.BrandId))
                {
                    var brandExists = await _context.Brands.AnyAsync(b => b.BrandId == dto.BrandId);
                    if (!brandExists)
                    {
                        return BadRequest(new { message = "Thương hiệu không tồn tại" });
                    }
                }

                // Xử lý upload ảnh
                string? avatarFileName = null;
                if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
                {
                    try
                    {
                        avatarFileName = await SaveImageAsync(dto.AvatarFile);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { message = "Lỗi khi upload ảnh đại diện", error = ex.Message });
                    }
                }

                var product = new Product
                {
                    ProductId = dto.ProductId,
                    ProductName = dto.ProductName,
                    ProductModel = dto.ProductModel,
                    WarrantyPeriod = dto.WarrantyPeriod,
                    OriginalSellingPrice = dto.OriginalSellingPrice,
                    SellingPrice = dto.SellingPrice,
                    Screen = dto.Screen,
                    Camera = dto.Camera,
                    Connect = dto.Connect,
                    Weight = dto.Weight,
                    Pin = dto.Pin,
                    BrandId = dto.BrandId,
                    Avatar = avatarFileName,
                    Active = true // Mặc định sản phẩm mới tạo sẽ active
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Xử lý Configurations
                // Nếu ConfigurationsJson được gửi, xử lý configurations
                if (!string.IsNullOrEmpty(dto.ConfigurationsJson))
                {
                    try
                    {
                        // Sử dụng JsonSerializerOptions với PropertyNameCaseInsensitive để match camelCase từ frontend
                        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var configurations = JsonSerializer.Deserialize<List<ProductConfigurationCreateDTO>>(dto.ConfigurationsJson, jsonOptions);
                        
                        if (configurations != null && configurations.Count > 0)
                        {
                            // Lọc bỏ configurations có tất cả các trường đều rỗng
                            var validConfigurations = configurations
                                .Where(c => !string.IsNullOrWhiteSpace(c.Cpu) || 
                                           !string.IsNullOrWhiteSpace(c.Ram) || 
                                           !string.IsNullOrWhiteSpace(c.Rom) || 
                                           !string.IsNullOrWhiteSpace(c.Card) ||
                                           (c.Price.HasValue && c.Price.Value > 0) ||
                                           (c.Quantity.HasValue && c.Quantity.Value > 0))
                                .ToList();

                            if (validConfigurations.Count > 0)
                            {
                                var configIdGenerator = await CreateConfigurationIdGeneratorAsync();
                                foreach (var configDto in validConfigurations)
                                {
                                    _context.ProductConfigurations.Add(new ProductConfiguration
                                    {
                                        ConfigurationId = configIdGenerator.Next(),
                                        ProductId = product.ProductId,
                                        Cpu = configDto.Cpu?.Trim() ?? string.Empty,
                                        Ram = configDto.Ram?.Trim() ?? string.Empty,
                                        Rom = configDto.Rom?.Trim() ?? string.Empty,
                                        Card = configDto.Card?.Trim() ?? string.Empty,
                                        Price = configDto.Price ?? 0,
                                        Quantity = configDto.Quantity ?? 0
                                    });
                                }
                                
                                // Save configurations ngay sau khi thêm
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"Error parsing configurations JSON: {jsonEx.Message}");
                        Console.WriteLine($"ConfigurationsJson: {dto.ConfigurationsJson}");
                        // Không throw để không fail toàn bộ request
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving configurations: {ex.Message}");
                        Console.WriteLine($"ConfigurationsJson: {dto.ConfigurationsJson}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        // Không throw để không fail toàn bộ request
                    }
                }
                else
                {
                    // Log để debug nếu ConfigurationsJson không được gửi
                    Console.WriteLine("CreateProduct: ConfigurationsJson is null or empty");
                }

                // Xử lý Product Images
                try
                {
                    var imageFiles = Request.Form.Files.Where(f => f.Name == "ImageFiles").ToList();
                    if (imageFiles != null && imageFiles.Count > 0)
                    {
                        foreach (var imageFile in imageFiles)
                        {
                            if (imageFile != null && imageFile.Length > 0)
                            {
                                try
                                {
                                    // Validate file type
                                    if (!imageFile.ContentType.StartsWith("image/"))
                                    {
                                        continue;
                                    }

                                    var imageFileName = await SaveImageAsync(imageFile);
                                    _context.ProductImages.Add(new ProductImage
                                    {
                                        ImageId = imageFileName, // Lưu tên file vào ImageId
                                        ProductId = product.ProductId
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error processing image file {imageFile.FileName}: {ex.Message}");
                                    // Tiếp tục xử lý các file khác
                                }
                            }
                        }
                        if (_context.ChangeTracker.HasChanges())
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving product images: {ex.Message}");
                    // Không throw để không fail toàn bộ request
                }

                // Load lại product với đầy đủ thông tin
                var result = await GetProductByIdAsync(product.ProductId);
                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating product: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi tạo sản phẩm", error = ex.Message });
            }
        }

        // PUT: api/admin/products/{id}
        // Cập nhật sản phẩm
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromForm] ProductUpdateDTO dto)
        {
            try
            {
                // Kiểm tra Request.Form trực tiếp và override DTO nếu cần (fix model binding issue)
                if (Request.Form.ContainsKey("ConfigurationsJson") && string.IsNullOrEmpty(dto.ConfigurationsJson))
                {
                    dto.ConfigurationsJson = Request.Form["ConfigurationsJson"].ToString();
                }
                
                if (Request.Form.ContainsKey("ConfigurationsToDeleteJson") && string.IsNullOrEmpty(dto.ConfigurationsToDeleteJson))
                {
                    dto.ConfigurationsToDeleteJson = Request.Form["ConfigurationsToDeleteJson"].ToString();
                }
                
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                // Kiểm tra thương hiệu có tồn tại không
                if (!string.IsNullOrEmpty(dto.BrandId))
                {
                    var brandExists = await _context.Brands.AnyAsync(b => b.BrandId == dto.BrandId);
                    if (!brandExists)
                    {
                        return BadRequest(new { message = "Thương hiệu không tồn tại" });
                    }
                }

                // Xử lý upload ảnh mới
                if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(product.Avatar))
                    {
                        DeleteImage(product.Avatar);
                    }

                    product.Avatar = await SaveImageAsync(dto.AvatarFile);
                }

                // Cập nhật thông tin
                product.ProductName = dto.ProductName;
                product.ProductModel = dto.ProductModel;
                product.WarrantyPeriod = dto.WarrantyPeriod;
                product.OriginalSellingPrice = dto.OriginalSellingPrice;
                product.SellingPrice = dto.SellingPrice;
                product.Screen = dto.Screen;
                product.Camera = dto.Camera;
                product.Connect = dto.Connect;
                product.Weight = dto.Weight;
                product.Pin = dto.Pin;
                product.BrandId = dto.BrandId;

                await _context.SaveChangesAsync();

                // Xử lý xóa Configurations (tương tự như xóa Images)
                if (!string.IsNullOrEmpty(dto.ConfigurationsToDeleteJson))
                {
                    try
                    {
                        var configurationIdsToDelete = JsonSerializer.Deserialize<List<string>>(dto.ConfigurationsToDeleteJson);
                        if (configurationIdsToDelete != null && configurationIdsToDelete.Count > 0)
                        {
                            var configsToDelete = await _context.ProductConfigurations
                                .Where(c => c.ProductId == id && configurationIdsToDelete.Contains(c.ConfigurationId))
                                .ToListAsync();

                            if (configsToDelete.Any())
                            {
                                _context.ProductConfigurations.RemoveRange(configsToDelete);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting configurations: {ex.Message}");
                    }
                }

                // Xử lý thêm/Update Configurations (tương tự như thêm Images)
                if (!string.IsNullOrEmpty(dto.ConfigurationsJson))
                {
                    try
                    {
                        // Sử dụng JsonSerializerOptions với PropertyNameCaseInsensitive để match camelCase từ frontend
                        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var configurations = JsonSerializer.Deserialize<List<ProductConfigurationUpdateDTO>>(dto.ConfigurationsJson, jsonOptions);
                        
                        if (configurations != null && configurations.Count > 0)
                        {
                            // Lọc bỏ configurations có tất cả các trường đều rỗng
                            var validConfigurations = configurations
                                .Where(c => !string.IsNullOrWhiteSpace(c.Cpu) || 
                                           !string.IsNullOrWhiteSpace(c.Ram) || 
                                           !string.IsNullOrWhiteSpace(c.Rom) || 
                                           !string.IsNullOrWhiteSpace(c.Card) ||
                                           (c.Price.HasValue && c.Price.Value > 0) ||
                                           (c.Quantity.HasValue && c.Quantity.Value > 0))
                                .ToList();

                            if (validConfigurations.Count > 0)
                            {
                                ConfigurationIdGenerator? configIdGenerator = null;
                                if (validConfigurations.Any(c => string.IsNullOrWhiteSpace(c.ConfigurationId)))
                                {
                                    configIdGenerator = await CreateConfigurationIdGeneratorAsync();
                                }
                                
                                // Xử lý từng configuration
                                foreach (var configDto in validConfigurations)
                                {
                                    if (!string.IsNullOrWhiteSpace(configDto.ConfigurationId))
                                    {
                                        // Update existing
                                        var existingConfig = await _context.ProductConfigurations
                                            .FirstOrDefaultAsync(c => c.ProductId == id && c.ConfigurationId == configDto.ConfigurationId);
                                        
                                        if (existingConfig != null)
                                        {
                                            existingConfig.Cpu = configDto.Cpu?.Trim() ?? string.Empty;
                                            existingConfig.Ram = configDto.Ram?.Trim() ?? string.Empty;
                                            existingConfig.Rom = configDto.Rom?.Trim() ?? string.Empty;
                                            existingConfig.Card = configDto.Card?.Trim() ?? string.Empty;
                                            existingConfig.Price = configDto.Price ?? 0;
                                            existingConfig.Quantity = configDto.Quantity ?? 0;
                                            _context.ProductConfigurations.Update(existingConfig);
                                        }
                                        else
                                        {
                                            // Nếu không tìm thấy, tạo mới
                                            configIdGenerator ??= await CreateConfigurationIdGeneratorAsync();
                                            _context.ProductConfigurations.Add(new ProductConfiguration
                                            {
                                                ConfigurationId = configIdGenerator.Next(),
                                                ProductId = id,
                                                Cpu = configDto.Cpu?.Trim() ?? string.Empty,
                                                Ram = configDto.Ram?.Trim() ?? string.Empty,
                                                Rom = configDto.Rom?.Trim() ?? string.Empty,
                                                Card = configDto.Card?.Trim() ?? string.Empty,
                                                Price = configDto.Price ?? 0,
                                                Quantity = configDto.Quantity ?? 0
                                            });
                                        }
                                    }
                                    else
                                    {
                                        // Create new
                                        configIdGenerator ??= await CreateConfigurationIdGeneratorAsync();
                                        _context.ProductConfigurations.Add(new ProductConfiguration
                                        {
                                            ConfigurationId = configIdGenerator.Next(),
                                            ProductId = id,
                                            Cpu = configDto.Cpu?.Trim() ?? string.Empty,
                                            Ram = configDto.Ram?.Trim() ?? string.Empty,
                                            Rom = configDto.Rom?.Trim() ?? string.Empty,
                                            Card = configDto.Card?.Trim() ?? string.Empty,
                                            Price = configDto.Price ?? 0,
                                            Quantity = configDto.Quantity ?? 0
                                        });
                                    }
                                }

                                if (_context.ChangeTracker.HasChanges())
                                {
                                    await _context.SaveChangesAsync();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing configurations: {ex.Message}");
                        // Không throw để không fail toàn bộ request
                    }
                }

                // Xử lý xóa Images
                if (!string.IsNullOrEmpty(dto.ImagesToDeleteJson))
                {
                    try
                    {
                        var imageIdsToDelete = JsonSerializer.Deserialize<List<string>>(dto.ImagesToDeleteJson);
                        if (imageIdsToDelete != null && imageIdsToDelete.Count > 0)
                        {
                            var imagesToDelete = await _context.ProductImages
                                .Where(img => img.ProductId == id && imageIdsToDelete.Contains(img.ImageId))
                                .ToListAsync();

                            foreach (var img in imagesToDelete)
                            {
                                DeleteImage(img.ImageId); // Xóa file ảnh
                            }

                            _context.ProductImages.RemoveRange(imagesToDelete);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting images: {ex.Message}");
                    }
                }

                // Xử lý thêm Images mới
                var imageFiles = Request.Form.Files.Where(f => f.Name == "ImageFiles").ToList();
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    foreach (var imageFile in imageFiles)
                    {
                        if (imageFile.Length > 0)
                        {
                            try
                            {
                                var imageFileName = await SaveImageAsync(imageFile);
                                _context.ProductImages.Add(new ProductImage
                                {
                                    ImageId = imageFileName, // Lưu tên file vào ImageId
                                    ProductId = id
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing image file {imageFile.FileName}: {ex.Message}");
                                // Tiếp tục xử lý các file khác
                            }
                        }
                    }
                    if (_context.ChangeTracker.HasChanges())
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { message = "Cập nhật sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật sản phẩm", error = ex.Message });
            }
        }

        // DELETE: api/admin/products/{id}
        // Ẩn sản phẩm (set active = 0) thay vì xóa thực sự
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                // Set active = 0 thay vì xóa
                product.Active = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã ẩn sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding product: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi ẩn sản phẩm", error = ex.Message });
            }
        }

        // POST: api/admin/products/{id}/restore
        // Khôi phục sản phẩm (set active = 1)
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreProduct(string id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                // Set active = 1
                product.Active = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Khôi phục sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring product: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi khôi phục sản phẩm", error = ex.Message });
            }
        }

        // GET: api/admin/products/brands
        // Lấy danh sách thương hiệu (để dropdown)
        [HttpGet("brands")]
        public async Task<ActionResult<List<Brand>>> GetBrands()
        {
            try
            {
                var brands = await _context.Brands
                    .OrderBy(b => b.BrandName)
                    .ToListAsync();
                return Ok(brands);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách thương hiệu", error = ex.Message });
            }
        }

        // Helper method để load product với đầy đủ thông tin
        private async Task<ProductDTO> GetProductByIdAsync(string productId)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return null!;
            }

            // Load configurations trực tiếp từ database
            var configurations = await _context.ProductConfigurations
                .Where(c => c.ProductId == productId)
                .ToListAsync();

            // Load images trực tiếp từ database
            var images = await _context.ProductImages
                .Where(img => img.ProductId == productId)
                .ToListAsync();

            return new ProductDTO
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ProductModel = product.ProductModel,
                WarrantyPeriod = product.WarrantyPeriod,
                OriginalSellingPrice = product.OriginalSellingPrice,
                SellingPrice = product.SellingPrice,
                Screen = product.Screen,
                Camera = product.Camera,
                Connect = product.Connect,
                Weight = product.Weight,
                Pin = product.Pin,
                BrandId = product.BrandId,
                BrandName = product.Brand != null ? product.Brand.BrandName : null,
                Avatar = product.Avatar,
                Active = product.Active,
                Configurations = configurations.Select(c => new ProductConfigurationDTO
                {
                    ConfigurationId = c.ConfigurationId,
                    Cpu = c.Cpu,
                    Ram = c.Ram,
                    Rom = c.Rom,
                    Card = c.Card,
                    Price = c.Price,
                    Quantity = c.Quantity,
                    ProductId = c.ProductId
                }).ToList(),
                Images = images.Select(img => new ProductImageDTO
                {
                    ImageId = img.ImageId,
                    ProductId = img.ProductId,
                    ImageUrl = img.ImageId // Sử dụng ImageId làm tên file
                }).ToList()
            };
        }

        private async Task<ConfigurationIdGenerator> CreateConfigurationIdGeneratorAsync()
        {
            var existingIds = await _context.ProductConfigurations
                .AsNoTracking()
                .Select(c => c.ConfigurationId)
                .ToListAsync();

            var normalizedIds = existingIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim().ToUpperInvariant())
                .ToList();

            var maxSequence = 0;
            foreach (var id in normalizedIds)
            {
                if (id.StartsWith("CF") && id.Length > 2)
                {
                    var numberPart = id.Substring(2);
                    if (int.TryParse(numberPart, out var number) && number > maxSequence)
                    {
                        maxSequence = number;
                    }
                }
            }

            return new ConfigurationIdGenerator(normalizedIds, maxSequence + 1);
        }

        private sealed class ConfigurationIdGenerator
        {
            private readonly HashSet<string> _allocatedIds;
            private int _nextSequence;

            public ConfigurationIdGenerator(IEnumerable<string> existingIds, int startSequence)
            {
                _allocatedIds = new HashSet<string>(
                    existingIds.Where(id => !string.IsNullOrWhiteSpace(id))
                               .Select(id => id.Trim().ToUpperInvariant()),
                    StringComparer.OrdinalIgnoreCase);

                _nextSequence = startSequence < 1 ? 1 : startSequence;
            }

            public string Next()
            {
                string candidate;
                do
                {
                    candidate = $"CF{_nextSequence:D3}";
                    _nextSequence++;
                } while (_allocatedIds.Contains(candidate));

                _allocatedIds.Add(candidate);
                return candidate;
            }
        }

        // Hàm hỗ trợ lưu ảnh
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is null or empty");
                }

                // Validate file type
                if (string.IsNullOrEmpty(file.ContentType) || !file.ContentType.StartsWith("image/"))
                {
                    throw new ArgumentException($"Invalid file type: {file.ContentType}. Only image files are allowed.");
                }

                // Validate file size (max 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    throw new ArgumentException($"File size ({file.Length} bytes) exceeds maximum allowed size ({maxFileSize} bytes)");
                }

                // Tạo tên file unique nhưng TUÂN THỦ GIỚI HẠN cột DB:
                // - ProductImage.ImageId tối đa 20 ký tự (xem cấu hình model)
                // => Luôn tạo tên file với tổng độ dài <= 20 để tránh lỗi khi lưu ảnh sản phẩm
                var fileExtension = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    // Nếu không có extension, thử lấy từ content type
                    fileExtension = file.ContentType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/gif" => ".gif",
                        "image/webp" => ".webp",
                        _ => ".jpg" // Default
                    };
                }

                // Tính toán độ dài tối đa cho phần baseName để tổng độ dài (base + ext) <= 20
                const int maxTotalLength = 20;
                var extLength = fileExtension.Length;
                var baseNameMaxLength = Math.Max(1, maxTotalLength - extLength);

                // Tạo baseName ngẫu nhiên có độ dài phù hợp
                string GenerateRandomBaseName(int length)
                {
                    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    var rnd = new Random();
                    var buffer = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        buffer[i] = chars[rnd.Next(chars.Length)];
                    }
                    return new string(buffer);
                }

                var baseName = GenerateRandomBaseName(baseNameMaxLength);
                var fileName = $"{baseName}{fileExtension}";
                
                // Đường dẫn thư mục lưu ảnh
                var webRootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }
                var uploadsFolder = Path.Combine(webRootPath, "image");
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                // Lưu file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu ảnh: {ex.Message}", ex);
            }
        }

        // Hàm hỗ trợ xóa ảnh
        private void DeleteImage(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "image", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch
            {
                // Không throw exception nếu xóa file thất bại
            }
        }
    }
}
