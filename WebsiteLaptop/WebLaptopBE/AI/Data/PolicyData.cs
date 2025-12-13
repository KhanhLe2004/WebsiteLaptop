using System.Collections.Generic;

namespace WebLaptopBE.AI.Data;

/// <summary>
/// Lưu trữ toàn bộ chính sách của TenTech
/// Dữ liệu này sẽ được sử dụng để phản hồi khách hàng
/// </summary>
public static class PolicyData
{
    /// <summary>
    /// Chính sách bảo hành TenTech
    /// </summary>
    public static readonly PolicyDocument WarrantyPolicy = new()
    {
        PolicyId = "warranty_policy",
        Title = "CHÍNH SÁCH BẢO HÀNH TẠI TENTECH",
        Category = PolicyCategory.Warranty,
        Keywords = new[]
        {
            "bảo hành", "warranty", "bao hanh", 
            "đổi máy", "doi may", "thay thế", "thay the",
            "linh kiện", "linh kien", 
            "màn hình", "man hinh", "screen",
            "pin", "battery",
            "tem", "seal",
            "lỗi", "hỏng", "hong", "error"
        },
        Content = @"CHÍNH SÁCH BẢO HÀNH TẠI TENTECH

*Lưu ý: Các thiết bị bảo hành phải trong thời gian bảo hành và còn nguyên tem của TenTech!

1. BẢO HÀNH 01 ĐỔI 01
   - Nếu linh kiện thay thế không có sẵn, cần đặt hàng thì TenTech sẽ giải quyết trong tối đa 07 ngày làm việc (không kể Chủ nhật & ngày lễ). 
   - Nếu quá 07 ngày mà chưa có linh kiện, TenTech sẽ đổi sang máy tương tự khác.
   - Nếu khách hàng không đồng ý đổi máy khác, TenTech sẽ nhập máy cùng model và cấu hình để đổi tối đa trong 01 tháng.

2. BẢO HÀNH MÀN HÌNH
   - Màn hình được bảo hành khi có từ 05 điểm lỗi trở lên (bao gồm điểm chết và điểm sáng). 
   - Không phân biệt loại tấm nền, không bảo hành hở sáng IPS (IPS glow).

3. BẢO HÀNH PIN VÀ LINH KIỆN KHÁC
   - Pin: Bảo hành khi sử dụng ứng dụng văn phòng dưới 1,5 giờ trong vòng 03 tháng. Bảo hành thêm 01 tháng nếu phát sinh trong tháng cuối cùng.
   - Linh kiện khác ngoài pin: Bảo hành thêm 03 tháng nếu phát sinh trong 03 tháng cuối cùng.
   *Không áp dụng cộng dồn thời gian bảo hành.

4. GIỮ BẢO HÀNH CHO LINH KIỆN NÂNG CẤP HOẶC THAY THẾ
   - Áp dụng nếu linh kiện còn tem của TenTech và nhà cung cấp.

5. KHÔNG BẢO HÀNH CÁC TÙY CHỌN CỘNG THÊM
   - Bàn phím LED, trackpoint, cảm biến vân tay, bluetooth, webcam, màn hình cảm ứng...

6. KHÔNG BẢO HÀNH CÁC HỎNG HÓC DO NGOẠI LỰC HOẶC YẾU TỐ MÔI TRƯỜNG
   - Hơi ẩm, đổ nước, cháy nổ, tỳ đè, rơi vỡ, côn trùng xâm nhập...

7. KHÔNG BẢO HÀNH MÀN HÌNH BỊ VỠ, BẦM, DẬP, CHẢY MỰC
   - Không bảo hành các hư hỏng màn hình do lực bên ngoài hoặc sọc.

8. KHÔNG CHỊU TRÁCH NHIỆM DỮ LIỆU & PHẦN MÀM KHÁCH HÀNG
   - Khách hàng vui lòng sao lưu dữ liệu trước khi gửi bảo hành.

THÔNG TIN LIÊN HỆ BẢO HÀNH:
Địa chỉ: TenTech, 3 Đ. Cầu Giấy, Ngọc Khánh, Đống Đa, Hà Nội
Thời gian tiếp nhận: 8h00 - 21h00 tất cả các ngày trong tuần (trừ Lễ Tết)
Điện thoại: 024.7106.9999"
    };

