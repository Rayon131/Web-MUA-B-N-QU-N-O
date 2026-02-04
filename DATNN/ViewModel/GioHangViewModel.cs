using DATNN.Models;

namespace DATNN.ViewModel
{
    public class GioHangViewModel
    {
        public List<ChiTietGioHangViewModel> Items { get; set; } = new List<ChiTietGioHangViewModel>();
        public decimal TongTienHang { get; set; } // Đây là Tạm tính (giá sản phẩm đã giảm)
        public decimal TongTienThanhToan { get; set; } // Đây là Thành tiền (sau khi áp dụng voucher)
    }
}
