using System.Text.Json;

namespace WebLaptopBE.Models.VnPay
{
    /// <summary>
    /// Logger cho VNPay để debug các vấn đề chữ ký
    /// </summary>
    public static class VnPayLogger
    {
        /// <summary>
        /// Log thông tin request VNPay
        /// </summary>
        /// <param name="requestData">Dữ liệu request</param>
        /// <param name="signData">Chuỗi dữ liệu để ký</param>
        /// <param name="signature">Chữ ký được tạo</param>
        /// <param name="finalUrl">URL cuối cùng</param>
        public static void LogPaymentRequest(
            SortedList<string, string> requestData, 
            string signData, 
            string signature, 
            string finalUrl)
        {
            try
            {
                var logData = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Type = "VNPay_Request",
                    RequestData = requestData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    SignData = signData,
                    Signature = signature,
                    FinalUrl = finalUrl
                };

                var json = JsonSerializer.Serialize(logData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                System.Diagnostics.Debug.WriteLine("=== VNPay Payment Request ===");
                System.Diagnostics.Debug.WriteLine(json);
                System.Diagnostics.Debug.WriteLine("============================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VNPay Log Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Log thông tin response VNPay
        /// </summary>
        /// <param name="responseData">Dữ liệu response</param>
        /// <param name="signData">Chuỗi dữ liệu để verify</param>
        /// <param name="receivedSignature">Chữ ký nhận được</param>
        /// <param name="calculatedSignature">Chữ ký tính toán</param>
        /// <param name="isValid">Kết quả xác thực</param>
        public static void LogPaymentResponse(
            SortedList<string, string> responseData,
            string signData,
            string receivedSignature,
            string calculatedSignature,
            bool isValid)
        {
            try
            {
                var logData = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Type = "VNPay_Response",
                    ResponseData = responseData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    SignData = signData,
                    ReceivedSignature = receivedSignature,
                    CalculatedSignature = calculatedSignature,
                    IsValid = isValid,
                    SignatureMatch = receivedSignature?.Equals(calculatedSignature, StringComparison.InvariantCultureIgnoreCase) ?? false
                };

                var json = JsonSerializer.Serialize(logData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                System.Diagnostics.Debug.WriteLine("=== VNPay Payment Response ===");
                System.Diagnostics.Debug.WriteLine(json);
                System.Diagnostics.Debug.WriteLine("===============================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VNPay Log Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Log lỗi VNPay
        /// </summary>
        /// <param name="error">Thông tin lỗi</param>
        /// <param name="context">Context bổ sung</param>
        public static void LogError(string error, object? context = null)
        {
            try
            {
                var logData = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Type = "VNPay_Error",
                    Error = error,
                    Context = context
                };

                var json = JsonSerializer.Serialize(logData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                System.Diagnostics.Debug.WriteLine("=== VNPay Error ===");
                System.Diagnostics.Debug.WriteLine(json);
                System.Diagnostics.Debug.WriteLine("===================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VNPay Log Error: {ex.Message}");
            }
        }

        /// <summary>
        /// So sánh chi tiết 2 chữ ký để debug
        /// </summary>
        /// <param name="signature1">Chữ ký 1</param>
        /// <param name="signature2">Chữ ký 2</param>
        /// <param name="label1">Nhãn cho chữ ký 1</param>
        /// <param name="label2">Nhãn cho chữ ký 2</param>
        public static void CompareSignatures(string signature1, string signature2, string label1 = "Signature 1", string label2 = "Signature 2")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Signature Comparison ===");
                System.Diagnostics.Debug.WriteLine($"{label1}: {signature1}");
                System.Diagnostics.Debug.WriteLine($"{label2}: {signature2}");
                System.Diagnostics.Debug.WriteLine($"Length 1: {signature1?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"Length 2: {signature2?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"Equal (Case Sensitive): {signature1 == signature2}");
                System.Diagnostics.Debug.WriteLine($"Equal (Case Insensitive): {string.Equals(signature1, signature2, StringComparison.InvariantCultureIgnoreCase)}");
                
                if (!string.IsNullOrEmpty(signature1) && !string.IsNullOrEmpty(signature2))
                {
                    var minLength = Math.Min(signature1.Length, signature2.Length);
                    for (int i = 0; i < minLength; i++)
                    {
                        if (signature1[i] != signature2[i])
                        {
                            System.Diagnostics.Debug.WriteLine($"First difference at position {i}: '{signature1[i]}' vs '{signature2[i]}'");
                            break;
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("============================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Signature comparison error: {ex.Message}");
            }
        }
    }
}
