using DATNN.Libraries;
using DATNN.Models;
using Newtonsoft.Json;
using System.Text;

namespace AppView.Models.Service.VNPay
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context, string returnUrl)
        {
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var pay = new VnPayLibrary();

            // =================================================================
            // SỬA LỖI Ở ĐÂY: KHÔNG NỐI THÊM GUID VÀO OrderId
            // Mã TxnRef phải là mã duy nhất bạn tạo ra từ đầu và dùng xuyên suốt
            var txnRef = model.OrderId;
            // =================================================================

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);
            pay.AddRequestData("vnp_Amount", ((long)model.Amount * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]);
            pay.AddRequestData("vnp_OrderInfo", $"{model.Name} {model.OrderDescription}");
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);

            // Sử dụng txnRef đã được sửa
            pay.AddRequestData("vnp_TxnRef", txnRef);

            var paymentUrl = pay.CreateRequestUrl(
                _configuration["Vnpay:BaseUrl"],
                _configuration["Vnpay:HashSecret"]
            );

            return paymentUrl;
        }


        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var response = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]);

            return response;
        }
        public async Task<VnPayRefundResponse> Refund(VnPayRefundRequest model, HttpContext context)
        {
            var vnp_Api = _configuration["Vnpay:ApiUrl"];
            var vnp_HashSecret = _configuration["Vnpay:HashSecret"];
            var vnp_TmnCode = _configuration["Vnpay:TmnCode"];

            // [FIX CỨNG]: Luôn dùng 127.0.0.1 để đảm bảo không bao giờ bị lỗi IP trên Localhost
            // Khi nào deploy lên server thật thì đổi lại sau, hoặc VNPAY server vẫn chấp nhận 127.0.0.1 trong nội dung request
            model.vnp_IpAddr = "127.0.0.1";

            model.vnp_TmnCode = vnp_TmnCode;

            // Tạo chuỗi Checksum
            var data = $"{model.vnp_RequestId}|{model.vnp_Version}|{model.vnp_Command}|{model.vnp_TmnCode}|" +
                       $"{model.vnp_TransactionType}|{model.vnp_TxnRef}|{model.vnp_Amount}|{model.vnp_TransactionNo}|" +
                       $"{model.vnp_TransactionDate}|{model.vnp_CreateBy}|{model.vnp_CreateDate}|{model.vnp_IpAddr}|{model.vnp_OrderInfo}";

            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, data);
            model.vnp_SecureHash = vnp_SecureHash;

            var requestJson = JsonConvert.SerializeObject(model);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(vnp_Api, content);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<VnPayRefundResponse>(responseString);
                return responseObject;
            }
        }
        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new System.Security.Cryptography.HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}