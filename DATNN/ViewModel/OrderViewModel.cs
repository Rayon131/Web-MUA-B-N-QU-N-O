namespace DATNN.ViewModel
{
    public class OrderViewModel
    {
        public int? CustomerId { get; set; } // Có thể là khách vãng lai
        public int? PromotionId { get; set; }
        public string PaymentMethod { get; set; } // "Tiền mặt", "Chuyển khoản"...
        public int Status { get; set; } // 1: Chờ thanh toán, 2: Đã thanh toán
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }
}
