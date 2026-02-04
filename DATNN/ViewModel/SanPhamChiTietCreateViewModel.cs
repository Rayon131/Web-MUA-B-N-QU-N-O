using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class SanPhamChiTietCreateViewModel
    {
        // === Dữ liệu cần thiết ===
        public int MaSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu sắc.")]
        public int? MaMauSac { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn size.")]
        public int? MaSize { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán không được là số âm.")]
        public decimal? GiaBan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được là số âm.")]
        public int? SoLuong { get; set; }

        public string? GhiChu { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ảnh cho phiên bản.")]
        public IFormFile file { get; set; }

        public int TrangThai { get; set; }

        // === Dữ liệu chỉ để hiển thị trên View ===
        public string? TenSanPham { get; set; }
    }
}
