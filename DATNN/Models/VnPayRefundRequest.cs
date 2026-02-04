namespace DATNN.Models
{
    public class VnPayRefundRequest
    {
        public string vnp_RequestId { get; set; }
        public string vnp_Version { get; set; } = "2.1.0";
        public string vnp_Command { get; set; } = "refund";
        public string vnp_TmnCode { get; set; }
        public string vnp_TransactionType { get; set; } // 02: Hoàn toàn phần, 03: Hoàn một phần
        public string vnp_TxnRef { get; set; }
        public long vnp_Amount { get; set; }
        public string vnp_OrderInfo { get; set; }
        public string vnp_TransactionNo { get; set; }
        public string vnp_TransactionDate { get; set; } // yyyyMMddHHmmss
        public string vnp_CreateBy { get; set; }
        public string vnp_CreateDate { get; set; } // yyyyMMddHHmmss
        public string vnp_IpAddr { get; set; }
        public string vnp_SecureHash { get; set; }
    }
}
