namespace DATNN.ViewModel
{
    public class OrderUpdateViewModel
    {

        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
        // PromotionId không cần thiết ở đây vì khuyến mãi tự động được tính toán ở backend
        // public int? PromotionId { get; set; } 
        public string PaymentMethod { get; set; }
        public decimal? TienMatDaNhan { get; set; }
        public int Status { get; set; }

        public int? MaGiamGiaId { get; set; } // << THÊM THUỘC TÍNH NÀY

        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }
}