    /// <summary>
    /// Chính sách bảo mật thông tin khách hàng
    /// </summary>
    public static readonly PolicyDocument PrivacyPolicy = new()
    {
        PolicyId = "privacy_policy",
        Title = "CHÍNH SÁCH BẢO MẬT THÔNG TIN KHÁCH HÀNG TẠI TENTECH",
        Category = PolicyCategory.Privacy,
        Keywords = new[]
        {
            "bảo mật", "bao mat", "privacy", "thông tin", "thong tin",
            "dữ liệu", "du lieu", "data",
            "cá nhân", "ca nhan", "personal",
            "thu thập", "thu thap", "collect",
            "lưu trữ", "luu tru", "storage",
            "an toàn", "an toan", "security"
        },
        Content = @"CHÍNH SÁCH BẢO MẬT THÔNG TIN KHÁCH HÀNG TẠI TENTECH

1. SỰ CHẤP THUẬN
   Bằng việc truy cập vào và sử dụng một số dịch vụ tại website TenTech, bạn đồng ý rằng thông tin cá nhân của bạn sẽ được thu thập và sử dụng như được nêu trong Chính Sách này. 
   Trường hợp bạn không đồng ý với Chính sách này, bạn có thể dừng cung cấp thông tin cho chúng tôi và/hoặc sử dụng các quyền như được nêu dưới đây.

2. PHẠM VI THU THẬP
   - Thông tin cá nhân trực tiếp từ bạn khi đăng ký tài khoản.
   - Thông tin truy cập như số trang, số liên kết click.
   - Thông tin trình duyệt: IP, loại browser, ngôn ngữ, thời gian truy cập.
   - Thông tin từ các nguồn hợp pháp khác.

3. MỤC ĐÍCH THU THẬP VÀ SỬ DỤNG THÔNG TIN
   Thông tin cá nhân được sử dụng cho các mục đích:
   - Xử lý đơn hàng và thông báo trạng thái.
   - Tạo và duy trì tài khoản.
   - Gửi thông tin khuyến mãi, bảo hành và khảo sát.
   - Cá nhân hóa dịch vụ, đảm bảo an ninh và tuân thủ pháp luật.

4. THỜI GIAN LƯU TRỮ THÔNG TIN
   Dữ liệu cá nhân sẽ được lưu trữ cho đến khi có yêu cầu hủy bỏ và luôn được bảo mật trên máy chủ của TenTech.

5. CAM KẾT BẢO MẬT THÔNG TIN CÁ NHÂN
   - Khách hàng có quyền yêu cầu thay đổi hoặc hủy bỏ thông tin.
   - TenTech cam kết bảo mật tuyệt đối theo chính sách.
   - Thông tin chỉ cung cấp cho các bên theo phạm vi hợp pháp.
   - Sử dụng biện pháp quản lý và kỹ thuật để bảo vệ dữ liệu.
   - Chỉ nhân viên, đại diện và nhà cung cấp được truy cập thông tin trên cơ sở cần thiết."
    };

