using DATNN.Models;

namespace AppView.Models.Service.VNPay
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context, string returnUrl);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
        Task<VnPayRefundResponse> Refund(VnPayRefundRequest model, HttpContext context); // <-- THÊM DÒNG NÀY
    }
}