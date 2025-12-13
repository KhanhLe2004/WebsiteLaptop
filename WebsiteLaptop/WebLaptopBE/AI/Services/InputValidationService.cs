using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebLaptopBE.AI.Services;

/// <summary>
/// Interface cho Input Validation Service
/// </summary>
public interface IInputValidationService
{
    /// <summary>
    /// Validate input từ người dùng
    /// Trả về ValidationResult với thông tin validation
    /// </summary>
    ValidationResult ValidateUserInput(string input);
}

/// <summary>
/// Service để validate input từ người dùng
/// Kiểm tra độ dài, lỗi cú pháp, spam, v.v.
/// </summary>
public class InputValidationService : IInputValidationService
{
    private readonly ILogger<InputValidationService> _logger;

    // Cấu hình validation
    private const int MaxLength = 500;
    private const int MinLength = 2;
    private const int MaxWords = 100;
    private const int MinWords = 1;
    private const double MaxRepeatedCharRatio = 0.4; // 40% ký tự lặp là quá nhiều

    public InputValidationService(ILogger<InputValidationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validate input từ người dùng
    /// </summary>
    public ValidationResult ValidateUserInput(string input)
    {
        try
        {
            // 1. Kiểm tra null hoặc empty
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.Empty,
                    Message = "Xin chào! Em là trợ lý ảo của TenTech. Anh/chị vui lòng nhập câu hỏi để em có thể hỗ trợ ạ."
                };
            }

            // 2. Kiểm tra độ dài quá ngắn
            if (input.Trim().Length < MinLength)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.TooShort,
                    Message = "Câu hỏi của anh/chị hơi ngắn ạ. Anh/chị có thể nói rõ hơn một chút được không?"
                };
            }

            // 3. Kiểm tra độ dài quá dài
            if (input.Length > MaxLength)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.TooLong,
                    Message = $"Em xin lỗi, câu hỏi của anh/chị hơi dài ạ (hiện tại {input.Length} ký tự). " +
                             $"Anh/chị vui lòng viết ngắn gọn hơn, tập trung vào nội dung chính (tối đa {MaxLength} ký tự) để em có thể hỗ trợ tốt hơn."
                };
            }

            // 4. Kiểm tra số từ quá nhiều
            var words = input.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > MaxWords)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.TooManyWords,
                    Message = $"Em xin lỗi, câu hỏi của anh/chị có quá nhiều từ ({words.Length} từ). " +
                             $"Anh/chị có thể tóm tắt ngắn gọn hơn được không? Ví dụ: 'Laptop Dell giá dưới 20 triệu' hoặc 'Chính sách bảo hành như thế nào?'"
                };
            }

            // 5. Kiểm tra ký tự lặp lại quá nhiều (spam detection)
            if (HasExcessiveRepeatedChars(input))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.ExcessiveRepetition,
                    Message = "Em nhận thấy có nhiều ký tự lặp lại trong câu hỏi. Anh/chị vui lòng viết lại một cách rõ ràng hơn nhé."
                };
            }

            // 6. Kiểm tra lỗi cú pháp nghiêm trọng (quá nhiều ký tự đặc biệt không hợp lệ)
            if (HasSyntaxErrors(input))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.SyntaxError,
                    Message = "Em có chút khó khăn khi đọc câu hỏi của anh/chị. " +
                             "Anh/chị vui lòng viết lại câu hỏi một cách rõ ràng hơn, " +
                             "tránh viết tắt hoặc gõ sai quá nhiều nhé. Ví dụ: 'Laptop gaming giá rẻ' hoặc 'Chính sách đổi trả'."
                };
            }

            // 7. Kiểm tra có phải chỉ là ký tự đặc biệt không
            if (IsOnlySpecialChars(input))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.OnlySpecialChars,
                    Message = "Em xin lỗi, em không hiểu câu hỏi của anh/chị. Anh/chị có thể viết rõ hơn được không?"
                };
            }

            // 8. Kiểm tra spam (cùng một ký tự/từ lặp lại liên tiếp)
            if (IsSpam(input))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorType = ValidationErrorType.Spam,
                    Message = "Em xin lỗi, tin nhắn của anh/chị có vẻ không hợp lệ. Anh/chị vui lòng nhập câu hỏi cụ thể để em có thể hỗ trợ."
                };
            }

            // 9. Cảnh báo nếu có lỗi chính tả nhiều (không block nhưng đưa ra gợi ý)
            var spellingWarning = CheckSpellingWarning(input);
            if (!string.IsNullOrEmpty(spellingWarning))
            {
                _logger.LogInformation("Spelling warning for input: {Input}", input);
                // Không block, chỉ log để theo dõi
            }

            // Input hợp lệ
            return new ValidationResult
            {
                IsValid = true,
                ErrorType = ValidationErrorType.None,
                Message = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating input");
            // Nếu có lỗi, cho phép input đi qua
            return new ValidationResult
            {
                IsValid = true,
                ErrorType = ValidationErrorType.None,
                Message = string.Empty
            };
        }
    }

    /// <summary>
    /// Kiểm tra xem có quá nhiều ký tự lặp lại không
    /// Ví dụ: "aaaaaa", "!!!!!!", "hahahahahaha"
    /// </summary>
    private bool HasExcessiveRepeatedChars(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length < 10)
        {
            return false;
        }

        // Đếm số lượng ký tự lặp lại liên tiếp
        int repeatedCount = 0;
        char lastChar = input[0];
        int consecutiveCount = 1;

        for (int i = 1; i < input.Length; i++)
        {
            if (input[i] == lastChar)
            {
                consecutiveCount++;
                // Nếu 1 ký tự lặp lại quá 4 lần liên tiếp
                if (consecutiveCount > 4)
                {
                    repeatedCount++;
                }
            }
            else
            {
                consecutiveCount = 1;
                lastChar = input[i];
            }
        }

        // Nếu quá 20% input là ký tự lặp lại
        double ratio = (double)repeatedCount / input.Length;
        return ratio > MaxRepeatedCharRatio;
    }

    /// <summary>
    /// Kiểm tra lỗi cú pháp nghiêm trọng
    /// </summary>
    private bool HasSyntaxErrors(string input)
    {
        // Đếm số lượng ký tự đặc biệt
        int specialCharCount = input.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && c != '?' && c != '!' && c != '.' && c != ',');
        
        // Nếu quá 30% là ký tự đặc biệt -> có vấn đề
        double specialCharRatio = (double)specialCharCount / input.Length;
        if (specialCharRatio > 0.3)
        {
            return true;
        }

        // Kiểm tra có quá nhiều số không
        int digitCount = input.Count(char.IsDigit);
        double digitRatio = (double)digitCount / input.Length;
        if (digitRatio > 0.7 && input.Length > 20)
        {
            return true; // Có thể là spam số
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra xem input có chỉ là ký tự đặc biệt không
    /// </summary>
    private bool IsOnlySpecialChars(string input)
    {
        var trimmed = input.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return true;
        }

        // Kiểm tra xem có ít nhất 1 chữ cái hoặc số không
        return !trimmed.Any(c => char.IsLetterOrDigit(c));
    }

    /// <summary>
    /// Kiểm tra spam (cùng một từ lặp lại nhiều lần)
    /// </summary>
    private bool IsSpam(string input)
    {
        var words = input.ToLower().Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length < 5)
        {
            return false;
        }

        // Đếm tần suất của từng từ
        var wordFrequency = words.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
        
        // Nếu 1 từ xuất hiện quá 50% -> spam
        foreach (var freq in wordFrequency.Values)
        {
            if ((double)freq / words.Length > 0.5)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra cảnh báo lỗi chính tả
    /// Không block, chỉ return warning message
    /// </summary>
    private string? CheckSpellingWarning(string input)
    {
        // Danh sách từ phổ biến trong domain laptop
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "laptop", "máy", "tính", "giá", "bao", "nhiêu", "mua", "bán", 
            "bảo", "hành", "chính", "sách", "thanh", "toán", "đổi", "trả",
            "gaming", "văn", "phòng", "học", "tập", "dell", "hp", "lenovo", "asus"
        };

        var words = input.Split(new[] { ' ', '\n', '\r', '\t', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length < 3)
        {
            return null;
        }

        // Đếm số từ không có trong common words
        int uncommonCount = 0;
        foreach (var word in words)
        {
            if (word.Length >= 3 && !commonWords.Contains(word) && !char.IsDigit(word[0]))
            {
                uncommonCount++;
            }
        }

        // Nếu quá 80% từ không phổ biến
        double uncommonRatio = (double)uncommonCount / words.Length;
        if (uncommonRatio > 0.8)
        {
            return "Possible spelling errors detected";
        }

        return null;
    }
}

/// <summary>
/// Kết quả validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public ValidationErrorType ErrorType { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Loại lỗi validation
/// </summary>
public enum ValidationErrorType
{
    None,
    Empty,
    TooShort,
    TooLong,
    TooManyWords,
    ExcessiveRepetition,
    SyntaxError,
    OnlySpecialChars,
    Spam
}



