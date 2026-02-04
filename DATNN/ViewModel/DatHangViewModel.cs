using DATNN.Models;

namespace DATNN.ViewModel
{
    public class DatHangViewModel
    {   // Thông tin người nhận
       
            // Thông tin người nhận
            public string HoTenNguoiNhan { get; set; }
            public string SoDienThoaiNguoiNhan { get; set; }
            public int MaDiaChiDuocChon { get; set; }
            public List<DiaChiNguoiDung> DanhSachDiaChi { get; set; } = new List<DiaChiNguoiDung>();
            public string DiaChiGiaoHang { get; set; }

        public int? SelectedAddressId { get; set; }
        public string Email { get; set; }
            public string? GhiChu { get; set; }

            // Sản phẩm
            public List<ChiTietGioHangViewModel> ItemsToPurchase { get; set; } = new List<ChiTietGioHangViewModel>();
            public List<int> SelectedCartItemIds { get; set; }

            // Thanh toán & Voucher
            public decimal TongTienHang { get; set; }
            public decimal PhiVanChuyen { get; set; }
            public string? AppliedVoucherCode { get; set; } // Mã voucher đang được áp dụng
            public decimal TienGiamGia { get; set; }
            public decimal TongThanhToan { get; set; }
            public string PhuongThucThanhToan { get; set; }

            public double KhoangCachGiaoHang { get; set; }
       

        public List<MaGiamGia> AvailableVouchers { get; set; } = new List<MaGiamGia>();
        
    }
}