    /// <summary>
    /// Hướng dẫn thanh toán
    /// </summary>
    public static readonly PolicyDocument PaymentPolicy = new()
    {
        PolicyId = "payment_policy",
        Title = "HƯỚNG DẪN THANH TOÁN",
        Category = PolicyCategory.Payment,
        Keywords = new[]
        {
            "thanh toán", "thanh toan", "payment", "pay",
            "tiền", "tien", "money", "cash",
            "chuyển khoản", "chuyen khoan", "transfer",
            "QR", "VietQR", "quét mã", "quet ma",
            "COD", "ship cod",
            "thẻ", "the", "card", "quẹt thẻ", "quet the",
            "hóa đơn", "hoa don", "invoice", "VAT",
            "online", "trực tuyến", "truc tuyen"
        },
        Content = @"HƯỚNG DẪN THANH TOÁN

TenTech hiện hỗ trợ thanh toán theo các cách thức sau:

A. THANH TOÁN TẠI CỬA HÀNG
   - Tiền mặt
   - Chuyển khoản
   - Quẹt thẻ

B. THANH TOÁN VỚI ĐƠN HÀNG ONLINE
   Khách hàng có thể thanh toán bằng 2 cách sau:

   CÁCH 1: THANH TOÁN COD (Ship COD)
   - Nhận hàng tại nhà và thanh toán trực tiếp cho nhân viên bưu tá khi nhận hàng.

   CÁCH 2: THANH TOÁN QUA QR CODE TẠI WEBSITE
   Hướng dẫn thanh toán bằng mã QR:
   1. Khách hàng nhập thông tin mua hàng.
   2. Sau khi tiến hành đặt hàng, khách chọn thanh toán qua Chuyển khoản VietQR.
   3. Khách hàng truy cập Ứng dụng ngân hàng hoặc Ví điện tử và thực hiện thanh toán bằng hình thức quét mã QR để thanh toán.

LƯU Ý QUAN TRỌNG:
   - Thanh toán online qua VietQR KHÔNG ÁP DỤNG với đơn hàng trên 20 triệu đồng.
   - Trường hợp muốn xuất hóa đơn đỏ VAT công ty thì đối tượng giao dịch phải thuộc công ty cần xuất hóa đơn.

MỌI THẮC MẮC VỀ THANH TOÁN:
   Nếu quý khách có bất kỳ thắc mắc, khiếu nại hoặc cần hỗ trợ về thanh toán, vui lòng liên hệ với chúng tôi theo các kênh sau:

   Hotline: 1900 1234 (từ 8:00 - 21:00 tất cả các ngày trong tuần)
   Email: TenTech@gmail.vn

   Chúng tôi cam kết phản hồi mọi yêu cầu trong thời gian sớm nhất để đảm bảo trải nghiệm tốt nhất cho quý khách."
    };

    /// <summary>
    /// Lấy tất cả chính sách
    /// </summary>
    public static List<PolicyDocument> GetAllPolicies()
    {
        return new List<PolicyDocument>
        {
            WarrantyPolicy,
            PrivacyPolicy,
            PaymentPolicy
        };
    }

    /// <summary>
    /// Tìm kiếm chính sách dựa trên keywords
    /// </summary>
    public static List<PolicyDocument> SearchPolicies(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAllPolicies();
        }

        var queryLower = query.ToLower().Trim();
        var results = new List<PolicyDocument>();

        foreach (var policy in GetAllPolicies())
        {
            // Kiểm tra xem query có chứa bất kỳ keyword nào không
            foreach (var keyword in policy.Keywords)
            {
                if (queryLower.Contains(keyword.ToLower()))
                {
                    if (!results.Contains(policy))
                    {
                        results.Add(policy);
                    }
                    break;
                }
            }
        }

        // Nếu không tìm thấy, trả về tất cả để chatbot có thể xử lý
        return results.Count > 0 ? results : GetAllPolicies();
    }
}

/// <summary>
/// Đại diện cho một document chính sách
/// </summary>
public class PolicyDocument
{
    public string PolicyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public PolicyCategory Category { get; set; }
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Danh mục chính sách
/// </summary>
public enum PolicyCategory
{
    Warranty,      // Bảo hành
    Privacy,       // Bảo mật
    Payment,       // Thanh toán
    Shipping,      // Vận chuyển
    Return         // Đổi trả
}

