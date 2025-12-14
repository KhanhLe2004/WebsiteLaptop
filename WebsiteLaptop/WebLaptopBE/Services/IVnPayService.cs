using WebLaptopBE.Models.VnPay;

namespace WebLaptopBE.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model, string? customTxnRef = null);
        VnPaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
