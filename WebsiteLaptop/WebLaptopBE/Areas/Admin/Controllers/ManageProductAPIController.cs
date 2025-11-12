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
        private readonly Testlaptop20Context _context;
        private readonly IWebHostEnvironment _environment;

        public ManageProductAPIController(Testlaptop20Context context, IWebHostEnvironment environment)
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
            [FromQuery] string? brandId = null)
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
                        Avatar = p.Avatar
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
                // Lấy tất cả productId hiện có, sắp xếp theo thứ tự giảm dần
                var lastProduct = await _context.Products
                    .Where(p => p.ProductId.StartsWith("P") && p.ProductId.Length >= 2)
                    .OrderByDescending(p => p.ProductId)
                    .FirstOrDefaultAsync();

                string nextId;
                if (lastProduct == null)
                {
                    // Nếu chưa có sản phẩm nào, bắt đầu từ P001
                    nextId = "P001";
                }
                else
                {
                    // Lấy số từ productId (ví dụ: P040 -> 40)
                    var lastId = lastProduct.ProductId;
                    if (lastId.StartsWith("P") && lastId.Length > 1)
                    {
                        var numberPart = lastId.Substring(1);
                        if (int.TryParse(numberPart, out int lastNumber))
                        {
                            var nextNumber = lastNumber + 1;
                            nextId = $"P{nextNumber:D3}"; // Format với 3 chữ số (P001, P002, ...)
                        }
                        else
                        {
                            // Nếu không parse được, bắt đầu từ P001
                            nextId = "P001";
                        }
                    }
                    else
                    {
                        nextId = "P001";
                    }
                }

                return Ok(new { productId = nextId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo ProductId", error = ex.Message });
            }
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
                    Configurations = configurations.Select(c => new ProductConfigurationDTO
                    {
                        ConfigurationId = c.ConfigurationId,
                        Specifications = c.Specifications,
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
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Nếu không có ProductId hoặc rỗng, tự động generate
                if (string.IsNullOrWhiteSpace(dto.ProductId))
                {
                    var lastProduct = await _context.Products
                        .Where(p => p.ProductId.StartsWith("P") && p.ProductId.Length >= 2)
                        .OrderByDescending(p => p.ProductId)
                        .FirstOrDefaultAsync();

                    if (lastProduct == null)
                    {
                        dto.ProductId = "P001";
                    }
                    else
                    {
                        var lastId = lastProduct.ProductId;
                        if (lastId.StartsWith("P") && lastId.Length > 1)
                        {
                            var numberPart = lastId.Substring(1);
                            if (int.TryParse(numberPart, out int lastNumber))
                            {
                                var nextNumber = lastNumber + 1;
                                dto.ProductId = $"P{nextNumber:D3}";
                            }
                            else
                            {
                                dto.ProductId = "P001";
                            }
                        }
                        else
                        {
                            dto.ProductId = "P001";
                        }
                    }
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
                        Console.WriteLine($"Avatar saved: {avatarFileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving avatar: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        return StatusCode(500, new { message = "Lỗi khi upload ảnh đại diện", error = ex.Message, details = ex.ToString() });
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
                    Avatar = avatarFileName
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Xử lý Configurations
                // Nếu ConfigurationsJson được gửi, xử lý configurations
                if (!string.IsNullOrEmpty(dto.ConfigurationsJson))
                {
                    try
                    {
                        var configurations = JsonSerializer.Deserialize<List<ProductConfigurationCreateDTO>>(dto.ConfigurationsJson);
                        if (configurations != null && configurations.Count > 0)
                        {
                            // Lọc bỏ configurations có Specifications rỗng
                            var validConfigurations = configurations
                                .Where(c => !string.IsNullOrWhiteSpace(c.Specifications))
                                .ToList();

                            if (validConfigurations.Count > 0)
                            {
                                var configIdGenerator = await CreateConfigurationIdGeneratorAsync();
                                foreach (var configDto in validConfigurations)
                                {
                                    var configuration = new ProductConfiguration
                                    {
                                        ConfigurationId = configIdGenerator.Next(),
                                        ProductId = product.ProductId,
                                        Specifications = configDto.Specifications?.Trim() ?? string.Empty,
                                        Price = configDto.Price ?? 0,
                                        Quantity = configDto.Quantity ?? 0
                                    };
                                    _context.ProductConfigurations.Add(configuration);
                                }
                                
                                if (_context.ChangeTracker.HasChanges())
                                {
                                    await _context.SaveChangesAsync();
                                    Console.WriteLine($"Successfully created {validConfigurations.Count} configurations for product {product.ProductId}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No valid configurations to create (all have empty specifications)");
                            }
                        }
                        else
                        {
                            Console.WriteLine("ConfigurationsJson is empty or null, no configurations created");
                        }
                    }
                    catch (JsonException ex)
                    {
                        // Log error nhưng không fail request
                        Console.WriteLine($"Error parsing configurations JSON: {ex.Message}");
                        Console.WriteLine($"ConfigurationsJson: {dto.ConfigurationsJson}");
                        // Không throw exception để không fail toàn bộ request
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving configurations: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        // Không throw để không fail toàn bộ request
                    }
                }
                else
                {
                    // Nếu không có ConfigurationsJson, không tạo configurations (sản phẩm có thể không có cấu hình)
                    Console.WriteLine("ConfigurationsJson not provided, no configurations created");
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
                                        Console.WriteLine($"Warning: Skipping non-image file: {imageFile.FileName}");
                                        continue;
                                    }

                                    var imageFileName = await SaveImageAsync(imageFile);
                                    
                                    var productImage = new ProductImage
                                    {
                                        ImageId = imageFileName, // Lưu tên file vào ImageId
                                        ProductId = product.ProductId
                                    };
                                    _context.ProductImages.Add(productImage);
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
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Không throw để không fail toàn bộ request
                }

                // Load lại product với đầy đủ thông tin
                var result = await GetProductByIdAsync(product.ProductId);
                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating product: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Lỗi khi tạo sản phẩm", error = ex.Message, details = ex.ToString() });
            }
        }

        // PUT: api/admin/products/{id}
        // Cập nhật sản phẩm
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromForm] ProductUpdateDTO dto)
        {
            try
            {
                // Log dữ liệu nhận được
                Console.WriteLine($"=== UpdateProduct called for product ID: {id} ===");
                Console.WriteLine($"ProductName: {dto.ProductName}");
                Console.WriteLine($"ConfigurationsJson from DTO: {dto.ConfigurationsJson ?? "NULL"}");
                Console.WriteLine($"ConfigurationsToDeleteJson from DTO: {dto.ConfigurationsToDeleteJson ?? "NULL"}");
                
                // Kiểm tra Request.Form trực tiếp và override DTO nếu cần
                if (Request.Form.ContainsKey("ConfigurationsJson"))
                {
                    var configJsonFromForm = Request.Form["ConfigurationsJson"].ToString();
                    Console.WriteLine($"ConfigurationsJson from Request.Form: {configJsonFromForm}");
                    // Override DTO nếu Request.Form có giá trị nhưng DTO không có
                    if (string.IsNullOrEmpty(dto.ConfigurationsJson) && !string.IsNullOrEmpty(configJsonFromForm))
                    {
                        dto.ConfigurationsJson = configJsonFromForm;
                        Console.WriteLine("Overrode DTO.ConfigurationsJson from Request.Form");
                    }
                }
                else
                {
                    Console.WriteLine("ConfigurationsJson NOT found in Request.Form");
                }
                
                if (Request.Form.ContainsKey("ConfigurationsToDeleteJson"))
                {
                    var configsToDeleteFromForm = Request.Form["ConfigurationsToDeleteJson"].ToString();
                    Console.WriteLine($"ConfigurationsToDeleteJson from Request.Form: {configsToDeleteFromForm}");
                    // Override DTO nếu Request.Form có giá trị nhưng DTO không có
                    if (string.IsNullOrEmpty(dto.ConfigurationsToDeleteJson) && !string.IsNullOrEmpty(configsToDeleteFromForm))
                    {
                        dto.ConfigurationsToDeleteJson = configsToDeleteFromForm;
                        Console.WriteLine("Overrode DTO.ConfigurationsToDeleteJson from Request.Form");
                    }
                }
                else
                {
                    Console.WriteLine("ConfigurationsToDeleteJson NOT found in Request.Form");
                }
                
                // Log lại sau khi override
                Console.WriteLine($"After override - ConfigurationsJson: {dto.ConfigurationsJson ?? "NULL"}");
                Console.WriteLine($"After override - ConfigurationsToDeleteJson: {dto.ConfigurationsToDeleteJson ?? "NULL"}");
                
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is invalid:");
                    foreach (var error in ModelState)
                    {
                        Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                    return BadRequest(ModelState);
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {id} not found");
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
                                Console.WriteLine($"Deleting {configsToDelete.Count} configurations");
                                _context.ProductConfigurations.RemoveRange(configsToDelete);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error parsing configurations to delete JSON: {ex.Message}");
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
                        Console.WriteLine($"Received ConfigurationsJson: {dto.ConfigurationsJson}");
                        
                        // Sử dụng JsonSerializerOptions với PropertyNameCaseInsensitive để match camelCase từ frontend
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var configurations = JsonSerializer.Deserialize<List<ProductConfigurationUpdateDTO>>(dto.ConfigurationsJson, jsonOptions);
                        Console.WriteLine($"Deserialized configurations count: {configurations?.Count ?? 0}");
                        
                        if (configurations != null && configurations.Count > 0)
                        {
                            // Lọc bỏ configurations có Specifications rỗng
                            var validConfigurations = configurations
                                .Where(c => !string.IsNullOrWhiteSpace(c.Specifications))
                                .ToList();
                            
                            Console.WriteLine($"Valid configurations count: {validConfigurations.Count}");

                            if (validConfigurations.Count > 0)
                            {
                                ConfigurationIdGenerator? configIdGenerator = null;
                                if (validConfigurations.Any(c => string.IsNullOrWhiteSpace(c.ConfigurationId)))
                                {
                                    configIdGenerator = await CreateConfigurationIdGeneratorAsync();
                                    Console.WriteLine("Created ConfigurationIdGenerator for new configurations");
                                }

                                int addedCount = 0;
                                int updatedCount = 0;
                                
                                // Xử lý từng configuration
                                foreach (var configDto in validConfigurations)
                                {
                                    try
                                    {
                                        if (!string.IsNullOrWhiteSpace(configDto.ConfigurationId))
                                        {
                                            // Update existing
                                            var existingConfig = await _context.ProductConfigurations
                                                .FirstOrDefaultAsync(c => c.ProductId == id && c.ConfigurationId == configDto.ConfigurationId);
                                            
                                            if (existingConfig != null)
                                            {
                                                existingConfig.Specifications = configDto.Specifications?.Trim() ?? string.Empty;
                                                existingConfig.Price = configDto.Price ?? 0;
                                                existingConfig.Quantity = configDto.Quantity ?? 0;
                                                _context.ProductConfigurations.Update(existingConfig);
                                                updatedCount++;
                                                Console.WriteLine($"Updated configuration: {configDto.ConfigurationId}, Specs: {configDto.Specifications}, Price: {configDto.Price}, Qty: {configDto.Quantity}");
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Warning: Configuration with ID {configDto.ConfigurationId} not found for product {id}, creating new");
                                                // Nếu không tìm thấy, tạo mới
                                                configIdGenerator ??= await CreateConfigurationIdGeneratorAsync();
                                                var newConfig = new ProductConfiguration
                                                {
                                                    ConfigurationId = configIdGenerator.Next(),
                                                    ProductId = id,
                                                    Specifications = configDto.Specifications?.Trim() ?? string.Empty,
                                                    Price = configDto.Price ?? 0,
                                                    Quantity = configDto.Quantity ?? 0
                                                };
                                                _context.ProductConfigurations.Add(newConfig);
                                                addedCount++;
                                                Console.WriteLine($"Created new configuration: {newConfig.ConfigurationId}, Specs: {newConfig.Specifications}, Price: {newConfig.Price}, Qty: {newConfig.Quantity}");
                                            }
                                        }
                                        else
                                        {
                                            // Create new
                                            configIdGenerator ??= await CreateConfigurationIdGeneratorAsync();

                                            var newConfig = new ProductConfiguration
                                            {
                                                ConfigurationId = configIdGenerator.Next(),
                                                ProductId = id,
                                                Specifications = configDto.Specifications?.Trim() ?? string.Empty,
                                                Price = configDto.Price ?? 0,
                                                Quantity = configDto.Quantity ?? 0
                                            };
                                            _context.ProductConfigurations.Add(newConfig);
                                            addedCount++;
                                            Console.WriteLine($"Created new configuration: {newConfig.ConfigurationId}, Specs: {newConfig.Specifications}, Price: {newConfig.Price}, Qty: {newConfig.Quantity}");
                                        }
                                    }
                                    catch (Exception configEx)
                                    {
                                        Console.WriteLine($"Error processing individual configuration: {configEx.Message}");
                                        Console.WriteLine($"Stack trace: {configEx.StackTrace}");
                                        // Tiếp tục xử lý các configuration khác
                                    }
                                }

                                // Kiểm tra xem có changes không
                                if (_context.ChangeTracker.HasChanges())
                                {
                                    Console.WriteLine($"Saving changes for configurations. Added: {addedCount}, Updated: {updatedCount}");
                                    var savedCount = await _context.SaveChangesAsync();
                                    Console.WriteLine($"SaveChangesAsync returned: {savedCount} entities saved");
                                    Console.WriteLine($"Successfully processed {validConfigurations.Count} configurations (Added: {addedCount}, Updated: {updatedCount})");
                                }
                                else
                                {
                                    Console.WriteLine("No changes detected in ChangeTracker");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No valid configurations to process (all have empty specifications)");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Configurations list is null or empty after deserialization");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error parsing configurations JSON: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        Console.WriteLine($"ConfigurationsJson: {dto.ConfigurationsJson}");
                        // Không throw để không fail toàn bộ request
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing configurations: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        // Không throw để không fail toàn bộ request
                    }
                }
                else
                {
                    Console.WriteLine("ConfigurationsJson is null or empty");
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
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error parsing images to delete: {ex.Message}");
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
                            var imageFileName = await SaveImageAsync(imageFile);

                            var productImage = new ProductImage
                            {
                                ImageId = imageFileName, // Lưu tên file vào ImageId
                                ProductId = id
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Cập nhật sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật sản phẩm", error = ex.Message });
            }
        }

        // DELETE: api/admin/products/{id}
        // Xóa sản phẩm
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

                // Kiểm tra ràng buộc với các bảng quan trọng (không thể xóa nếu có)
                var hasCartDetails = await _context.CartDetails.AnyAsync(cd => cd.ProductId == id);
                var hasSaleInvoiceDetails = await _context.SaleInvoiceDetails.AnyAsync(sid => sid.ProductId == id);
                var hasProductSerials = await _context.ProductSerials.AnyAsync(ps => ps.ProductId == id);
                var hasStockExportDetails = await _context.StockExportDetails.AnyAsync(sed => sed.ProductId == id);
                var hasStockImportDetails = await _context.StockImportDetails.AnyAsync(sid => sid.ProductId == id);
                
                if (hasCartDetails || hasSaleInvoiceDetails || hasProductSerials || hasStockExportDetails || hasStockImportDetails)
                {
                    var reasons = new List<string>();
                    if (hasCartDetails) reasons.Add("giỏ hàng");
                    if (hasSaleInvoiceDetails) reasons.Add("hóa đơn bán hàng");
                    if (hasProductSerials) reasons.Add("sản phẩm serial");
                    if (hasStockExportDetails) reasons.Add("phiếu xuất kho");
                    if (hasStockImportDetails) reasons.Add("phiếu nhập kho");
                    
                    return BadRequest(new { message = $"Không thể xóa sản phẩm vì đã có trong {string.Join(", ", reasons)}" });
                }

                // Xóa ảnh avatar nếu có
                if (!string.IsNullOrEmpty(product.Avatar))
                {
                    DeleteImage(product.Avatar);
                }

                // Xóa Product Images
                var productImages = await _context.ProductImages
                    .Where(img => img.ProductId == id)
                    .ToListAsync();
                
                foreach (var img in productImages)
                {
                    DeleteImage(img.ImageId); // Xóa file ảnh
                }
                _context.ProductImages.RemoveRange(productImages);

                // Xóa Product Configurations
                var productConfigs = await _context.ProductConfigurations
                    .Where(c => c.ProductId == id)
                    .ToListAsync();
                _context.ProductConfigurations.RemoveRange(productConfigs);

                // Xóa Product Reviews
                var productReviews = await _context.ProductReviews
                    .Where(pr => pr.ProductId == id)
                    .ToListAsync();
                _context.ProductReviews.RemoveRange(productReviews);

                // Xóa Promotions
                var promotions = await _context.Promotions
                    .Where(p => p.ProductId == id)
                    .ToListAsync();
                _context.Promotions.RemoveRange(promotions);

                // Xóa Product
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Lỗi khi xóa sản phẩm", error = ex.Message, details = ex.ToString() });
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
                Configurations = configurations.Select(c => new ProductConfigurationDTO
                {
                    ConfigurationId = c.ConfigurationId,
                    Specifications = c.Specifications,
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
                    Console.WriteLine($"Created directory: {uploadsFolder}");
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                // Lưu file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                Console.WriteLine($"Image saved successfully: {fileName} at {filePath}");
                return fileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveImageAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
