namespace DATNN.ViewModel
{
    public class ThongKeViewModel
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }

        public decimal TongDoanhThu { get; set; }
        public decimal TongChiTieu { get; set; }
        public decimal LoiNhuan { get; set; }

        // Dữ liệu cho biểu đồ phân loại chi phí
        public decimal ChiPhiVanHanh { get; set; }
        public decimal ChiPhiRuiRoDonHang { get; set; }
        public decimal ChiPhiRuiRoDoiTra { get; set; }

        // Dữ liệu đếm số lượng
        public int SoDonHangThanhCong { get; set; }
        public int SoDonHangHuy { get; set; }
        public int SoYeuCauDoiTra { get; set; }
    }
}
