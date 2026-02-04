using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class SanPhamChiTietItem
    {
        // Thêm dấu ? vào int
        [Required(ErrorMessage = "Vui lòng chọn màu sắc.")]
        public int? MaMauSac { get; set; }

        // Thêm dấu ? vào int
        [Required(ErrorMessage = "Vui lòng chọn size.")]
        public int? MaSize { get; set; }

        // Thêm dấu ? vào decimal
        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán không được là số âm.")]
        public decimal? GiaBan { get; set; }

        // Thêm dấu ? vào int
        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được là số âm.")]
        public int? SoLuong { get; set; }

        public string? GhiChu { get; set; }

        // Ảnh không cần dấu ? vì IFormFile là reference type (có thể null sẵn rồi)
        // Nhưng cần validation này để bắt buộc chọn
        [Required(ErrorMessage = "Vui lòng chọn ảnh cho phiên bản.")]
        public IFormFile file { get; set; }

        public int TrangThai { get; set; }
    }

}
