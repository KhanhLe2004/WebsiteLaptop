using WebLaptopBE.Models.VnPay;

namespace WebLaptopBE.Services
{

    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model)
        {
            var vnpay = new VnPayLibrary();
            
            // Validate cấu hình trước khi tạo URL
            ValidateVnPayConfig();
            
            // Tạo mã giao dịch unique
            var txnRef = VnPayHelper.GenerateTransactionRef(model.OrderId);
            
            // Các tham số bắt buộc theo đúng thứ tự và chuẩn VNPay
            // Đảm bảo đầy đủ tất cả tham số theo tài liệu VNPay
            vnpay.AddRequestData("vnp_Version", _config["VnPay:Version"]);
            vnpay.AddRequestData("vnp_Command", _config["VnPay:Command"]);
            vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]);
            vnpay.AddRequestData("vnp_Amount", VnPayHelper.FormatAmount((decimal)model.Amount).ToString());
            vnpay.AddRequestData("vnp_CurrCode", _config["VnPay:CurrCode"]);
            vnpay.AddRequestData("vnp_TxnRef", txnRef);
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {model.OrderId}"); // Không dùng ký tự đặc biệt
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_Locale", _config["VnPay:Locale"]);
            vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:PaymentBackReturnUrl"]);
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
            
            // Thêm thời gian hết hạn (15 phút từ thời điểm tạo)
            var expireDate = model.CreatedDate.AddMinutes(15);
            vnpay.AddRequestData("vnp_ExpireDate", expireDate.ToString("yyyyMMddHHmmss"));

            var paymentUrl = vnpay.CreateRequestUrl(_config["VnPay:BaseUrl"], _config["VnPay:HashSecret"]);

            return paymentUrl;
        }

        // Validate cấu hình VNPay
        private void ValidateVnPayConfig()
        {
            var requiredConfigs = new[]
            {
                "VnPay:TmnCode",
                "VnPay:HashSecret", 
                "VnPay:BaseUrl",
                "VnPay:Version",
                "VnPay:Command",
                "VnPay:CurrCode",
                "VnPay:Locale",
                "VnPay:PaymentBackReturnUrl"
            };

            foreach (var config in requiredConfigs)
            {
                var value = _config[config];
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException($"Thiếu cấu hình VNPay: {config}");
                }
            }

            // Validate TmnCode format (thường là 8 ký tự)
            var tmnCode = _config["VnPay:TmnCode"];
            if (tmnCode.Length != 8)
            {
                throw new InvalidOperationException($"TmnCode không đúng format: {tmnCode}");
            }

            // Validate HashSecret format (thường là 32 ký tự)
            var hashSecret = _config["VnPay:HashSecret"];
            if (hashSecret.Length != 32)
            {
                throw new InvalidOperationException($"HashSecret không đúng format: độ dài {hashSecret.Length}, yêu cầu 32 ký tự");
            }
        }

        public VnPaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            
            // Thêm tất cả các tham số VNPay vào response data
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            // Lấy các thông tin cần thiết
            var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_TransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
            var vnp_Amount = vnpay.GetResponseData("vnp_Amount");

            // Xác thực chữ ký
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _config["VnPay:HashSecret"]);
            if (!checkSignature)
            {
                return new VnPaymentResponseModel
                {
                    Success = false,
                    VnPayResponseCode = "97" // Chữ ký không hợp lệ
                };
            }

            // Kiểm tra mã phản hồi từ VNPay
            bool isSuccessful = vnp_ResponseCode == "00";

            return new VnPaymentResponseModel
            {
                Success = isSuccessful,
                PaymentMethod = "VnPay",
                OrderDescription = vnp_OrderInfo,
                OrderId = vnp_TxnRef, // Giữ nguyên TxnRef để xử lý trong controller
                TransactionId = vnp_TransactionNo,
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode
            };
        }
    }

}
